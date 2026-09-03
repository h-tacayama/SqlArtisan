using System.Data;
using System.Reflection;
using System.Text;
using SqlArtisan.Internal;
using static SqlArtisan.Sql;

namespace SqlArtisan.Tests;

// Mechanizes the null-guard boundary in guards-and-empty-states.md: a
// degenerate argument (null, empty string, empty array, null element) fed to
// any public Sql factory must either throw — eagerly or at Build(), either is
// loud — or build SQL recorded verbatim in AcceptedSilentBuilds. A silent
// build outside that catalog is the class both 1.0 audit rounds kept finding
// one instance at a time (empty DbColumn name, WITH "c" AS (), INTO :); this
// sweep finds the next instance mechanically. Outside its reach: instance
// members (e.g. SqlExpression.In, .Over), public constructors, and the
// factories whose return type TryBuild does not embed — that last set is a
// recorded ledger (UnembeddedReturnTypes), so it cannot grow silently;
// extend TryBuild's switch to shrink it.
public class FactoryGuardSweepTests
{
    private const string ResolverNullValueMessage =
        "Value cannot be null. Use Sql.Null to represent SQL NULL.";

    // Key: "Signature :: injection"; value: the exact SQL the degenerate call
    // builds — an entry asserts the acceptance is deliberate (a smaller legal
    // call, or a quoted/literal position where whitespace is RD-004's).
    private static readonly Dictionary<string, string> AcceptedSilentBuilds = new()
    {
        ["Case(SearchedCaseWhenClause, SearchedCaseWhenClause[]) :: whenClauses=[]"] =
            "SELECT CASE WHEN (\"a\".c > :0) THEN :1 END",
        ["Coalesce(Object, Object, Object[]) :: others=[]"] = "SELECT COALESCE(:0, :1)",
        ["Concat(Object, Object, Object, Object[]) :: others=[]"] = "SELECT CONCAT(:0, :1, :2)",
        ["ConcatWs(Object, Object, Object, Object[]) :: others=[]"] = "SELECT CONCAT_WS(:0, :1, :2)",
        ["Date(Object, Object, Object[]) :: modifiers=[]"] = "SELECT DATE(:0, :1)",
        ["Datetime(Object, Object[]) :: modifiers=[]"] = "SELECT DATETIME(:0)",
        ["DoublePipe(Object, Object, Object[]) :: others=[]"] = "SELECT (:0 || :1)",
        ["Grouping(Object, Object, Object[]) :: others=[]"] = "SELECT GROUPING(:0, :1)",
        ["GroupingId(Object, Object[]) :: others=[]"] = "SELECT GROUPING_ID(:0)",
        ["Julianday(Object, Object[]) :: modifiers=[]"] = "SELECT JULIANDAY(:0)",
        ["Strftime(Object, Object, Object[]) :: modifiers=[]"] = "SELECT STRFTIME(:0, :1)",
        // An empty STRING_AGG/GROUP_CONCAT separator is meaningful (concatenate
        // adjacently), unlike the empty identifiers this sweep exists to reject.
        ["StringAgg(Object, String) :: separator=\"\""] = "SELECT STRING_AGG(:0, '')",
        ["StringAgg(Object, String, OrderByClause) :: separator=\"\""] =
            "SELECT STRING_AGG(:0, '' ORDER BY \"a\".c)",
        ["Separator(String) :: separator=\"\""] = "SELECT GROUP_CONCAT(`x`.c SEPARATOR '') FROM e `x`",
        // Leading-parameter + empty params tail is the documented smaller call
        // (Coalesce/Concat/Grouping already catalog this shape) — not a dropped
        // clause, since the tail was never the sole carrier of meaning.
        ["Cube(Object, Object[]) :: elements=[]"] = "SELECT \"x\".c FROM e \"x\" GROUP BY CUBE(:0)",
        ["Rollup(Object, Object[]) :: elements=[]"] = "SELECT \"x\".c FROM e \"x\" GROUP BY ROLLUP(:0)",
        ["GroupingSets(GroupingSet, GroupingSet[]) :: sets=[]"] =
            "SELECT \"x\".c FROM e \"x\" GROUP BY GROUPING SETS(\"a\".c)",
        // Group() with zero columns is the documented grand-total row, not a
        // dropped column list.
        ["Group(Object[]) :: columns=[]"] = "SELECT \"x\".c FROM e \"x\" GROUP BY GROUPING SETS(())",
        // SqlHints.Format deliberately coalesces null to "" and elides an empty
        // hint (SqlHints.cs) — hints are a non-semantic query-plan decoration
        // with an existing no-hints spelling (bare Select(...)), unlike the
        // clauses above whose absence changes the result.
        ["Hints(String) :: hints=null"] = "SELECT c FROM e",
        ["Hints(String) :: hints=\"\""] = "SELECT c FROM e",
        // Whitespace in a quoted alias or a string-literal position (sequence
        // names via CURRVAL('...'), JSON paths, ts configs, separators, the
        // INTERVAL literal, raw hint text) is the engine's to judge (RD-004);
        // the bare-token positions (CAST type, NEXT VALUE FOR name) throw.
        ["Currval(String) :: sequenceName=\" \""] = "SELECT CURRVAL(' ')",
        ["Nextval(String) :: sequenceName=\" \""] = "SELECT NEXTVAL(' ')",
        ["Hints(String) :: hints=\" \""] = "SELECT   c FROM e",
        ["IntervalLiteral(String) :: text=\" \""] = "SELECT INTERVAL ' '",
        ["IntervalLiteral(String, IntervalField) :: value=\" \""] = "SELECT INTERVAL ' ' YEAR",
        ["JsonExtract(Object, String) :: path=\" \""] = "SELECT JSON_EXTRACT(:0, ' ')",
        ["JsonQuery(Object, String) :: path=\" \""] = "SELECT JSON_QUERY(:0, ' ')",
        ["JsonValue(Object, String) :: path=\" \""] = "SELECT JSON_VALUE(:0, ' ')",
        ["PlaintoTsquery(String, Object) :: config=\" \""] = "SELECT PLAINTO_TSQUERY(' ', :0)",
        ["ToTsquery(String, Object) :: config=\" \""] = "SELECT TO_TSQUERY(' ', :0)",
        ["ToTsvector(String, Object) :: config=\" \""] = "SELECT TO_TSVECTOR(' ', :0)",
        ["Separator(String) :: separator=\" \""] =
            "SELECT GROUP_CONCAT(`x`.c SEPARATOR ' ') FROM e `x`",
        ["StringAgg(Object, String) :: separator=\" \""] = "SELECT STRING_AGG(:0, ' ')",
        ["StringAgg(Object, String, OrderByClause) :: separator=\" \""] =
            "SELECT STRING_AGG(:0, ' ' ORDER BY \"a\".c)",
        ["Values(String, String[], Object[][]) :: alias=\" \""] =
            "SELECT c FROM (VALUES (:0)) \" \" (c1)",
    };

