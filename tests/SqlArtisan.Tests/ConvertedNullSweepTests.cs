using System.Reflection;
using static SqlArtisan.Sql;

namespace SqlArtisan.Tests;

// guards-and-empty-states.md, null arguments: a value converter's NULL is rejected as any
// operand of a predicate (#810). Every public member returning a SqlCondition, given one in
// a value slot or as its receiver, throws naming the converter; a slot added later fails here.
[Collection("NonParallelTests")]
public class ConvertedNullSweepTests : IDisposable
{
    private const string ConverterMessage = "the value converter for";

    // The NULL tests are the remedy the rejection names, so they take it as their receiver.
    private static readonly string[] NullTests = ["get_IsNull", "get_IsNotNull"];

    public ConvertedNullSweepTests()
    {
        SqlArtisanConfig.AddValueConverter<NoneId>(_ => null);
    }

    public void Dispose() => SqlArtisanConfig.ResetValueConverters();

    [Fact]
    public void Predicates_ConverterNullOperand_Throw()
    {
        List<string> violations = [];
        int checkedSlots = 0;

        foreach (MethodInfo method in PredicateMembers())
        {
            ParameterInfo[] parameters = method.GetParameters();
            for (int i = 0; i < parameters.Length; i++)
            {
                if (NullOf(parameters[i].ParameterType) is not { } injected)
                {
                    continue;
                }

                checkedSlots++;
                string? failure = Run(method, Receiver(method), Fill(method, i, injected));
                if (failure is not null)
                {
                    violations.Add($"{Describe(method)} :: {parameters[i].Name} — {failure}");
                }
            }

            if (!method.IsStatic && !NullTests.Contains(method.Name)
                && typeof(SqlExpression).IsAssignableFrom(method.DeclaringType))
            {
                checkedSlots++;
                string? failure = Run(method, Bind(new NoneId()), Fill(method, -1, null));
                if (failure is not null)
                {
                    violations.Add($"{Describe(method)} :: receiver — {failure}");
                }
            }
        }

        Assert.True(checkedSlots > 50, $"Only {checkedSlots} slots were checked.");
        Assert.True(
            violations.Count == 0,
            $"{violations.Count} predicate slot(s) take a converter's NULL:\n"
                + string.Join("\n", violations));
    }

    private static IEnumerable<MethodInfo> PredicateMembers()
    {
        foreach (Type type in typeof(Sql).Assembly.GetExportedTypes())
        {
            foreach (MethodInfo declared in type.GetMethods(
                BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance
                    | BindingFlags.DeclaredOnly))
            {
                MethodInfo method = declared.IsGenericMethodDefinition
                    ? declared.MakeGenericMethod(typeof(object))
                    : declared;
                if (typeof(SqlCondition).IsAssignableFrom(method.ReturnType))
                {
                    yield return method;
                }
            }
        }
    }

    private static object? NullOf(Type type) =>
        type == typeof(object) ? new NoneId()
        : type == typeof(object[]) ? new object[] { new NoneId() }
        : type == typeof(IReadOnlyCollection<object>) ? new List<object> { new NoneId() }
        : type == typeof(SqlExpression) ? Bind(new NoneId())
        : null;

    private static object? Receiver(MethodInfo method) =>
        method.IsStatic ? null
        : typeof(SqlExpression).IsAssignableFrom(method.DeclaringType) ? Column()
        : method.DeclaringType == typeof(MatchFunction) ? Match(Column())
        : throw new InvalidOperationException(
            $"No receiver for {method.DeclaringType}; add one to Receiver().");

    private static object?[] Fill(MethodInfo method, int injectedIndex, object? injected)
    {
        ParameterInfo[] parameters = method.GetParameters();
        object?[] args = new object?[parameters.Length];
        for (int i = 0; i < parameters.Length; i++)
        {
            Type type = parameters[i].ParameterType;
            args[i] = i == injectedIndex ? injected
                : type == typeof(SqlExpression) ? Column()
                : type == typeof(IReadOnlyCollection<object>) ? new List<object> { 1 }
                : FactoryGuardSweepTests.Filler(method, parameters[i]);
        }

        return args;
    }

    // Null when the NULL was rejected naming the converter, at the call or at Build().
    private static string? Run(MethodInfo method, object? receiver, object?[] args)
    {
        object? result;
        try
        {
            result = method.Invoke(receiver, args);
        }
        catch (TargetInvocationException ex)
        {
            return ex.InnerException?.Message.Contains(ConverterMessage) == true
                ? null
                : $"threw for another reason: {ex.InnerException?.Message}";
        }

        try
        {
            SqlStatement sql = Select(Column()).From(new DbTable("t", "a"))
                .Where((SqlCondition)result!).Build(Dbms.PostgreSql);
            return $"built {sql.Text}";
        }
        catch (ArgumentException ex) when (ex.Message.Contains(ConverterMessage))
        {
            return null;
        }
    }

    private static string Describe(MethodInfo method) =>
        $"{method.DeclaringType!.Name}.{FactoryGuardSweepTests.Signature(method)}";

    private static SqlExpression Column() => new DbTable("t", "a").Column("c");

    public readonly record struct NoneId(long Value);
}
