using System.Reflection;

namespace SqlArtisan.Tests;

/// <summary>
/// The inverse of the #244 boundary rule <see cref="PublicSurfaceNamingTests"/>
/// exercises: that one says every type a user must NAME lives in the root
/// namespace; these four say nothing else becomes public surface by accident —
/// from 1.0 a slipped <c>public</c> is a SemVer promise nobody meant to make.
/// </summary>
public class PublicSurfaceBoundaryTests
{
    private const string RootNamespace = "SqlArtisan";
    private const string InternalNamespace = "SqlArtisan.Internal";

    /// <summary>
    /// The other two gates key on <see cref="InternalNamespace"/> exactly, so a
    /// type one namespace deeper would escape both while still being exported —
    /// and a folder-derived namespace is exactly what an IDE's "add class" emits
    /// in a tree this deep. Holding the set closed is what makes them total.
    /// </summary>
    [Fact]
    public void ExportedTypes_SpanOnlyTheRootAndInternalNamespaces()
    {
        List<string> unexpected = [.. typeof(Sql).Assembly.GetExportedTypes()
            .Select(t => t.Namespace ?? "<global namespace>")
            .Distinct()
            .Where(ns => ns != RootNamespace && ns != InternalNamespace)
            .OrderBy(ns => ns, StringComparer.Ordinal)];

        Assert.True(
            unexpected.Count == 0,
            $"{unexpected.Count} exported namespaces beyond {RootNamespace} and "
                + $"{InternalNamespace} — declare the type internal, or move it under one of "
                + "the two. A third namespace is a surface decision, so wanting one means "
                + "naming it here and in docs/versioning.md:\n  "
                + string.Join("\n  ", unexpected));
    }

    /// <summary>
    /// A type here is legitimately public only because some root-namespace
    /// signature hands it back. With no constructor reachable from outside the
    /// assembly, one that no signature returns can be named but never held —
    /// surface a caller cannot reach at all.
    /// </summary>
    [Fact]
    public void ExportedType_InInternalNamespace_IsHandedBackByARootNamespaceSignature()
    {
        Type[] exported = typeof(Sql).Assembly.GetExportedTypes();
        HashSet<Type> candidates = [.. exported.Where(t => t.Namespace == InternalNamespace)];

        HashSet<Type> reachable = [];
        Queue<Type> pending = new(exported
            .Where(t => t.Namespace == RootNamespace)
            .SelectMany(HandedBackTypes)
            .Where(candidates.Contains));

        while (pending.Count > 0)
        {
            Type type = pending.Dequeue();
            if (!reachable.Add(type))
            {
                continue;
            }

            // Base types and interfaces travel with the type the user already
            // holds: casting to them is part of what the signature handed over.
            foreach (Type next in HandedBackTypes(type)
                .Concat(type.GetInterfaces())
                .Concat(type.BaseType is { } baseType ? [baseType] : [])
                .Where(candidates.Contains))
            {
                pending.Enqueue(next);
            }
        }

        List<string> unreachable = [.. candidates
            .Where(t => !reachable.Contains(t))
            .Select(t => t.Name)
            .OrderBy(n => n, StringComparer.Ordinal)];

        Assert.True(
            unreachable.Count == 0,
            $"{unreachable.Count} public types in {InternalNamespace} are handed back by no "
                + $"public signature — make them internal:\n  " + string.Join("\n  ", unreachable));
    }

    /// <summary>
    /// These types are reached through the <c>Sql.*</c> call, operator, or chain
    /// step that produces them, so none needs a constructor another assembly can
    /// reach. Three spellings publish one silently — a primary constructor, whose
    /// accessibility follows its class, a class declaring no constructor at all,
    /// and either of those on an <c>abstract</c> class, where the constructor is
    /// <c>protected</c> rather than public and so reachable by deriving (#492).
    /// The root namespace cannot take the same blanket — deriving is the
    /// documented use of three bases there (<see cref="DbTableBase"/>,
    /// <see cref="CteBase"/>, <see cref="DerivedTableBase"/>) — so
    /// <see cref="ExportedAbstractType_InRootNamespace_IsDerivableOnlyWhenAllowlisted"/>
    /// scans it against that allowlist rather than leaving it unchecked.
    /// </summary>
    [Fact]
    public void ExportedType_InInternalNamespace_HasNoConstructorReachableFromOutside()
    {
        List<string> constructible = [.. typeof(Sql).Assembly.GetExportedTypes()
            .Where(t => t.Namespace == InternalNamespace)
            .Where(HasConstructorReachableFromOutside)
            .Select(t => t.Name)
            .OrderBy(n => n, StringComparer.Ordinal)];

        Assert.True(
            constructible.Count == 0,
            $"{constructible.Count} public types in {InternalNamespace} can be constructed or "
                + "derived from outside the assembly — make each constructor internal, or "
                + "private protected on an abstract base:\n  "
                + string.Join("\n  ", constructible));
    }