    [Fact]
    public void Factories_DegenerateArguments_ThrowOrAreCataloged()
    {
        List<string> violations = new();
        HashSet<string> usedCatalogKeys = new();

        foreach (MethodInfo method in SweepableMethods())
        {
            foreach ((int index, string label, object? injected) in Injections(method))
            {
                string key = $"{Signature(method)} :: {label}";
                object?[] args = BuildArguments(method, index, injected);

                object? result;
                try
                {
                    result = Invoke(method, args);
                }
                catch (TargetInvocationException ex)
                {
                    if (ex.InnerException is NullReferenceException && IsElementInjection(label))
                    {
                        violations.Add(
                            $"BARE NRE {key} — a null element owes a named ArgumentNullException.");
                    }

                    // A ParamName absent from the invoked signature leaks an internal
                    // name (#497); the expression-resolver family's position names are exempt.
                    if (ex.InnerException is ArgumentException { ParamName: { } paramName } argEx
                        && !argEx.Message.StartsWith(ResolverNullValueMessage, StringComparison.Ordinal)
                        && System.Array.TrueForAll(
                            method.GetParameters(), p => p.Name != paramName))
                    {
                        violations.Add(
                            $"FOREIGN PARAMNAME {key} — '{paramName}' is not a parameter "
                                + "of the invoked signature.");
                    }

                    continue; // eager throw — loud, OK
                }

                string? sql;
                try
                {
                    sql = TryBuild(result);
                }
                catch (NullReferenceException) when (IsElementInjection(label))
                {
                    violations.Add(
                        $"BARE NRE {key} — a null element owes a named ArgumentNullException.");
                    continue;
                }
                catch (NullReferenceException)
                {
                    continue; // single-reference-parameter NRE — the loud-failure exemption
                }

                if (sql is null)
                {
                    continue; // threw at Build() (loud, OK) or not embeddable
                }

                if (AcceptedSilentBuilds.TryGetValue(key, out string? expected))
                {
                    usedCatalogKeys.Add(key);
                    if (sql != expected)
                    {
                        violations.Add($"CATALOG MISMATCH {key}\n  expected: {expected}\n  actual:   {sql}");
                    }
                }
                else
                {
                    violations.Add($"SILENT BUILD {key}\n  built: {sql}");
                }
            }
        }

        foreach (string staleKey in AcceptedSilentBuilds.Keys.Except(usedCatalogKeys))
        {
            violations.Add($"STALE CATALOG ENTRY {staleKey} — the case no longer builds silently; remove it.");
        }

        Assert.True(
            violations.Count == 0,
            $"{violations.Count} factory guard sweep violation(s):\n\n{string.Join("\n\n", violations)}");
    }

