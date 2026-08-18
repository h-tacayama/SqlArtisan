using System.Reflection;

namespace SqlArtisan.Tests;

/// <summary>
/// The inverse of the #244 boundary rule that <see cref="PublicSurfaceNamingTests"/>
/// exercises: that rule says every type a user must NAME lives in the root
/// namespace; this one says nothing else may become public surface by accident.
/// A type is only legitimately public because some root-namespace signature hands
/// it to the user — a <c>public</c> that no signature reaches is a slip of the
/// keyword, and from 1.0 it is a SemVer promise nobody meant to make.
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

    [Fact]
    public void ExportedType_InInternalNamespace_IsReachableFromRootNamespaceSignature()
    {
        Type[] exported = typeof(Sql).Assembly.GetExportedTypes();
        HashSet<Type> candidates = [.. exported.Where(t => t.Namespace == InternalNamespace)];

        HashSet<Type> reachable = [];
        Queue<Type> pending = new(exported
            .Where(t => t.Namespace == RootNamespace)
            .SelectMany(SignatureTypes)
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
            foreach (Type next in SignatureTypes(type)
                .Concat(type.GetInterfaces())
                .Append(type.BaseType)
                .Where(t => t is not null && candidates.Contains(t))!)
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
            $"{unreachable.Count} public types in {InternalNamespace} are named by no public "
                + $"signature — make them internal:\n  " + string.Join("\n  ", unreachable));
    }

    /// <summary>
    /// These types are reached through the <c>Sql.*</c> call, operator, or chain
    /// step that produces them, so none needs a public constructor. Two spellings
    /// publish one silently — a primary constructor, whose accessibility follows
    /// its class, and a class declaring no constructor at all.
    /// </summary>
    [Fact]
    public void ExportedType_InInternalNamespace_HasNoPublicConstructor()
    {
        List<string> constructible = [.. typeof(Sql).Assembly.GetExportedTypes()
            .Where(t => t.Namespace == InternalNamespace)
            .Where(t => t.GetConstructors(BindingFlags.Public | BindingFlags.Instance).Length > 0)
            .Select(t => t.Name)
            .OrderBy(n => n, StringComparer.Ordinal)];

        Assert.True(
            constructible.Count == 0,
            $"{constructible.Count} public types in {InternalNamespace} can be constructed "
                + "directly — give each an internal constructor:\n  "
                + string.Join("\n  ", constructible));
    }

    private static IEnumerable<Type> SignatureTypes(Type type)
    {
        const BindingFlags Declared = BindingFlags.Public
            | BindingFlags.Static
            | BindingFlags.Instance
            | BindingFlags.DeclaredOnly;

        IEnumerable<Type> methods = type
            .GetMethods(Declared)
            .SelectMany(m => m.GetParameters().Select(p => p.ParameterType).Append(m.ReturnType));
        IEnumerable<Type> constructors = type
            .GetConstructors(BindingFlags.Public | BindingFlags.Instance)
            .SelectMany(c => c.GetParameters().Select(p => p.ParameterType));
        IEnumerable<Type> properties = type.GetProperties(Declared).Select(p => p.PropertyType);
        IEnumerable<Type> fields = type.GetFields(Declared).Select(f => f.FieldType);

        return methods
            .Concat(constructors)
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
