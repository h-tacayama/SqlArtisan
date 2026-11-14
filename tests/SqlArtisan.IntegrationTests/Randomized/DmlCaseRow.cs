using CsCheck;
using SqlArtisan.Analyzers;

namespace SqlArtisan.IntegrationTests.Randomized;

// Each enum spells one DML model factor, member for level, so a level a model gains fails
// DmlCaseRow.Parse until it is placed here (PictModelTests).
internal enum DmlJoinLevel { None, Direct, From, FromRelisted, Using }

internal enum DmlSubqueryLevel
{
    None,
    In,
    NotIn,
    Exists,
    NotExists,
    ScalarInWhere,
    ScalarInSet,
}

internal enum InsertSourceLevel { Values, MultiValues, Select, SetForm }

internal enum ColumnListLevel { Listed, Omitted }

internal enum DmlValueLevel { Constant, Column, Expression, Subquery }

internal enum DmlOutputLevel { None, Returning, Output, OutputInto }

internal enum DmlCteLevel { None, Leading, InSelect, LeadingRecursive }

/// <summary>One row of <c>insert</c>, <c>update</c> or <c>delete.cases.csv</c>; a factor its
/// model lacks keeps its first level.</summary>
internal sealed record DmlCaseRow(
    string Model,
    int Number,
    Dbms Dbms,
    TargetAliasLevel TargetAlias,
    DmlJoinLevel Join,
    WhereLevel Where,
    DmlSubqueryLevel Subquery,
    bool Correlated,
    InsertSourceLevel Source,
    ColumnListLevel ColumnList,
    DmlValueLevel Value,
    DmlOutputLevel Output,
    DmlCteLevel Cte,
    bool Recursive,
    string Summary)
{
    public static readonly IReadOnlyDictionary<string, string[]> FactorNames =
        new Dictionary<string, string[]>
        {
            ["insert"] =
            [
                "Dbms", "TargetAlias", "Source", "ColumnList", "Value", "Output", "Cte",
                "Recursive",
            ],
            ["update"] =
            [
                "Dbms", "TargetAlias", "Join", "Where", "Subquery", "Correlated", "SetValue",
                "Output", "Cte",
            ],
            ["delete"] =
            [
                "Dbms", "TargetAlias", "Join", "Where", "Subquery", "Correlated", "Output", "Cte",
            ],
        };

    public DmlKind Kind => Model switch
    {
        "insert" => DmlKind.Insert,
        "update" => DmlKind.Update,
        _ => DmlKind.Delete,
    };

    public static DmlCaseRow Parse(string model, int number, IReadOnlyList<string> values)
    {
        string[] names = FactorNames[model];
        if (values.Count != names.Length)
        {
            throw new FormatException(
                $"{model} row {number} has {values.Count} values for {names.Length} factors.");
        }

        Dictionary<string, string> level =
            names.Zip(values).ToDictionary(p => p.First, p => p.Second);
        T Read<T>(string factor) where T : struct, Enum =>
            level.TryGetValue(factor, out string? text) ? CaseRow.Level<T>(text) : default;
        bool Flag(string factor) => level.TryGetValue(factor, out string? text)
            && CaseRow.YesNo(text);

        DmlCteLevel cte = Read<DmlCteLevel>("Cte");
        return new DmlCaseRow(
            model,
            number,
            Read<Dbms>("Dbms"),
            Read<TargetAliasLevel>("TargetAlias"),
            Read<DmlJoinLevel>("Join"),
            Read<WhereLevel>("Where"),
            Read<DmlSubqueryLevel>("Subquery"),
            Flag("Correlated"),
            Read<InsertSourceLevel>("Source"),
            Read<ColumnListLevel>("ColumnList"),
            Read<DmlValueLevel>(level.ContainsKey("SetValue") ? "SetValue" : "Value"),
            Read<DmlOutputLevel>("Output"),
            cte,
            Flag("Recursive") || cte == DmlCteLevel.LeadingRecursive,
            string.Join(" ", values));
    }
}

/// <summary>The rows of the three DML models, typed for <see cref="DmlGenerators"/>.</summary>
internal static class DmlCaseTable
{
    public static readonly string[] Models = ["delete", "insert", "update"];

    private static readonly Lazy<IReadOnlyList<DmlCaseRow>> s_rows = new(Load);

    public static IReadOnlyList<DmlCaseRow> Rows => s_rows.Value;

    public static TheoryData<string, int, string> TheoryRows(Dbms dbms)
    {
        TheoryData<string, int, string> data = [];
        foreach (DmlCaseRow row in Rows.Where(r => r.Dbms == dbms))
        {
            data.Add(row.Model, row.Number, row.Summary);
        }

        return data;
    }