    // Mirrors TryBuild's switch: a return type assignable to one of these is
    // reached by the sweep. Everything else must sit in UnembeddedReturnTypes,
    // so the blind spot is a recorded list, not a silent fall-through.
    private static readonly Type[] EmbeddableReturnTypes =
    [
        typeof(SqlStatement), typeof(ISqlBuilder), typeof(ExpressionAlias), typeof(SqlExpression),
        typeof(SqlCondition), typeof(TableReference), typeof(CommonTableExpression),
        typeof(SortOrder), typeof(SqlHints), typeof(GroupingSet), typeof(GroupingElement),
        typeof(SeparatorClause), typeof(IWithBuilderWith), typeof(OfClause),
    ];

    private static readonly string[] UnembeddedReturnTypes =
    [
        // Pending types awaiting a completing call (.Over/.WithinGroup/.Against/
        // .Then/.Values/.Set/.Using) — the incomplete-construct category.
        "AnalyticCumeDistFunction", "AnalyticDenseRankFunction", "AnalyticFirstValueFunction",
        "AnalyticLagFunction", "AnalyticLastValueFunction", "AnalyticLeadFunction",
        "AnalyticNthValueFunction", "AnalyticNtileFunction", "AnalyticPercentRankFunction",
        "AnalyticRankFunction", "AnalyticRowNumberFunction", "ListaggFunction", "MatchFunction",
        "PercentileContFunction", "PercentileDiscFunction", "SearchedCaseWhenCondition",
        "SimpleCaseWhenExpression", "IInsertBuilderColumnsOutput", "IInsertBuilderTable",
        "IInsertIgnoreBuilderColumns", "IInsertIgnoreBuilderTable", "IMergeBuilderTarget",
        "IUpdateBuilderUpdate",
        // Complete argument/clause objects TryBuild does not yet embed — a real
        // coverage gap, not a rejected category; extend TryBuild to shrink it.
        "DbSequence", "DistinctOnKeyword", "FrameBound", "IntervalField", "OrderByClause",
        "PartitionByClause", "TopClause", "WaitBehavior",
    ];

    [Fact]
    public void ReturnTypes_AreEmbeddableOrRecorded()
    {
        HashSet<string> unembedded = [];
        foreach (MethodInfo method in SweepableMethods())
        {
            if (!EmbeddableReturnTypes.Any(t => t.IsAssignableFrom(method.ReturnType)))
            {
                unembedded.Add(method.ReturnType.Name);
            }
        }

        string[] unrecorded = [.. unembedded.Except(UnembeddedReturnTypes).OrderBy(n => n)];
        string[] stale = [.. UnembeddedReturnTypes.Except(unembedded).OrderBy(n => n)];

        Assert.True(
            unrecorded.Length == 0 && stale.Length == 0,
            $"Return types outside TryBuild's reach and not recorded (extend TryBuild or record "
                + $"them): [{string.Join(", ", unrecorded)}]; stale records: [{string.Join(", ", stale)}]");
    }

