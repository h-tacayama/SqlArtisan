using System.Reflection;
using static SqlArtisan.Sql;

namespace SqlArtisan.Tests;

// Pins ADR 0016's forced-literal row: every public text parameter is classified, and
// each forced literal is built with ', \, a backslash-newline and a non-ASCII character
// on every dialect, so a node rewritten to append its string raw fails here.
public class ForcedLiteralSweepTests
{
    private const string StringInput = "a'b\\c";

    private const string NonAsciiInput = "名";

    private const string BackslashNewlineInput = "a\\\nb\\\r\nc";

    // A char position cannot hold every hazard at once, so it takes them one by one.
    private static readonly string[] CharInputs = ["'", "\\", NonAsciiInput];

    private static readonly string[] StringInputs =
        [StringInput, BackslashNewlineInput, NonAsciiInput];

    private static readonly (string, Dbms) CodePageCase = (NonAsciiInput, Dbms.SqlServer);

    // Escaping is the dialect's, not the construct's, so every case builds on every
    // dialect, whether or not the construct runs there.
    private static readonly Dictionary<(string Input, Dbms Dbms), string> ExpectedLiterals = new()
    {
        [(StringInput, Dbms.MySql)] = "'a''b\\\\c'",
        [(StringInput, Dbms.Oracle)] = "'a''b\\c'",
        [(StringInput, Dbms.PostgreSql)] = "E'a''b\\\\c'",
        [(StringInput, Dbms.Sqlite)] = "'a''b\\c'",
        [(StringInput, Dbms.SqlServer)] = "'a''b\\c'",
        [(BackslashNewlineInput, Dbms.MySql)] = "'a\\\\\nb\\\\\r\nc'",
        [(BackslashNewlineInput, Dbms.Oracle)] = "'a\\\nb\\\r\nc'",
        [(BackslashNewlineInput, Dbms.PostgreSql)] = "E'a\\\\\nb\\\\\r\nc'",
        [(BackslashNewlineInput, Dbms.Sqlite)] = "'a\\\nb\\\r\nc'",
        [(BackslashNewlineInput, Dbms.SqlServer)] = "'a\\\\\n\nb\\\\\r\n\r\nc'",
        [("'", Dbms.MySql)] = "''''",
        [("'", Dbms.Oracle)] = "''''",
        [("'", Dbms.PostgreSql)] = "''''",
        [("'", Dbms.Sqlite)] = "''''",
        [("'", Dbms.SqlServer)] = "''''",
        [("\\", Dbms.MySql)] = "'\\\\'",
        [("\\", Dbms.Oracle)] = "'\\'",
        [("\\", Dbms.PostgreSql)] = "E'\\\\'",
        [("\\", Dbms.Sqlite)] = "'\\'",
        [("\\", Dbms.SqlServer)] = "'\\'",
        [(NonAsciiInput, Dbms.MySql)] = "'名'",
        [(NonAsciiInput, Dbms.Oracle)] = "'名'",
        [(NonAsciiInput, Dbms.PostgreSql)] = "'名'",
        [(NonAsciiInput, Dbms.Sqlite)] = "'名'",
        [(NonAsciiInput, Dbms.SqlServer)] = "N'名'",
    };

