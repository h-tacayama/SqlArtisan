using CsCheck;

namespace SqlArtisan.IntegrationTests.Randomized;

// Each enum spells one select.pict factor, member for level, so a level the model
// gains fails CaseRow.Parse until it is placed here (PictModelTests).
internal enum TargetAliasLevel { Aliased, Unaliased }

internal enum JoinLevel { None, Inner, Left }

internal enum WhereLevel
{
    None,
    Compare,
    NullTest,
    InList,
    Like,
    Between,
    AndOrNest,
    Not,
    ConditionIfMix,
}

internal enum SubqueryLevel
{
    None,
    In,
    NotIn,
    Exists,
    NotExists,
    ScalarInSelect,
    ScalarInWhere,
}

internal enum AggregateLevel { None, WholeTable, GroupBy, GroupByHaving }

internal enum SelectFunctionLevel { None, Coalesce, Text, Arithmetic, Case }

internal enum OrderByLevel { None, UniqueAsc, UniqueDesc, NullsFirst, NullsLast }

internal enum PagingLevel { None, Limit, LimitOffset, FetchFirst, OffsetFetch, Top }

internal enum SetOperatorLevel { None, Union, UnionAll, Intersect, Except }

internal enum WrapLevel { None, DerivedTable, Cte }

/// <summary>One row of <c>select.cases.csv</c>: the skeleton a generated query must fit.</summary>
internal sealed record CaseRow(
    int Number,
    Dbms Dbms,
    TargetAliasLevel TargetAlias,
    JoinLevel Join,
    WhereLevel Where,
    SubqueryLevel Subquery,
    bool Correlated,
    AggregateLevel Aggregate,
    bool Distinct,
    SelectFunctionLevel SelectFunction,
    OrderByLevel OrderBy,
    PagingLevel Paging,
    SetOperatorLevel SetOperator,
    WrapLevel Wrap)
{
    public static readonly string[] FactorNames =
    [
        "Dbms", "TargetAlias", "Join", "Where", "Subquery", "Correlated", "Aggregate",
        "Distinct", "SelectFunction", "OrderBy", "Paging", "SetOperator", "Wrap",
    ];

    public static CaseRow Parse(int number, IReadOnlyList<string> values)
    {
        if (values.Count != FactorNames.Length)
        {
            throw new FormatException(
                $"Row {number} has {values.Count} values for {FactorNames.Length} factors.");
        }

        return new CaseRow(
            number,
            Level<Dbms>(values[0]),
            Level<TargetAliasLevel>(values[1]),
            Level<JoinLevel>(values[2]),
            Level<WhereLevel>(values[3]),
            Level<SubqueryLevel>(values[4]),
            YesNo(values[5]),
            Level<AggregateLevel>(values[6]),
            YesNo(values[7]),
            Level<SelectFunctionLevel>(values[8]),
            Level<OrderByLevel>(values[9]),
            Level<PagingLevel>(values[10]),
            Level<SetOperatorLevel>(values[11]),
            Level<WrapLevel>(values[12]));
    }

    /// <summary>The level each factor takes, in the model's factor order.</summary>
    public string Summary => string.Join(
        " ",
        Dbms,
        TargetAlias,
        Join,
        Where,
        Subquery,
        Correlated ? "Correlated" : "Uncorrelated",
        Aggregate,
        Distinct ? "Distinct" : "All",
        SelectFunction,
        OrderBy,
        Paging,
        SetOperator,
        Wrap);

    // Exact spelling only: a case-insensitive parse would let a model typo through.
    internal static T Level<T>(string text) where T : struct, Enum =>
        Enum.TryParse(text, ignoreCase: false, out T value) && Enum.IsDefined(value)
            && value.ToString() == text
            ? value
            : throw new FormatException($"'{text}' is not a {typeof(T).Name} level.");

    internal static bool YesNo(string text) => text switch
    {
        "Yes" => true,
        "No" => false,
        _ => throw new FormatException($"'{text}' is not Yes or No."),
    };
}

/// <summary>The rows of <c>select.cases.csv</c>, typed for the SELECT generator.</summary>
internal static class CaseTable
{
    public const string Model = "select";

    public static IReadOnlyList<CaseRow> Rows => s_rows.Value;

    private static readonly Lazy<IReadOnlyList<CaseRow>> s_rows = new(Load);