    private static IEnumerable<MethodInfo> SweepableMethods()
    {
        foreach (MethodInfo method in typeof(Sql)
            .GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Where(m => !m.IsSpecialName)
            .OrderBy(m => m.Name, StringComparer.Ordinal))
        {
            if (!method.IsGenericMethodDefinition)
            {
                yield return method;
                continue;
            }

            // BindArray<T> is the only generic family; close it with a
            // reference type so a null element is representable.
            MethodInfo closed;
            try
            {
                closed = method.MakeGenericMethod(typeof(object));
            }
            catch (ArgumentException)
            {
                closed = method.MakeGenericMethod(typeof(int));
            }

            yield return closed;
        }
    }

    // Element injections are the shapes the guards rule's element clause covers;
    // a whole-argument null on a single reference parameter keeps the loud-NRE
    // exemption, so only these labels turn an NRE into a sweep violation.
    private static bool IsElementInjection(string label) =>
        label.EndsWith("=[null]", StringComparison.Ordinal)
        || label.EndsWith("=[(null, 1)]", StringComparison.Ordinal);

    private static IEnumerable<(int Index, string Label, object? Injected)> Injections(MethodInfo method)
    {
        ParameterInfo[] parameters = method.GetParameters();
        for (int i = 0; i < parameters.Length; i++)
        {
            Type t = parameters[i].ParameterType;
            string name = parameters[i].Name!;

            if (t == typeof(string))
            {
                yield return (i, $"{name}=null", null);
                yield return (i, $"{name}=\"\"", "");
                // Whitespace separates the bare-token positions (must throw)
                // from quoted/literal ones, where it is the engine's to judge
                // (RD-004) and the build lands in the catalog.
                yield return (i, $"{name}=\" \"", " ");
            }
            else if (t == typeof(object))
            {
                yield return (i, $"{name}=null", null);
            }
            else if (t.IsArray && !t.GetElementType()!.IsValueType)
            {
                Type element = t.GetElementType()!;
                yield return (i, $"{name}=null", null);
                yield return (i, $"{name}=[]", System.Array.CreateInstance(element, 0));

                System.Array withNull = System.Array.CreateInstance(element, 1);
                yield return (i, $"{name}=[null]", withNull);

                if (element == typeof(string))
                {
                    System.Array withEmpty = new string[] { "" };
                    yield return (i, $"{name}=[\"\"]", withEmpty);
                }
            }
            else if (t == typeof((object, object)))
            {
                yield return (i, $"{name}=(null, 1)", ((object?)null, (object)1));
            }
            else if (t == typeof((object, object)[]))
            {
                yield return (i, $"{name}=null", null);
                yield return (i, $"{name}=[]", System.Array.Empty<(object, object)>());
                yield return (i, $"{name}=[(null, 1)]", new[] { ((object)null!, (object)1) });
            }
            else if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(IReadOnlyCollection<>)
                && !t.GetGenericArguments()[0].IsValueType)
            {
                yield return (i, $"{name}=null", null);
                yield return (i, $"{name}=[]", new List<object>());
                yield return (i, $"{name}=[null]", new List<object?> { null });
            }
            else if (!t.IsValueType && Filler(method, parameters[i]) is not null)
            {
                // Single typed reference parameter: the rule says a loud NRE
                // is acceptable here, but a silent build still is not.
                yield return (i, $"{name}=null", null);
            }
        }
    }

    private static object?[] BuildArguments(MethodInfo method, int injectedIndex, object? injected)
    {
        ParameterInfo[] parameters = method.GetParameters();
        object?[] args = new object?[parameters.Length];
        for (int i = 0; i < parameters.Length; i++)
        {
            args[i] = i == injectedIndex
                ? injected
                : Filler(method, parameters[i])
                    ?? throw new InvalidOperationException(
                        $"No filler for parameter type {parameters[i].ParameterType} "
                            + $"of {Signature(method)}; add one to Filler().");
        }

        return args;
    }

    // A valid value for every parameter type the Sql surface declares, so the
    // injected degenerate argument is the only invalid thing in the call.
    private static object? Filler(MethodInfo method, ParameterInfo parameter)
    {
        Type t = parameter.ParameterType;
        DbTable table = new("t", "a");

        if (t == typeof(object))
        {
            return 1;
        }

        if (t == typeof(string))
        {
            return "x";
        }

        if (t == typeof(int) || t == typeof(int?))
        {
            return 1;
        }

        if (t == typeof(double))
        {
            return 0.5;
        }

        if (t == typeof(bool))
        {
            return true;
        }

        if (t == typeof(DbType?))
        {
            return DbType.Int32;
        }

        if (t == typeof(DateTimePart))
        {
            // NUMTODSINTERVAL accepts only day-to-second units.
            return method.Name == "Numtodsinterval" ? DateTimePart.Day : DateTimePart.Year;
        }

        if (t == typeof(SearchModifier))
        {
            return SearchModifier.InNaturalLanguageMode;
        }

        if (t == typeof(RegexpOptions))
        {
            return RegexpOptions.CaseInsensitive;
        }

        if (t == typeof(object[]))
        {
            return new object[] { 1 };
        }

        if (t == typeof(object[][]))
        {
            return new object[][] { [1] };
        }

        if (t == typeof(string[]))
        {
            return new string[] { "c1" };
        }

        if (t == typeof((object, object)))
        {
            return ((object)1, (object)2);
        }

        if (t == typeof((object, object)[]))
        {
            return new[] { ((object)1, (object)2) };
        }

        if (t == typeof(DbTableBase))
        {
            return new DbTable("t");
        }

        if (t == typeof(DbColumn))
        {
            return table.Column("c");
        }

        if (t == typeof(DbColumn[]))
        {
            return new[] { new DbTable("t").Column("c") };
        }

        if (t == typeof(SqlExpression))
        {
            return table.Column("c");
        }

        if (t == typeof(SqlCondition))
        {
            return table.Column("c") > 0;
        }

        if (t == typeof(ISubquery))
        {
            return Select(new DbTable("s").Column("c")).From(new DbTable("s"));
        }

        if (t == typeof(CommonTableExpression[]))
        {
            return new[] { new Cte("cte").As(Select(new DbTable("s").Column("c")).From(new DbTable("s"))) };
        }

        if (t == typeof(SearchedCaseWhenClause))
        {
            return When(table.Column("c") > 0).Then(1);
        }

        if (t == typeof(SearchedCaseWhenClause[]))
        {
            return new[] { When(table.Column("c") > 0).Then(1) };
        }

        if (t == typeof(SimpleCaseWhenClause))
        {
            return When(1).Then(2);
        }

        if (t == typeof(SimpleCaseWhenClause[]))
        {
            return new[] { When(1).Then(2) };
        }

        if (t == typeof(CaseElseExpression))
        {
            return Else(1);
        }

        if (t == typeof(DistinctKeyword))
        {
            return Distinct;
        }

        if (t == typeof(DistinctOnKeyword))
        {
            return DistinctOn(table.Column("c"));
        }

        if (t == typeof(AsteriskMarker))
        {
            return Asterisk;
        }

        if (t == typeof(OrderByClause))
        {
            return OrderBy(table.Column("c"));
        }

        if (t == typeof(SeparatorClause))
        {
            return Separator(", ");
        }

        if (t == typeof(SqlHints))
        {
            return Hints("INDEX(t)");
        }

        if (t == typeof(TopClause))
        {
            return Top(1);
        }

        if (t == typeof(GroupingSet))
        {
            return Group(table.Column("c"));
        }

        if (t == typeof(GroupingSet[]))
        {
            return new[] { Group(table.Column("c")) };
        }

        if (t == typeof(IntervalField))
        {
            return Year();
        }

        if (t == typeof(IntervalExpression))
        {
            return Interval(1, DateTimePart.Day);
        }

        if (t == typeof(IntervalLiteralExpression))
        {
            return IntervalLiteral("1-2", Year(), ToMonth);
        }

        if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(IReadOnlyCollection<>))
        {
            return t.GetGenericArguments()[0].IsValueType
                ? new List<int> { 1 }
                : new List<object> { 1 };
        }

        if (t.IsArray)
        {
            System.Array array = System.Array.CreateInstance(t.GetElementType()!, 1);
            if (t.GetElementType() == typeof(int))
            {
                array.SetValue(1, 0);
            }

            return array;
        }

        return null;
    }

    private static object? Invoke(MethodInfo method, object?[] args)
    {
        try
        {
            return method.Invoke(null, args);
        }
        catch (ArgumentException ex)
        {
            // Reflection-layer rejection (e.g. a null passed where the runtime
            // representation cannot hold it) counts as a loud failure.
            throw new TargetInvocationException(ex);
        }
    }

    // Embeds the factory's return value in a minimal statement and builds it.
    // Returns the SQL on a silent build; null when the build threw (loud) or
    // the node cannot reach SQL without further completing calls.
    private static string? TryBuild(object? node)
    {
        try
        {
            return node switch
            {
                null => null,
                SqlStatement statement => statement.Text,
                ISqlBuilder builder => builder.Build(Dbms.PostgreSql).Text,
                ExpressionAlias alias => Select(alias).Build(Dbms.PostgreSql).Text,
                SqlExpression expr => Select(expr).Build(Dbms.PostgreSql).Text,
                SqlCondition cond =>
                    Select(new DbTable("e", "x").Column("c"))
                        .From(new DbTable("e", "x"))
                        .Where(cond)
                        .Build(Dbms.PostgreSql).Text,
                TableReference tableRef =>
                    Select(new DbTable("e").Column("c")).From(tableRef).Build(Dbms.PostgreSql).Text,
                CommonTableExpression cte =>
                    With(cte).Select(new DbTable("e").Column("c")).From(new DbTable("e"))
                        .Build(Dbms.PostgreSql).Text,
                SortOrder order =>
                    Select(new DbTable("e", "x").Column("c"))
                        .From(new DbTable("e", "x"))
                        .OrderBy(order)
                        .Build(Dbms.PostgreSql).Text,
                SqlHints hints =>
                    Select(hints, new DbTable("e").Column("c"))
                        .From(new DbTable("e"))
                        .Build(Dbms.Oracle).Text,
                GroupingSet set =>
                    Select(new DbTable("e", "x").Column("c"))
                        .From(new DbTable("e", "x"))
                        .GroupBy(GroupingSets(set))
                        .Build(Dbms.PostgreSql).Text,
                GroupingElement element =>
                    Select(new DbTable("e", "x").Column("c"))
                        .From(new DbTable("e", "x"))
                        .GroupBy(element)
                        .Build(Dbms.PostgreSql).Text,
                SeparatorClause separator =>
                    Select(GroupConcat(new DbTable("e", "x").Column("c"), separator))
                        .From(new DbTable("e", "x"))
                        .Build(Dbms.MySql).Text,
                IWithBuilderWith withBuilder =>
                    withBuilder.Select(new DbTable("e").Column("c"))
                        .From(new DbTable("e"))
                        .Build(Dbms.PostgreSql).Text,
                OfClause ofClause =>
                    Select(new DbTable("e", "x").Column("c"))
                        .From(new DbTable("e", "x"))
                        .ForUpdate(ofClause)
                        .Build(Dbms.Oracle).Text,
                // Fall-throughs are ledgered in UnembeddedReturnTypes.
                _ => null,
            };
        }
        catch (NullReferenceException)
        {
            // Rethrown so the sweep can flag a null element that reached Build()
            // as a bare NRE instead of counting it as a compliant loud guard.
            throw;
        }
        catch
        {
            return null;
        }
    }

    private static string Signature(MethodInfo method)
    {
        StringBuilder signature = new();
        signature.Append(method.Name).Append('(');
        ParameterInfo[] parameters = method.GetParameters();
        for (int i = 0; i < parameters.Length; i++)
        {
            if (i > 0)
            {
                signature.Append(", ");
            }

            signature.Append(parameters[i].ParameterType.Name);
        }

        return signature.Append(')').ToString();
    }
}