    // Key: "Type.Member(signature) :: parameter". Operands are NULL so the expected
    // text differs across dialects only in the literal itself.
    private static readonly Dictionary<string, LiteralCase> ForcedLiterals = new()
    {
        ["LikeCondition.Escape(Char) :: escapeChar"] = new(
            v => Select(Case(When(Null.Like(Null).Escape(v[0])).Then(Null))),
            l => $"SELECT CASE WHEN (NULL LIKE NULL ESCAPE {l}) THEN NULL END"),
        ["NotLikeCondition.Escape(Char) :: escapeChar"] = new(
            v => Select(Case(When(Null.NotLike(Null).Escape(v[0])).Then(Null))),
            l => $"SELECT CASE WHEN (NULL NOT LIKE NULL ESCAPE {l}) THEN NULL END"),
        ["Sql.Currval(String) :: sequenceName"] = new(
            v => Select(Currval(v)),
            l => $"SELECT CURRVAL({l})"),
        ["Sql.IntervalLiteral(String) :: text"] = new(
            v => Select(IntervalLiteral(v)),
            l => $"SELECT INTERVAL {l}"),
        ["Sql.IntervalLiteral(String, IntervalField) :: value"] = new(
            v => Select(IntervalLiteral(v, Year())),
            l => $"SELECT INTERVAL {l} YEAR"),
        ["Sql.IntervalLiteral(String, IntervalField, IntervalField) :: value"] = new(
            v => Select(IntervalLiteral(v, Year(), ToMonth)),
            l => $"SELECT INTERVAL {l} YEAR TO MONTH"),
        ["Sql.JsonArrow(Object, String) :: key"] = new(
            v => Select(JsonArrow(Null, v)),
            l => $"SELECT (NULL -> {l})"),
        ["Sql.JsonArrow(Object, Char) :: key"] = new(
            v => Select(JsonArrow(Null, v[0])),
            l => $"SELECT (NULL -> {l})"),
        ["Sql.JsonArrowText(Object, String) :: key"] = new(
            v => Select(JsonArrowText(Null, v)),
            l => $"SELECT (NULL ->> {l})"),
        ["Sql.JsonArrowText(Object, Char) :: key"] = new(
            v => Select(JsonArrowText(Null, v[0])),
            l => $"SELECT (NULL ->> {l})"),
        ["Sql.JsonExtract(Object, String) :: path"] = new(
            v => Select(JsonExtract(Null, v)),
            l => $"SELECT JSON_EXTRACT(NULL, {l})"),
        ["Sql.JsonQuery(Object, String) :: path"] = new(
            v => Select(JsonQuery(Null, v)),
            l => $"SELECT JSON_QUERY(NULL, {l})"),
        ["Sql.JsonValue(Object, String) :: path"] = new(
            v => Select(JsonValue(Null, v)),
            l => $"SELECT JSON_VALUE(NULL, {l})"),
        ["Sql.Nextval(String) :: sequenceName"] = new(
            v => Select(Nextval(v)),
            l => $"SELECT NEXTVAL({l})"),
        ["Sql.PlaintoTsquery(String, Object) :: config"] = new(
            v => Select(PlaintoTsquery(v, Null)),
            l => $"SELECT PLAINTO_TSQUERY({l}, NULL)"),
        ["Sql.Separator(String) :: separator"] = new(
            v => Select(GroupConcat(Null, Separator(v))),
            l => $"SELECT GROUP_CONCAT(NULL SEPARATOR {l})"),
        ["Sql.StringAgg(Object, String) :: separator"] = new(
            v => Select(StringAgg(Null, v)),
            l => $"SELECT STRING_AGG(NULL, {l})",
            CodePage: true),
        ["Sql.StringAgg(Object, String, OrderByClause) :: separator"] = new(
            v => Select(StringAgg(Null, v, OrderBy(Null))),
            l => $"SELECT STRING_AGG(NULL, {l} ORDER BY NULL)",
            CodePage: true),
        ["Sql.ToTsquery(String, Object) :: config"] = new(
            v => Select(ToTsquery(v, Null)),
            l => $"SELECT TO_TSQUERY({l}, NULL)"),
        ["Sql.ToTsvector(String, Object) :: config"] = new(
            v => Select(ToTsvector(v, Null)),
            l => $"SELECT TO_TSVECTOR({l}, NULL)"),
    };

    // Key as above; value: why the position is not a forced literal. "identifier" is
    // ADR 0016's verbatim row; "raw" is the Hints escape hatch; the rest never reach
    // the SQL text as caller text ("bound" is a bind value, "rejected" throws).
    private static readonly Dictionary<string, string> OtherTextParameters = new()
    {
        ["Cte..ctor(String) :: name"] = "identifier",
        ["Cte.Column(String) :: name"] = "identifier",
        ["CteBase..ctor(String) :: name"] = "identifier",
        ["DbColumn..ctor(TableReference, String) :: name"] = "identifier",
        ["DbmsResolver.RegisterProvider(String, Dbms) :: typeFullName"] = "unrendered",
        ["DbTable..ctor(String, String) :: tableAlias"] = "identifier",
        ["DbTable..ctor(String, String) :: tableName"] = "identifier",
        ["DbTable.Column(String) :: name"] = "identifier",
        ["DbTableBase..ctor(String, String) :: tableAlias"] = "identifier",
        ["DbTableBase..ctor(String, String) :: tableName"] = "identifier",
        ["DerivedTable..ctor(String) :: name"] = "identifier",
        ["DerivedTable.Column(String) :: name"] = "identifier",
        ["DerivedTableBase..ctor(String) :: name"] = "identifier",
        ["ExpressionAlias.op_Addition(ExpressionAlias, String) :: rightSide"] = "rejected",
        ["ExpressionAlias.op_Addition(String, ExpressionAlias) :: leftSide"] = "rejected",
        ["ISubquery.As(String) :: alias"] = "identifier",
        ["ISubquery.AsTable(String) :: alias"] = "identifier",
        ["IntervalExpression.op_Addition(String, IntervalExpression) :: leftSide"] = "bound",
        ["OutputParameter..ctor(String, DbType, Nullable`1) :: variable"] = "identifier",
        ["QuantifiedExpression.op_Addition(QuantifiedExpression, String) :: rightSide"] = "rejected",
        ["QuantifiedExpression.op_Addition(String, QuantifiedExpression) :: leftSide"] = "rejected",
        ["QuantifiedSubquery.op_Addition(QuantifiedSubquery, String) :: rightSide"] = "rejected",
        ["QuantifiedSubquery.op_Addition(String, QuantifiedSubquery) :: leftSide"] = "rejected",
        ["Sql.Cast(Object, String) :: type"] = "identifier",
        ["Sql.Hints(String) :: hints"] = "raw",
        ["Sql.NextValueFor(String) :: sequenceName"] = "identifier",
        ["Sql.Sequence(String) :: name"] = "identifier",
        ["Sql.Values(String, String[], Object[][]) :: alias"] = "identifier",
        ["Sql.Values(String, String[], Object[][]) :: columnNames"] = "identifier",
        ["SqlExpression.As(String) :: alias"] = "identifier",
        ["SqlExpression.op_Addition(String, SqlExpression) :: leftSide"] = "bound",
        ["SqlParameters.Get(String) :: name"] = "unrendered",
        ["SubqueryDerivedTable.Column(String) :: name"] = "identifier",
        ["UnnestDerivedTable.Column(String) :: name"] = "identifier",
        ["UnnestFunction.AsTable(String) :: alias"] = "identifier",
        ["UnnestFunction.AsTable(String, String[]) :: alias"] = "identifier",
        ["UnnestFunction.AsTable(String, String[]) :: columns"] = "identifier",
        ["ValuesDerivedTable.Column(String) :: name"] = "identifier",
    };