    public static DmlCaseRow Row(string model, int number) =>
        Rows.Single(r => r.Model == model && r.Number == number);

    public static CaseModelBinding Binding(string model) => new(
        DmlCaseRow.FactorNames[model],
        values => DmlCaseRow.Parse(model, 0, values),
        (factor, level) => DmlLevelKeys.For(model, factor, level),
        (number, values) => BuildCases(model, number, values));

    /// <summary>Every statement a lane builds for one row: each seed's statement, its rewrites,
    /// its match query and its negated WHERE.</summary>
    private static IEnumerable<BuildCase> BuildCases(
        string model,
        int number,
        IReadOnlyList<string> values)
    {
        DmlCaseRow row = DmlCaseRow.Parse(model, number, values);
        Gen<DmlSpec> gen = DmlGenerators.For(row);
        return CaseSeeds.For(number).Select(seed => new BuildCase(
            row.Dbms,
            seed,
            () => [.. DmlPlanner.Plan(gen.Generate(PCG.Parse(seed), null!, out _)).Statements()]));
    }

    private static List<DmlCaseRow> Load() =>
    [
        .. Models.SelectMany(model => CaseModels.ReadRaw(model).Rows
            .Select((values, i) => DmlCaseRow.Parse(model, i + 1, values))),
    ];
}

/// <summary>The verdicts a DML level's statements depend on, for the DB-less gate: matrix
/// members, the analyzer's DML shapes and the <see cref="BuildGuards"/>.</summary>
internal static class DmlLevelKeys
{
    public static IReadOnlyList<LevelKey> For(string model, string factor, string level) =>
        (model, factor, level) switch
        {
            ("insert", "TargetAlias", "Aliased") =>
            [
                BuildGuards.InsertTargetAliasedOnMySql,
                BuildGuards.InsertTargetAliasedOnSqlServer,
            ],
            ("insert", "Source", "MultiValues") => [ContextRules.DmlShape.InsertValuesRow],
            ("insert", "Source", "SetForm") => ["Set"],
            ("insert", "Cte", "Leading") =>
                ["With", BuildGuards.LeadingWithOnOracle, BuildGuards.LeadingWithOnMySqlInsert],
            ("insert", "Cte", "InSelect") => ["With", ContextRules.DmlShape.InsertSelectWith],
            ("insert", "Recursive", "Yes") => ["WithRecursive"],
            ("insert", "Output", "Output") => ["Output", "Inserted"],
            ("insert", "Output", "OutputInto") => ["Output", "Inserted", ("Into", 2)],
            ("update", "Join", "Direct") =>
            [
                "InnerJoin", "LeftJoin", "On", ContextRules.DmlShape.JoinedUpdateJoin,
                BuildGuards.DirectJoinUpdateOnSqlServer,
            ],
            ("update", "Join", "From") =>
            [
                "From", ContextRules.DmlShape.JoinedUpdateFrom,
                BuildGuards.UpdateFromNotRelistedOnSqlServer,
            ],
            ("update", "Join", "FromRelisted") =>
            [
                "From", "InnerJoin", "LeftJoin", "On", ContextRules.DmlShape.JoinedUpdateFrom,
                BuildGuards.UpdateRelistedOffSqlServer,
            ],
            ("update", "Output", "Output" or "OutputInto") =>
                ["Output", "Inserted", "Deleted", .. Into(level)],
            ("delete", "Join", "Using") =>
                ["Using", ContextRules.DmlShape.DeleteUsing, BuildGuards.DeleteUsingOnSqlServer],
            ("delete", "Join", "FromRelisted") =>
                ["From", "InnerJoin", "LeftJoin", "On", ContextRules.DmlShape.JoinedDeleteLead],
            ("delete", "Output", "Output" or "OutputInto") => ["Output", "Deleted", .. Into(level)],
            (_, "Cte", "Leading") => ["With", BuildGuards.LeadingWithOnOracle],
            (_, "Cte", "LeadingRecursive") => ["WithRecursive", BuildGuards.LeadingWithOnOracle],
            (_, "Output", "Returning") => ["Returning"],
            (_, "Where", _) => LevelMatrixKeys.For("Where", level),
            (_, "Subquery", "ScalarInSet") => LevelMatrixKeys.For("Subquery", "ScalarInWhere"),
            (_, "Subquery", _) => LevelMatrixKeys.For("Subquery", level),
            _ => [],
        };

    private static LevelKey[] Into(string level) =>
        level == "OutputInto" ? [("Into", 2)] : [];
}
