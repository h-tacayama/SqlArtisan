using System.Reflection;

namespace SqlArtisan.Tests;

/// <summary>
/// The inverse of the #244 boundary rule <see cref="PublicSurfaceNamingTests"/>
/// exercises: that one says every type a user must NAME lives in the root
/// namespace; these three say nothing else becomes public surface by accident —
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
    /// signature hands it back. With every constructor internal, one that no
    /// signature returns can be named but never held — surface a caller cannot
    /// reach at all.
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
    /// The root namespace is deliberately outside this check: a generated table
    /// class derives from <c>DbTableBase</c> in the caller's own assembly.
    /// </summary>
    [Fact]
    public void ExportedType_InInternalNamespace_HasNoConstructorReachableFromOutside()
    {
        List<string> constructible = [.. typeof(Sql).Assembly.GetExportedTypes()
            .Where(t => t.Namespace == InternalNamespace)
            .Where(t => t.GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                .Any(c => c.IsPublic || c.IsFamily || c.IsFamilyOrAssembly))
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
    /// Output positions only — what a caller can end up holding. A parameter type
    /// does not count: with every constructor internal, a type that is only ever
    /// accepted is one no caller can obtain to pass.
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