    [Fact]
    public void TextParameters_PublicSurface_AreClassified()
    {
        HashSet<string> scanned = [.. TextParameters().Select(p => p.Key)];
        List<string> violations = new();

        foreach (string key in scanned.Order(StringComparer.Ordinal))
        {
            bool literal = ForcedLiterals.ContainsKey(key);
            if (literal == OtherTextParameters.ContainsKey(key))
            {
                violations.Add(literal
                    ? $"DOUBLY CLASSIFIED {key} — keep it in one catalog only."
                    : $"UNCLASSIFIED {key} — add it to ForcedLiterals or OtherTextParameters "
                        + "(ADR 0016).");
            }
        }

        foreach (string stale in ForcedLiterals.Keys.Concat(OtherTextParameters.Keys)
            .Except(scanned))
        {
            violations.Add($"STALE CATALOG ENTRY {stale} — no such parameter; remove it.");
        }

        Assert.True(
            violations.Count == 0,
            $"{violations.Count} text-parameter classification "
                + $"violation(s):\n\n{string.Join("\n", violations)}");
    }

    [Fact]
    public void ForcedLiterals_QuoteBackslashAndNonAscii_EscapesLiteral()
    {
        Dictionary<string, Type> types = TextParameters().ToDictionary(p => p.Key, p => p.Type);
        Dbms[] dialects = [.. Enum.GetValues<Dbms>().Where(d => d != Dbms.Unknown)];
        List<string> violations = new();

        foreach ((string key, LiteralCase literalCase) in ForcedLiterals)
        {
            string[] inputs =
                types.GetValueOrDefault(key) == typeof(char) ? CharInputs : StringInputs;
            foreach (string input in inputs)
            {
                foreach (Dbms dbms in dialects)
                {
                    SqlStatement sql = literalCase.Build(input).Build(dbms);
                    string literal = literalCase.CodePage && (input, dbms) == CodePageCase
                        ? $"'{NonAsciiInput}'"
                        : ExpectedLiterals[(input, dbms)];
                    string expected = literalCase.Expected(literal);
                    if (sql.Text != expected || sql.Parameters.Count != 0)
                    {
                        violations.Add(
                            $"{key} on {dbms}\n  expected: {expected}\n  actual:   {sql.Text} "
                                + $"({sql.Parameters.Count} parameter(s))");
                    }
                }
            }
        }

        Assert.True(
            violations.Count == 0,
            $"{violations.Count} forced-literal escaping "
                + $"violation(s):\n\n{string.Join("\n\n", violations)}");
    }

    private static IEnumerable<(string Key, Type Type)> TextParameters()
    {
        const BindingFlags Flags =
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static
            | BindingFlags.Instance | BindingFlags.DeclaredOnly;

        foreach (Type type in typeof(Sql).Assembly.GetExportedTypes())
        {
            // A protected member is surface too: a user's subclass calls it.
            foreach (MethodBase member in type.GetMethods(Flags)
                .Concat<MethodBase>(type.GetConstructors(Flags))
                .Where(m => m.IsPublic || m.IsFamily || m.IsFamilyOrAssembly))
            {
                ParameterInfo[] parameters = member.GetParameters();
                string signature = string.Join(", ", parameters.Select(p => p.ParameterType.Name));
                foreach (ParameterInfo parameter in parameters)
                {
                    if (!parameter.IsOut && IsText(parameter.ParameterType))
                    {
                        yield return (
                            $"{type.Name}.{member.Name}({signature}) :: {parameter.Name}",
                            parameter.ParameterType);
                    }
                }
            }
        }
    }

    // A collection of text carries caller text as surely as a lone string does; a
    // delegate's string arguments flow out to the caller, not into the SQL.
    private static bool IsText(Type type) =>
        type == typeof(string)
        || type == typeof(char)
        || (type.HasElementType && IsText(type.GetElementType()!))
        || (type.IsGenericType
            && !typeof(Delegate).IsAssignableFrom(type)
            && type.GetGenericArguments().Any(IsText));

    // CodePage: the position keeps a varchar literal on SQL Server (ADR 0016, #663).
    private sealed record LiteralCase(
        Func<string, ISqlBuilder> Build,
        Func<string, string> Expected,
        bool CodePage = false);
}
