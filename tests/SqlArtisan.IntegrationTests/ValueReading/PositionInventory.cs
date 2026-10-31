using System.Reflection;

namespace SqlArtisan.IntegrationTests.ValueReading;

/// <summary>
/// The position axis, enumerated from the shipped assembly by reflection rather than
/// from memory: every parameter of a public member of an exported type whose type can
/// carry a value (an object, a SqlExpression, a numeric literal, a string, an enum, a
/// value-typed generic). Structural parameters (tables, columns, sort orders, builders)
/// are counted separately and are not positions of this hazard type.
/// </summary>
public static class PositionInventory
{
    public sealed record Slot(
        string DeclaringType,
        string Member,
        string Signature,
        string Parameter,
        string ParameterType,
        SlotKind Kind);

    public enum SlotKind
    {
        /// <summary>`object` — resolved at run time by a resolver.</summary>
        Object,

        /// <summary>`object[]` (usually `params`).</summary>
        ObjectArray,

        /// <summary>`(object, object)` pairs and arrays of them.</summary>
        ObjectPair,

        /// <summary>`IEnumerable&lt;object[]&gt;` / `object[][]` rows.</summary>
        ObjectRows,

        /// <summary>A generic `T`, `T[]` or collection of `T` (In/NotIn/BindArray).</summary>
        Generic,

        /// <summary>`SqlExpression` or a subclass of it.</summary>
        Expression,

        /// <summary>A numeric primitive printed into the text or bound.</summary>
        Numeric,

        /// <summary>A `string` (identifier, forced literal or bound value).</summary>
        Text,

        /// <summary>A `char`.</summary>
        Character,

        /// <summary>A `bool` flag.</summary>
        Boolean,

        /// <summary>An enum declared in SqlArtisan.</summary>
        Enum,

        /// <summary>`ISubquery`.</summary>
        Subquery,

        /// <summary>An IIncompleteExpression type (INTERVAL, ANY/ALL/SOME operand).</summary>
        PositionBound,
    }

    private static readonly HashSet<string> ObjectMembers =
        ["Equals", "GetHashCode", "ToString", "GetType"];

    public static IReadOnlyList<Slot> Slots { get; } = Enumerate();

    internal static int StructuralParameterCount { get; private set; }

    internal static int MemberCount { get; private set; }

    private static List<Slot> Enumerate()
    {
        List<Slot> slots = new();
        HashSet<MethodBase> members = new();
        int structural = 0;

        foreach (Type type in typeof(Sql).Assembly.GetExportedTypes().OrderBy(t => t.FullName))
        {
            const BindingFlags flags = BindingFlags.Public | BindingFlags.Instance
                | BindingFlags.Static | BindingFlags.DeclaredOnly;

            IEnumerable<MethodBase> declared = type.GetMethods(flags)
                .Where(m => !ObjectMembers.Contains(m.Name) && !m.Name.StartsWith("get_"))
                .Cast<MethodBase>()
                .Concat(type.IsAbstract && type.IsSealed
                    ? []
                    : type.GetConstructors(BindingFlags.Public | BindingFlags.Instance));

            foreach (MethodBase member in declared.OrderBy(m => m.ToString(), StringComparer.Ordinal))
            {
                if (member.GetCustomAttribute<ObsoleteAttribute>() is { IsError: true })
                {
                    continue; // a compile error by design, the "compile stop" outcome
                }

                foreach (ParameterInfo p in member.GetParameters())
                {
                    SlotKind? kind = Classify(p.ParameterType);
                    if (kind is null)
                    {
                        structural++;
                        continue;
                    }

                    members.Add(member);
                    slots.Add(new Slot(
                        type.Name,
                        member is ConstructorInfo ? ".ctor" : member.Name,
                        Signature(member),
                        p.Name ?? "?",
                        TypeName(p.ParameterType),
                        kind.Value));
                }
            }
        }

        StructuralParameterCount = structural;
        MemberCount = members.Count;
        return slots;
    }

    internal static SlotKind? Classify(Type t)
    {
        if (t.IsByRef)
        {
            t = t.GetElementType()!;
        }

        Type u = Nullable.GetUnderlyingType(t) ?? t;

        if (u == typeof(object))
        {
            return SlotKind.Object;
        }

        if (u == typeof(object[]))
        {
            return SlotKind.ObjectArray;
        }

        if (u == typeof((object, object)) || u == typeof((object, object)[]))
        {
            return SlotKind.ObjectPair;
        }

        if (u == typeof(object[][]) || u == typeof(IEnumerable<object[]>))
        {
            return SlotKind.ObjectRows;
        }

        if (u.IsGenericParameter
            || (u.IsArray && u.GetElementType()!.IsGenericParameter)
            || (u.IsGenericType && u.GetGenericArguments().Any(a => a.IsGenericParameter)))
        {
            return SlotKind.Generic;
        }

        if (typeof(SqlExpression).IsAssignableFrom(u)
            || (u.IsArray && typeof(SqlExpression).IsAssignableFrom(u.GetElementType())))
        {
            return SlotKind.Expression;
        }

        if (u.GetInterfaces().Any(i => i.Name == "IIncompleteExpression"))
        {
            return SlotKind.PositionBound;
        }

        if (u == typeof(ISubquery))
        {
            return SlotKind.Subquery;
        }

        if (u == typeof(string))
        {
            return SlotKind.Text;
        }

        if (u == typeof(string[]))
        {
            return SlotKind.Text;
        }

        if (u == typeof(char))
        {
            return SlotKind.Character;
        }

        if (u == typeof(bool))
        {
            return SlotKind.Boolean;
        }

        if (u.IsEnum && u.Assembly == typeof(Sql).Assembly)
        {
            return SlotKind.Enum;
        }

        if (u.IsPrimitive || u == typeof(decimal))
        {
            return SlotKind.Numeric;
        }

        return null;
    }

    internal static string Signature(MethodBase m) =>
        $"{(m is ConstructorInfo ? m.DeclaringType!.Name : m.Name)}("
            + string.Join(", ", m.GetParameters().Select(p => TypeName(p.ParameterType))) + ")";

    internal static string TypeName(Type t)
    {
        if (t.IsByRef)
        {
            return TypeName(t.GetElementType()!);
        }

        if (t.IsArray)
        {
            return TypeName(t.GetElementType()!) + "[]";
        }

        if (t.IsGenericType)
        {
            string name = t.Name[..t.Name.IndexOf('`')];
            return $"{name}<{string.Join(", ", t.GetGenericArguments().Select(TypeName))}>";
        }

        return t.Name;
    }
}
