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
    /// A factory returns its node's own type only for what that type carries (ADR 0027): a
    /// class here declares a member, is taken by a parameter, or is another public type's
    /// base, so any other node stays internal.
    /// </summary>
    [Fact]
    public void ExportedClass_CarriesAMemberAParameterOrADerivedType()
    {
        // By type rather than by name: BindArrayValue's placement is recorded in
        // public-api-design.md (an execution layer type-tests for it).
        HashSet<Type> recorded = [typeof(BindArrayValue)];
        Type[] exported = typeof(Sql).Assembly.GetExportedTypes();
        HashSet<Type> parameters = [.. exported
            .SelectMany(t => t.GetMethods(Declared).Cast<MethodBase>()
                .Concat(t.GetConstructors(BindingFlags.Public | BindingFlags.Instance)))
            .SelectMany(m => m.GetParameters())
            .SelectMany(p => Unwrap(p.ParameterType))];
        HashSet<Type> bases = [.. exported.Select(t => t.BaseType).OfType<Type>()];

        List<string> hollow = [.. exported
            .Where(t => t.IsClass && !(t.IsAbstract && t.IsSealed) && !recorded.Contains(t))
            .Where(t => !DeclaresMember(t) && !parameters.Contains(t) && !bases.Contains(t))
            .Select(t => t.Name)
            .OrderBy(n => n, StringComparer.Ordinal)];

        Assert.True(
            hollow.Count == 0,
            $"{hollow.Count} public classes declare no member, are taken by no parameter and "
                + "are no public type's base — make each internal and return its nearest "
                + "public base:\n  " + string.Join("\n  ", hollow));
    }

    /// <summary>
    /// The other half of the return-type rule (ADR 0027): a declared return wider than the
    /// node's nearest public type hides the members that type offers, and narrowing it back
    /// after 1.0 is a binary break, so each SqlPart-returning member is held to its node.
    /// </summary>
    [Fact]
    public void SqlPartReturn_IsItsNodesNearestPublicType()
    {
        // Each only throws, rejecting `string + x`, so it builds no node to judge it by.
        string[] buildNoNode =
        [
            "ExpressionAlias.op_Addition", "QuantifiedExpression.op_Addition",
            "QuantifiedSubquery.op_Addition",
        ];

        List<string> widened = [];
        HashSet<string> unjudged = [];
        // Interfaces stay in: a default method (ISubquery.As) has a body to judge.
        foreach (MethodInfo method in typeof(Sql).Assembly.GetExportedTypes()
            .SelectMany(t => t.GetMethods(Declared))
            .Where(m => !m.IsAbstract && typeof(SqlPart).IsAssignableFrom(m.ReturnType)))
        {
            string name = $"{method.DeclaringType!.Name}.{method.Name}";
            if (BuiltNode(method, depth: 0) is not { } node)
            {
                unjudged.Add(name);
            }
            else if (NearestPublicType(node) != method.ReturnType)
            {
                widened.Add($"{name} -> {method.ReturnType.Name}, builds {node.Name} "
                    + $"(nearest public {NearestPublicType(node).Name})");
            }
        }

        string[] unrecorded = [.. unjudged.Except(buildNoNode).OrderBy(n => n)];
        string[] stale = [.. buildNoNode.Except(unjudged).OrderBy(n => n)];

        Assert.True(
            widened.Count == 0 && unrecorded.Length == 0 && stale.Length == 0,
            $"Return types other than the node's nearest public type (return that type): "
                + $"[{string.Join(", ", widened.Distinct().OrderBy(n => n))}]; members whose "
                + $"node the IL scan cannot find (record them): [{string.Join(", ", unrecorded)}]; "
                + $"stale records: [{string.Join(", ", stale)}]");
    }

    /// <summary>
    /// Nodes and stages come from the call or step that produces them, so only a handle a
    /// caller creates or derives from offers a constructor; a primary constructor or none
    /// at all publishes one silently, <c>protected</c> on an abstract base (#487, #492).
    /// </summary>
    [Fact]
    public void ExportedType_IsConstructibleOnlyWhenAllowlisted()
    {
        // By type rather than by name: renaming or unexporting one of these
        // fails to compile here instead of silently shrinking what is permitted.
        HashSet<Type> constructibleByDesign =
        [
            typeof(BindValue), typeof(Cte), typeof(CteBase), typeof(DbColumn),
            typeof(DbColumnMetadataAttribute), typeof(DbTable), typeof(DbTableBase),
            typeof(DerivedTable), typeof(DerivedTableBase),
        ];

        List<string> constructible = [.. typeof(Sql).Assembly.GetExportedTypes()
            .Where(t => t.IsClass && !(t.IsAbstract && t.IsSealed))
            .Where(t => !constructibleByDesign.Contains(t))
            .Where(HasConstructorReachableFromOutside)
            .Select(t => t.Name)
            .OrderBy(n => n, StringComparer.Ordinal)];

        Assert.True(
            constructible.Count == 0,
            $"{constructible.Count} public types can be constructed or derived from outside "
                + "the assembly — make each constructor internal, or private protected on an "
                + "abstract base, or, where a caller creates or derives from the type, name it "
                + "in this test's allowlist:\n  "
                + string.Join("\n  ", constructible));
    }

    /// <summary>
    /// The constructor gate closes every class off its allowlist; this one keeps the handles on
    /// it a caller only constructs sealed, so deriving stays the four bases' alone (#492) and
    /// <see cref="BindValue"/> is open for its array subclass alone.
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
            .Where(HasConstructorReachableFromOutside)
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
    /// The constructor and derivation gates turn on one question — can another assembly
    /// reach a constructor? — so they read the answer from here rather than each
    /// spelling out the three accessibilities that give it.
    /// </summary>
    private static bool HasConstructorReachableFromOutside(Type type) =>
        type.GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
            .Any(c => c.IsPublic || c.IsFamily || c.IsFamilyOrAssembly);

    /// <summary>
    /// Output positions only — what a caller can end up holding. A parameter type
    /// does not count: with no constructor reachable from outside the assembly, a
    /// type that is only ever accepted is one no caller can obtain to pass.
    /// </summary>
    private static IEnumerable<Type> HandedBackTypes(Type type)
    {
        IEnumerable<Type> returns = type.GetMethods(Declared).Select(m => m.ReturnType);
        IEnumerable<Type> properties = type.GetProperties(Declared).Select(p => p.PropertyType);
        IEnumerable<Type> fields = type.GetFields(Declared).Select(f => f.FieldType);

        return returns
            .Concat(properties)
            .Concat(fields)
            .SelectMany(Unwrap);
    }

    private const BindingFlags Declared = BindingFlags.Public
        | BindingFlags.Static
        | BindingFlags.Instance
        | BindingFlags.DeclaredOnly;

    // An override restates a base member, so it adds nothing a caller could not
    // already reach through the base the factory would return instead.
    private static bool DeclaresMember(Type type) =>
        type.GetMethods(Declared).Any(m => m.GetBaseDefinition() == m)
        || type.GetProperties(Declared).Any(p => p.GetMethod?.GetBaseDefinition() == p.GetMethod)
        || type.GetFields(Declared).Length > 0
        || type.GetEvents(Declared).Length > 0;

    private static Type NearestPublicType(Type type)
    {
        while (!type.IsVisible)
        {
            type = type.BaseType!;
        }

        return type;
    }

    // The node is the last constructor the IL calls that yields the return type, or, for a
    // member that delegates, what the last same-assembly call yielding it builds; a member
    // that only passes an argument through builds none and is not judged here.
    private static Type? BuiltNode(MethodInfo method, int depth)
    {
        byte[]? il = method.GetMethodBody()?.GetILAsByteArray();
        if (il is null || depth > 4)
        {
            return null;
        }

        Type? node = null;
        MethodInfo? delegated = null;
        for (int i = 0; i + 4 < il.Length; i++)
        {
            MethodBase? target = il[i] is 0x73 or 0x28 or 0x6F
                ? ResolveMethod(method.Module, BitConverter.ToInt32(il, i + 1))
                : null;
            if (il[i] == 0x73 && target is ConstructorInfo ctor
                && method.ReturnType.IsAssignableFrom(ctor.DeclaringType))
            {
                node = ctor.DeclaringType;
            }
            else if (il[i] != 0x73 && target is MethodInfo callee
                && callee.Module == method.Module && callee != method
                && method.ReturnType.IsAssignableFrom(callee.ReturnType))
            {
                delegated = callee;
            }
        }

        return node ?? (delegated is null ? null : BuiltNode(delegated, depth + 1));
    }

    private static MethodBase? ResolveMethod(Module module, int token)
    {
        try
        {
            return module.ResolveMethod(token);
        }
        catch (ArgumentException)
        {
            return null;
        }
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
