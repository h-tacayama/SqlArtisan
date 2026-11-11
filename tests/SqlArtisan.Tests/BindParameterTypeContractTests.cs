using System.Collections.Immutable;
using System.Data;
using System.Net;
using System.Numerics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using static SqlArtisan.Sql;

namespace SqlArtisan.Tests;

// docs/versioning.md lets a 1.x add a Bind Parameter Type only because a converter registered
// for it keeps converting it, which the shipped SqlArtisan.Dapper relies on (#814). Both
// tests register converters process-wide, so they run alone.
[Collection("NonParallelTests")]
public class BindParameterTypeContractTests : IDisposable
{
    private const string Converted = "converted";

    // The 1.0 list, enums aside. A type added later goes in a list of its own, read after
    // the converter registry, never here (.claude/rules/public-api-design.md).
    private static readonly Type[] TypesBoundWithoutAConverterAt10 =
    [
        typeof(bool), typeof(char), typeof(string), typeof(DateTime), typeof(DateTimeOffset),
        typeof(DateOnly), typeof(TimeOnly), typeof(TimeSpan), typeof(sbyte), typeof(byte),
        typeof(short), typeof(ushort), typeof(int), typeof(uint), typeof(nint), typeof(nuint),
        typeof(long), typeof(ulong), typeof(float), typeof(double), typeof(decimal),
        typeof(Complex), typeof(Guid),
    ];

    private static readonly Type[] CandidateArguments =
        [typeof(byte), typeof(char), typeof(int), typeof(string), typeof(object)];

    private readonly TestTable _t = new("t");

    public void Dispose() => SqlArtisanConfig.ResetValueConverters();

    public static TheoryData<string, object> Candidates() => new()
    {
        { "byte[]", new byte[] { 1, 2 } },
        { "char[]", new[] { 'a', 'b' } },
        { "Half", (Half)1 },
        { "Int128", (Int128)1 },
        { "UInt128", (UInt128)1 },
        { "BigInteger", new BigInteger(1) },
        { "Rune", new Rune('a') },
        { "Uri", new Uri("https://example.com/") },
        { "Version", new Version(1, 2) },
        { "IPAddress", IPAddress.Parse("192.0.2.1") },
        { "JsonElement", JsonDocument.Parse("1").RootElement },
        { "ReadOnlyMemory<byte>", new ReadOnlyMemory<byte>([1]) },
        { "Memory<byte>", new Memory<byte>([1]) },
        { "ArraySegment<byte>", new ArraySegment<byte>([1]) },
    };

    [Fact]
    public void AddValueConverter_RejectsAsBoundWithoutAConverterExactlyThe10List()
    {
        MethodInfo register = typeof(BindParameterTypeContractTests).GetMethod(
            nameof(RegisterNull), BindingFlags.NonPublic | BindingFlags.Static)!;
        HashSet<Type> rejected = [];
        HashSet<Type> enums = [];
        List<string> otherFailures = [];
        int candidates = 0;

        foreach (Type type in SweptTypes())
        {
            candidates++;
            if ((Nullable.GetUnderlyingType(type) ?? type).IsEnum)
            {
                enums.Add(Nullable.GetUnderlyingType(type) ?? type);
            }

            try
            {
                register.MakeGenericMethod(type).Invoke(null, null);
            }
            catch (TargetInvocationException ex) when (ex.InnerException is ArgumentException a)
            {
                Type unwrapped = Nullable.GetUnderlyingType(type) ?? type;
                if (a.Message == $"{unwrapped} binds without a value converter.")
                {
                    rejected.Add(unwrapped);
                }
            }
            catch (TargetInvocationException ex)
            {
                otherFailures.Add($"{type}: {ex.InnerException?.GetType()}");
            }
            finally
            {
                SqlArtisanConfig.ResetValueConverters();
            }
        }

        // SqlArtisan.Dapper's AddTypeHandler catches ArgumentException alone.
        Assert.Empty(otherFailures);
        Assert.True(candidates > 4000, $"Only {candidates} types were swept.");
        Assert.Subset(rejected, enums);
        Assert.Equal(
            TypesBoundWithoutAConverterAt10.Select(type => type.FullName).Order(),
            rejected.Where(type => !type.IsEnum).Select(type => type.FullName).Order());
    }

    [Fact]
    public void AddValueConverter_AnyEnum_ThrowsArgumentException()
    {
        ArgumentException ex = Assert.Throws<ArgumentException>(
            () => SqlArtisanConfig.AddValueConverter<DayOfWeek>(day => day.ToString()));

        Assert.Equal("System.DayOfWeek binds without a value converter.", ex.Message);
    }