    /// <summary>
    /// The other four public abstract types here are closed to a foreign
    /// subclass only by accident of separate mechanisms — a <c>private
    /// protected</c> constructor, or an <c>internal abstract</c> member such a
    /// subclass cannot implement (CS0534) — and nothing asserted that. An ADR
    /// 0005 promotion moves a base out of <see cref="InternalNamespace"/>, as
    /// #488 did, and the #492 gate stops scanning it with no test failing; a
    /// promoted base that implements every abstract member and keeps a protected
    /// constructor taking a raw token reopens the hole #492 closed, silently.
    /// Deriving here is a surface decision, so it is made once in the allowlist.
    /// </summary>
    [Fact]
    public void ExportedAbstractType_InRootNamespace_IsDerivableOnlyWhenAllowlisted()
    {
        // By type rather than by name: renaming or unexporting one of the three
        // fails to compile here instead of silently shrinking what is permitted.
        HashSet<Type> derivableByDesign =
            [typeof(DbTableBase), typeof(CteBase), typeof(DerivedTableBase)];

        List<string> derivable = [.. typeof(Sql).Assembly.GetExportedTypes()
            // A static class is abstract and sealed; only an open base is derivable.
            .Where(t => t.Namespace == RootNamespace && t.IsClass && t.IsAbstract && !t.IsSealed)
            .Where(t => !derivableByDesign.Contains(t))
            .Where(IsExternallyDerivable)
            .Select(t => t.Name)
            .OrderBy(n => n, StringComparer.Ordinal)];

        Assert.True(
            derivable.Count == 0,
            $"{derivable.Count} public abstract types in {RootNamespace} can be derived from "
                + "outside the assembly — make the constructor private protected, or, where "
                + "deriving is the documented use, say so by naming the type in this test's "
                + "allowlist and in its XML docs:\n  "
                + string.Join("\n  ", derivable));
    }

    /// <summary>
    /// Both derivation gates turn on one question — can another assembly reach a
    /// constructor? — so they read the answer from here rather than each
    /// spelling out the three accessibilities that give it.
    /// </summary>
    private static bool HasConstructorReachableFromOutside(Type type) =>
        type.GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
            .Any(c => c.IsPublic || c.IsFamily || c.IsFamilyOrAssembly);

    /// <summary>
    /// A reachable constructor alone does not make a base derivable: an abstract
    /// member this assembly keeps to itself leaves a foreign subclass unable to
    /// compile (CS0534), closing the type whatever its constructor says.
    /// Reflection resolves each virtual slot to its most derived declaration, so
    /// a member still abstract here is one no base along the chain implemented.
    /// </summary>
    private static bool IsExternallyDerivable(Type type) =>
        HasConstructorReachableFromOutside(type)
        && !type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
            .Any(m => m.IsAbstract && (m.IsAssembly || m.IsFamilyAndAssembly));

    /// <summary>
    /// Output positions only — what a caller can end up holding. A parameter type
    /// does not count: with no constructor reachable from outside the assembly, a
    /// type that is only ever accepted is one no caller can obtain to pass.
    /// </summary>
    private static IEnumerable<Type> HandedBackTypes(Type type)
    {
        const BindingFlags Declared = BindingFlags.Public
            | BindingFlags.Static
            | BindingFlags.Instance
            | BindingFlags.DeclaredOnly;

        IEnumerable<Type> returns = type.GetMethods(Declared).Select(m => m.ReturnType);
        IEnumerable<Type> properties = type.GetProperties(Declared).Select(p => p.PropertyType);
        IEnumerable<Type> fields = type.GetFields(Declared).Select(f => f.FieldType);

        return returns
            .Concat(properties)
            .Concat(fields)
            .SelectMany(Unwrap);
    }

    /// <summary>
    /// A signature's type is the payload, not the wrapper: an array element or a
    /// generic argument is just as much handed to the user as the bare type is.
    /// </summary>
    private static IEnumerable<Type> Unwrap(Type type)
    {
        while (type.HasElementType)
        {
            type = type.GetElementType()!;
        }

        return type.IsGenericType
            ? type.GetGenericArguments().SelectMany(Unwrap).Append(type)
            : [type];
    }
}
