using static SqlArtisan.Sql;

namespace SqlArtisan.IntegrationTests.ValueReading;

/// <summary>The value-kind axis: every kind the trial brief names, plus four the
/// records cover (double, a whole double, a user enum, Sql.Null, a subquery), the short and
/// byte integers mutation testing showed missing (step 4), and the two
/// position-bound operands.</summary>
public enum ValueKind
{
    IntZero,
    IntPositive,
    IntNegative,
    Long,
    Short,
    Byte,
    Double,
    DoubleWhole,
    Decimal,
    Text,
    Char,
    Bool,
    FunctionArgEnum,
    UserEnum,
    DateTime,
    Guid,
    DateTimeOffset,
    TimeSpan,
    CSharpNull,
    DbNull,
    BindInt,
    BindText,
    BindNull,
    SqlNull,
    Column,
    Expression,
    Subquery,
    Quantified,
    Interval,
}

/// <summary>A user-declared enum, the shape #678 measured binding as an integer.</summary>
public enum SortField
{
    Name = 2,
}

internal static class ValueKinds
{
    internal static readonly ValueKind[] All = Enum.GetValues<ValueKind>();

    /// <summary>The integer every ordinal-shaped example uses: column 2 of the probe
    /// select list, which the discriminating data sorts and groups apart from a
    /// constant.</summary>
    internal const int Ordinal = 2;

    internal static object? Sample(ValueKind kind, DbTable t) => kind switch
    {
        ValueKind.IntZero => 0,
        ValueKind.IntPositive => Ordinal,
        ValueKind.IntNegative => -1,
        ValueKind.Long => (long)Ordinal,
        ValueKind.Short => (short)Ordinal,
        ValueKind.Byte => (byte)Ordinal,
        ValueKind.Double => 2.5,
        ValueKind.DoubleWhole => 2.0,
        ValueKind.Decimal => 2.5m,
        ValueKind.Text => "b",
        ValueKind.Char => 'b',
        ValueKind.Bool => true,
        ValueKind.FunctionArgEnum => DateTimePart.Year,
        ValueKind.UserEnum => SortField.Name,
        ValueKind.DateTime => new DateTime(2024, 1, 2, 3, 4, 5, DateTimeKind.Unspecified),
        ValueKind.Guid => new Guid("00000000-0000-0000-0000-000000000002"),
        ValueKind.DateTimeOffset => new DateTimeOffset(2024, 1, 2, 3, 4, 5, TimeSpan.Zero),
        ValueKind.TimeSpan => TimeSpan.FromHours(2),
        ValueKind.CSharpNull => null,
        ValueKind.DbNull => DBNull.Value,
        ValueKind.BindInt => Bind(Ordinal),
        ValueKind.BindText => Bind("b"),
        ValueKind.BindNull => BindNull(),
        ValueKind.SqlNull => Null,
        ValueKind.Column => t.Column("b"),
        ValueKind.Expression => t.Column("b") + t.Column("id"),
        ValueKind.Subquery => Select(Max(new DbTable("vr_s", "s").Column("b")))
            .From(new DbTable("vr_s", "s")),
        ValueKind.Quantified => Any(Select(new DbTable("vr_s", "s").Column("b"))
            .From(new DbTable("vr_s", "s"))),
        ValueKind.Interval => Interval(1, DateTimePart.Day),
        _ => throw new ArgumentOutOfRangeException(nameof(kind)),
    };

    /// <summary>The CLR type a sample has at the call site, for the compile-stop
    /// check (a C# null is assignable to any reference or nullable type).</summary>
    internal static Type? StaticType(ValueKind kind) =>
        Sample(kind, new DbTable("vr_t", "t"))?.GetType();

    private static readonly Dictionary<Type, Type[]> ImplicitNumeric = new()
    {
        [typeof(int)] = [typeof(long), typeof(float), typeof(double), typeof(decimal)],
        [typeof(long)] = [typeof(float), typeof(double), typeof(decimal)],
        [typeof(short)] = [typeof(int), typeof(long), typeof(float), typeof(double),
            typeof(decimal)],
        [typeof(byte)] = [typeof(short), typeof(ushort), typeof(int), typeof(uint), typeof(long),
            typeof(ulong), typeof(float), typeof(double), typeof(decimal)],
        [typeof(char)] = [typeof(int), typeof(long), typeof(float), typeof(double),
            typeof(decimal), typeof(ushort), typeof(uint), typeof(ulong)],
    };

    /// <summary>Whether a sample of <paramref name="kind"/> compiles as an argument of
    /// <paramref name="parameter"/> (an element of it for an array).</summary>
    internal static bool Compiles(ValueKind kind, Type parameter)
    {
        Type target = Nullable.GetUnderlyingType(parameter) ?? parameter;
        if (target.IsArray && target != typeof(object[][]))
        {
            target = target.GetElementType()!;
        }

        if (target.IsGenericType && target.GetGenericTypeDefinition() == typeof(IReadOnlyCollection<>))
        {
            target = target.GetGenericArguments()[0];
        }

        if (target == typeof(object[][]) || target == typeof(IEnumerable<object[]>)
            || target == typeof((object, object)) || target == typeof((object, object)[])
            || target.IsGenericParameter || target.ContainsGenericParameters)
        {
            return true; // each element is an object slot
        }

        Type? source = StaticType(kind);
        if (source is null)
        {
            return !parameter.IsValueType || Nullable.GetUnderlyingType(parameter) is not null;
        }

        return target.IsAssignableFrom(source)
            || (ImplicitNumeric.TryGetValue(source, out Type[]? widen) && widen.Contains(target));
    }
}