    [Theory]
    [MemberData(nameof(Candidates))]
    public void RegisteredCandidate_EveryValuePosition_BindsTheConvertedValue(
        string name, object value)
    {
        Register(value.GetType(), _ => Converted);
        TestTable t = new("t");

        Assert.Equal(Converted, Bind(value).Value);
        AssertBindsConverted(Select(value).From(t).Build(), name);
        AssertBindsConverted(Select(t.Name).From(t).Where(t.Code == value).Build(), name);
        AssertBindsConverted(Select(t.Name).From(t).Where(t.Code.In(value, value)).Build(), name);
        AssertBindsConverted(InsertInto(t, t.Code).Values(value).Build(), name);
        AssertBindsConverted(Update(t).Set(t.Code == value).Build(), name);
    }

    [Theory]
    [MemberData(nameof(Candidates))]
    public void RegisteredCandidate_BindArray_ThrowsArgumentException(string _, object value)
    {
        Register(value.GetType(), _ => Converted);
        MethodInfo bindArray = typeof(BindParameterTypeContractTests)
            .GetMethod(nameof(BindArrayOf), BindingFlags.NonPublic | BindingFlags.Static)!
            .MakeGenericMethod(value.GetType());

        TargetInvocationException ex = Assert.Throws<TargetInvocationException>(
            () => bindArray.Invoke(null, [value]));

        ArgumentException inner = Assert.IsType<ArgumentException>(ex.InnerException);
        Assert.Equal(
            $"BindArray does not apply the value converter for {value.GetType()}; "
                + "pass an array of the values it wraps.",
            inner.Message);
    }

    [Theory]
    [MemberData(nameof(Candidates))]
    public void RegisteredCandidate_ConverterReturningNullCompared_ThrowsArgumentException(
        string _, object value)
    {
        Register(value.GetType(), _ => null);

        ArgumentException ex = Assert.Throws<ArgumentException>(() => _t.Code > value);

        Assert.Equal(
            $"> never matches the NULL the value converter for {value.GetType()} returned; "
                + "test for NULL with .IsNull or .IsNotNull.",
            ex.Message);
    }

    private static void AssertBindsConverted(SqlStatement sql, string name)
    {
        List<object?> values = [];
        sql.Parameters.ForEach((_, bind) => values.Add(bind.Value));

        Assert.True(
            values.Count > 0 && values.All(v => Equals(v, Converted)),
            $"{name}: {sql.Text} bound [{string.Join(", ", values)}]");
    }

    private static void Register(Type type, Func<object, object?> convert) =>
        typeof(BindParameterTypeContractTests)
            .GetMethod(nameof(RegisterAs), BindingFlags.NonPublic | BindingFlags.Static)!
            .MakeGenericMethod(type)
            .Invoke(null, [convert]);

    private static void RegisterAs<T>(Func<object, object?> convert) =>
        SqlArtisanConfig.AddValueConverter<T>(value => convert(value!));

    private static BindArrayValue BindArrayOf<T>(object value) => BindArray([(T)value]);

    private static void RegisterNull<T>() => SqlArtisanConfig.AddValueConverter<T>(_ => null);

    private static IEnumerable<Type> SweptTypes()
    {
        Assembly[] assemblies =
        [
            typeof(object).Assembly,
            typeof(BigInteger).Assembly,
            typeof(JsonElement).Assembly,
            typeof(IPAddress).Assembly,
            typeof(Uri).Assembly,
            typeof(DataTable).Assembly,
            typeof(ImmutableArray).Assembly,
        ];
        Type[] exported = [.. assemblies.SelectMany(assembly => assembly.GetExportedTypes())];
        List<Type> types = [.. exported.Where(IsTypeArgument)];
        foreach (Type definition in exported.Where(
            type => type.IsGenericTypeDefinition && type.GetGenericArguments().Length == 1))
        {
            foreach (Type argument in CandidateArguments)
            {
                if (TryClose(definition, argument) is Type closed && IsTypeArgument(closed))
                {
                    types.Add(closed);
                }
            }
        }

        List<Type> nullables = [.. types
            .Where(type => type.IsValueType && Nullable.GetUnderlyingType(type) is null)
            .Select(type => typeof(Nullable<>).MakeGenericType(type))];
        List<Type> arrays = [.. types.Where(type => !type.IsAbstract || !type.IsSealed)
            .Select(type => type.MakeArrayType())];
        return types.Concat(nullables).Concat(arrays).Distinct();
    }

    private static bool IsTypeArgument(Type type) =>
        !type.ContainsGenericParameters
        && !type.IsByRefLike
        && !type.IsPointer
        && !type.IsByRef
        && type != typeof(void)
        && type.GetCustomAttribute<InlineArrayAttribute>() is null;

    private static Type? TryClose(Type definition, Type argument)
    {
        try
        {
            return definition.MakeGenericType(argument);
        }
        catch (ArgumentException)
        {
            // The argument breaks the definition's constraint.
            return null;
        }
    }
}
