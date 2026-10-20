using System.Reflection;

namespace SqlArtisan.Tests;

/// <summary>
/// The inverse of the #244 boundary rule <see cref="PublicSurfaceNamingTests"/>
/// exercises: that one says every type a user must NAME lives in the root
/// namespace; these say nothing else becomes public surface by accident —
/// from 1.0 a slipped <c>public</c> is a SemVer promise nobody meant to make.
/// </summary>
public class PublicSurfaceBoundaryTests
{
    private const string RootNamespace = "SqlArtisan";

    /// <summary>
    /// The library exports one namespace (ADR 0027): every type it hands back is
    /// covered surface, so a type in a second namespace — what an IDE's "add class"
    /// emits from a folder this deep — would be a promise the records never made.
    /// </summary>
    [Fact]
    public void ExportedTypes_AreAllInTheRootNamespace()
    {
        List<string> unexpected = [.. typeof(Sql).Assembly.GetExportedTypes()
            .Where(t => t.Namespace != RootNamespace)
            .Select(t => t.FullName!)
            .OrderBy(n => n, StringComparer.Ordinal)];

        Assert.True(
            unexpected.Count == 0,
            $"{unexpected.Count} exported types outside {RootNamespace} — declare the type "
                + "internal, or move it to the root:\n  " + string.Join("\n  ", unexpected));
    }

    /// <summary>
    /// A type no caller can construct is public only because some signature hands
    /// it back; one that none returns can be named but never held — surface a
    /// caller cannot reach at all.
    /// </summary>
    [Fact]
    public void ExportedType_NoCallerConstructs_IsHandedBackBySomeSignature()
    {
        Type[] exported = typeof(Sql).Assembly.GetExportedTypes();
        HashSet<Type> candidates = [.. exported.Where(IsHandedBackOnly)];

        HashSet<Type> reachable = [];
        Queue<Type> pending = new(exported
            .Where(t => !candidates.Contains(t))
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
            $"{unreachable.Count} public types are handed back by no public signature and "
                + "constructed by no caller — make them internal:\n  "
                + string.Join("\n  ", unreachable));
    }

    /// <summary>
    /// Nodes and stages come from the call or step that produces them, so only a handle a
    /// caller creates offers a constructor; a primary constructor or none at all publishes
    /// one silently (#487). An abstract base is the derivation gate's, below.
    /// </summary>
    [Fact]
    public void ExportedConcreteType_IsConstructibleOnlyWhenAllowlisted()
    {
        // By type rather than by name: renaming or unexporting one of these
        // fails to compile here instead of silently shrinking what is permitted.
        HashSet<Type> constructibleByDesign =
        [
            typeof(BindValue), typeof(Cte), typeof(DbColumn), typeof(DbColumnMetadataAttribute),
            typeof(DbTable), typeof(DerivedTable),
        ];

        List<string> constructible = [.. typeof(Sql).Assembly.GetExportedTypes()
            .Where(t => t.IsClass && !t.IsAbstract && !constructibleByDesign.Contains(t))
            .Where(HasConstructorReachableFromOutside)
            .Select(t => t.Name)
            .OrderBy(n => n, StringComparer.Ordinal)];

        Assert.True(
            constructible.Count == 0,
            $"{constructible.Count} public types can be constructed from outside the "
                + "assembly — make each constructor internal, or, where a caller creates "
                + "the type, name it in this test's allowlist:\n  "
                + string.Join("\n  ", constructible));
    }

    /// <summary>
    /// An unsealed class off the allowlist is closed to a foreign subclass only by a <c>private
    /// protected</c> constructor or an <c>internal abstract</c> member (CS0534), so deriving is
    /// decided here once (#492); <see cref="BindValue"/> is open for its array subclass alone.
    /// </summary>
    [Fact]
    public void ExportedOpenType_InRootNamespace_IsDerivableOnlyWhenAllowlisted()
    {
        // By type rather than by name: renaming or unexporting one of these
        // fails to compile here instead of silently shrinking what is permitted.
        HashSet<Type> derivableByDesign =
            [typeof(DbTableBase), typeof(CteBase), typeof(DerivedTableBase), typeof(BindValue)];

        List<string> derivable = [.. typeof(Sql).Assembly.GetExportedTypes()
            // A static class is abstract and sealed; only an unsealed class is derivable.
            .Where(t => t.Namespace == RootNamespace && t.IsClass && !t.IsSealed)
            .Where(t => !derivableByDesign.Contains(t))
            .Where(IsExternallyDerivable)
            .Select(t => t.Name)
            .OrderBy(n => n, StringComparer.Ordinal)];

        Assert.True(
            derivable.Count == 0,
            $"{derivable.Count} unsealed public types in {RootNamespace} can be derived from "
                + "outside the assembly — seal a concrete class, make an abstract base's "
                + "constructor private protected, or, where deriving is the documented use "
                + "or an in-assembly subclass needs the class open, name the type in this "
                + "test's allowlist and say which in its XML docs:\n  "
                + string.Join("\n  ", derivable));
    }

    /// <summary>
    /// No companion signature hands a helper back, so a companion's exported set is
    /// closed outright: a helper slipped to <c>public</c>, in its <c>.Internal</c>
    /// namespace or not, fails here instead of shipping as surface (#640).
    /// </summary>
    [Fact]
    public void CompanionAssemblies_ExportOnlyTheirStaticEntryPoints()
    {
        // By type rather than by name: renaming one fails to compile here.
        Type[] entryPoints =
        [
            typeof(SqlArtisan.Dapper.SqlMapper),
            typeof(SqlArtisan.Dapper.SqlParametersExtensions),
            typeof(SqlArtisan.ArrayBind.OracleArrayBind),
        ];

        List<string> unexpected = [.. entryPoints
            .Select(t => t.Assembly)
            .Distinct()
            .SelectMany(a => a.GetExportedTypes())
            .Where(t => !entryPoints.Contains(t) || HasConstructorReachableFromOutside(t))
            .Select(t => t.FullName!)
            .OrderBy(n => n, StringComparer.Ordinal)];

        Assert.True(
            unexpected.Count == 0,
            $"{unexpected.Count} exported companion types are not a static entry point listed "
                + "here — declare the type internal, or, if it is meant as surface, list it "
                + "and record it in docs/versioning.md:\n  "
                + string.Join("\n  ", unexpected));
    }

    /// <summary>
    /// A caller reaches a class with no constructor of its own, or an interface,
    /// only through a signature; static classes, enums and value types are named
    /// directly.
    /// </summary>
    private static bool IsHandedBackOnly(Type type) =>
        !type.IsValueType
        && !(type.IsAbstract && type.IsSealed)
        && !HasConstructorReachableFromOutside(type);

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
