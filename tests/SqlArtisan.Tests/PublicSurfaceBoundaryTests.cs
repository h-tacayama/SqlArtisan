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
