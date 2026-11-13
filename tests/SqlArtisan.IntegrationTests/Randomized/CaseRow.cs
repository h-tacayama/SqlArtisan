using SqlArtisan.Analyzers;

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
    private static T Level<T>(string text) where T : struct, Enum =>
        Enum.TryParse(text, ignoreCase: false, out T value) && Enum.IsDefined(value)
            && value.ToString() == text
            ? value
            : throw new FormatException($"'{text}' is not a {typeof(T).Name} level.");

    private static bool YesNo(string text) => text switch
    {
        "Yes" => true,
        "No" => false,
        _ => throw new FormatException($"'{text}' is not Yes or No."),
    };
}

/// <summary>The committed PICT model and the rows generated from it, read from the test
/// output directory.</summary>
internal static class CaseTable
{
    public static string ModelPath => ModelFile("select.pict");

    public static string CasesPath => ModelFile("select.cases.csv");

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

    /// <summary>The header and the data rows, each split at its commas; row 1 is the first
    /// line after the header.</summary>
    public static (string[] Header, List<string[]> Rows) ReadRaw()
    {
        string[] lines = File.ReadAllLines(CasesPath);
        string[] header = lines[0].Split(',');
        List<string[]> rows = [.. lines.Skip(1).Where(l => l.Length > 0).Select(l => l.Split(','))];
        return (header, rows);
    }

    private static List<CaseRow> Load()
    {
        (_, List<string[]> raw) = ReadRaw();
        return [.. raw.Select((values, i) => CaseRow.Parse(i + 1, values))];
    }

    private static string ModelFile(string name) =>
        Path.Combine(AppContext.BaseDirectory, "Randomized", "Models", name);
}

/// <summary>
/// The matrix keys a level's queries may emit, for the DB-less gate. A function the generator
/// picks per engine (<c>Length</c>, <c>Substr</c>/<c>Substring</c>) is read from the matrix at
/// generation time, so it never needs a model constraint.
/// </summary>
internal static class LevelMatrixKeys
{
    public static IReadOnlyList<string> For(string factor, string level) => (factor, level) switch
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

/// <summary>Reads <see cref="DialectMatrix"/> for one engine at the baseline of its oldest
/// integration lane, the version every lane of that engine is at least.</summary>
internal static class MatrixLookup
{
    public static bool Supports(Dbms dbms, string member) => Verdict(dbms, member) is null;

    public static bool SupportsLevel(Dbms dbms, string factor, string level) =>
        LevelMatrixKeys.For(factor, level).All(key => Supports(dbms, key));

    /// <summary>Why <paramref name="member"/> does not run on <paramref name="dbms"/>, or
    /// <see langword="null"/> when it does.</summary>
    public static string? Verdict(Dbms dbms, string member)
    {
        TargetDbms target = ToTarget(dbms);
        if (!DialectMatrix.TryGetEntry(member, null, out DbmsSupport support, out _))
        {
            return $"{member} has no DialectMatrix entry";
        }

        if (!support.IsSupported(target))
        {
            return $"DialectMatrix marks {member} unsupported on {dbms}";
        }

        EngineVersion baseline = DialectMatrix.BaselineVersion[target];
        return DialectMatrix.TryGetMinVersion(new MatrixKey(member), target, out EngineVersion min)
            && baseline.CompareTo(min) < 0
            ? $"{member} needs {dbms} {min}, above the {baseline} baseline"
            : null;
    }

    private static TargetDbms ToTarget(Dbms dbms) => dbms switch
    {
        Dbms.MySql => TargetDbms.MySql,
        Dbms.Oracle => TargetDbms.Oracle,
        Dbms.PostgreSql => TargetDbms.PostgreSql,
        Dbms.Sqlite => TargetDbms.Sqlite,
        Dbms.SqlServer => TargetDbms.SqlServer,
        _ => throw new ArgumentOutOfRangeException(nameof(dbms), dbms, message: null),
    };
}