    public static TheoryData<int, string> TheoryRows(Dbms dbms)
    {
        TheoryData<int, string> data = [];
        foreach (CaseRow row in Rows.Where(r => r.Dbms == dbms))
        {
            data.Add(row.Number, row.Summary);
        }

        return data;
    }

    public static CaseRow Row(int number) => Rows.Single(r => r.Number == number);

    /// <summary>Every statement a lane builds for one row: each seed's query, its rewrites and
    /// its relation parts, and the negative control's negated WHERE.</summary>
    public static IEnumerable<BuildCase> BuildCases(int number, IReadOnlyList<string> values)
    {
        CaseRow row = CaseRow.Parse(number, values);
        Gen<QuerySpec> gen = QueryGenerators.For(row);
        CasePlanner planner = new(row);
        return CaseSeeds.For(number).Select(seed => new BuildCase(
            row.Dbms,
            seed,
            () => [.. planner.Plan(gen.Generate(PCG.Parse(seed), null!, out _)).Statements()]));
    }

    private static List<CaseRow> Load()
    {
        (_, List<string[]> raw) = CaseModels.ReadRaw(Model);
        return [.. raw.Select((values, i) => CaseRow.Parse(i + 1, values))];
    }
}

/// <summary>
/// The matrix keys a <c>select.pict</c> level's queries may emit, for the DB-less gate. A
/// function the generator picks per engine (<c>Length</c>, <c>Substr</c>/<c>Substring</c>) is
/// read from the matrix at generation time, so it never needs a model constraint.
/// </summary>
internal static class LevelMatrixKeys
{
    public static IReadOnlyList<LevelKey> For(string factor, string level) => (factor, level) switch
    {
        ("Join", "Inner") => ["InnerJoin", "On"],
        ("Join", "Left") => ["LeftJoin", "On"],
        ("Where", "NullTest") => ["IsNull", "IsNotNull"],
        ("Where", "InList") => ["In", "NotIn"],
        ("Where", "Like") => ["Like", "NotLike"],
        ("Where", "Between") => ["Between", "NotBetween"],
        ("Where", "Not") => ["Not"],
        ("Where", "ConditionIfMix") => ["ConditionIf"],
        ("Where", _) when level != "None" => ["Where"],
        ("Subquery", "In") => ["In"],
        ("Subquery", "NotIn") => ["NotIn"],
        ("Subquery", "Exists") => ["Exists"],
        ("Subquery", "NotExists") => ["NotExists"],
        ("Subquery", "ScalarInSelect" or "ScalarInWhere") => ["Max", "Min", "Count"],
        ("Aggregate", "WholeTable") => ["Count", "Sum", "Max", "Min"],
        ("Aggregate", "GroupBy") => ["GroupBy", "Count", "Sum", "Max", "Min"],
        ("Aggregate", "GroupByHaving") => ["GroupBy", "Having", "Count", "Sum", "Max", "Min"],
        ("Distinct", "Yes") => ["Distinct"],
        ("SelectFunction", "Coalesce") => ["Coalesce"],
        ("SelectFunction", "Text") => ["Upper", "Lower", "Trim", "Replace"],
        ("SelectFunction", "Arithmetic") => ["Abs"],
        ("SelectFunction", "Case") => ["Case", "When", "Then", "Else"],
        ("OrderBy", "UniqueAsc") => ["OrderBy", "Asc"],
        ("OrderBy", "UniqueDesc") => ["OrderBy", "Desc"],
        ("OrderBy", "NullsFirst") => ["OrderBy", "NullsFirst"],
        ("OrderBy", "NullsLast") => ["OrderBy", "NullsLast"],
        ("Paging", "Limit") => ["Limit"],
        ("Paging", "LimitOffset") => ["Limit", "Offset"],
        ("Paging", "FetchFirst") => ["FetchFirst"],
        ("Paging", "OffsetFetch") => ["OffsetRows", "FetchNext"],
        ("Paging", "Top") => ["Top"],
        ("SetOperator", "Union") => ["Union"],
        ("SetOperator", "UnionAll") => ["UnionAll"],
        ("SetOperator", "Intersect") => ["Intersect"],
        ("SetOperator", "Except") => ["Except"],
        ("Wrap", "Cte") => ["With"],
        _ => [],
    };
}
