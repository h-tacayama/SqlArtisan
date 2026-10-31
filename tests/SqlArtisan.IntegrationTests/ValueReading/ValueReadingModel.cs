namespace SqlArtisan.IntegrationTests.ValueReading;

/// <summary>The dialect-and-version axis: the eight integration lanes.</summary>
public enum Lane
{
    MySql,
    Oracle,
    Oracle23ai,
    PostgreSql,
    PostgreSql17,
    Sqlite,
    SqlServer,
    SqlServer2025,
}

/// <summary>The context axis. SetClause is not in the brief's list; it is added
/// because the `==` slot is also the SET assignment (#707).</summary>
public enum Context
{
    TopLevel,
    Subquery,
    CorrelatedSubquery,
    Cte,
    SetOperationBranch,
    InsideWindow,
    AggregateOrderBy,
    SetClause,
}

/// <summary>A cell's expected result. EngineRejects is a fifth outcome the brief's
/// four do not name: the records leave a loud engine error unguarded on purpose.</summary>
public enum Outcome
{
    Executes,
    BuildRejects,
    AnalyzerWarns,
    CompileStops,
    EngineRejects,
    Question,
    NotASqlSlot,
    Delegated,
}

/// <summary>The SQL shape an executing cell emits for the value.</summary>
public enum Shape
{
    None,
    Marker,
    Literal,
    Ordinal,
    ConstantKey,
    AsWritten,
    ScalarSubquery,
    PositionBound,
}

public sealed record Cell(
    Outcome Outcome,
    Shape Shape,
    string Basis,
    string? DiagnosticId = null,
    string? QuestionId = null,
    string? Meaning = null);

/// <summary>Position classes: the resolver path a slot's value takes. Every slot of
/// <see cref="PositionInventory"/> lands in exactly one (gated).</summary>
public enum PositionClass
{
    /// <summary>A function argument resolved by ExpressionResolver.Resolve.</summary>
    ValueArgument,

    /// <summary>ROUND's scale, a value argument with a recorded SQLA0104 cell.</summary>
    RoundScale,

    /// <summary>A SELECT list item (SelectItemResolver).</summary>
    SelectItem,

    /// <summary>OUTPUT / RETURNING items.</summary>
    OutputItem,

    /// <summary>An INSERT / MERGE / VALUES row value (InsertValueResolver).</summary>
    InsertValue,

    /// <summary>A comparison or predicate right side that also takes ANY/ALL/SOME.</summary>
    QuantifiableOperand,

    /// <summary>A comparison or predicate operand that takes no quantifier.</summary>
    ComparedOperand,

    /// <summary>The right side of `+`.</summary>
    PlusOperand,

    /// <summary>The right side of `-`.</summary>
    MinusOperand,

    /// <summary>The right side of `*`, `/`, `%`.</summary>
    ArithmeticOperand,

    /// <summary>A simple CASE operand or WHEN value.</summary>
    SimpleCaseOperand,

    /// <summary>A CASE THEN / ELSE result.</summary>
    CaseResult,

    /// <summary>MySQL MATCH ... AGAINST search text.</summary>
    AgainstText,

    /// <summary>Sql.Bind: the explicit bind.</summary>
    ExplicitBind,

    /// <summary>new BindValue(...): the explicit bind's constructor.</summary>
    ExplicitBindConstructor,

    /// <summary>A query's own ORDER BY item (OrderByItemResolver.Resolve).</summary>
    StatementOrderByKey,

    /// <summary>Sql.OrderBy / PartitionBy(...).OrderBy: OVER, WITHIN GROUP, aggregates.</summary>
    ExpressionOrderByKey,

    /// <summary>A query's own GROUP BY item.</summary>
    StatementGroupByKey,

    /// <summary>A ROLLUP / CUBE / grouping-set element.</summary>
    GroupingElement,

    /// <summary>A PARTITION BY key.</summary>
    PartitionKey,

    /// <summary>A DISTINCT ON key.</summary>
    DistinctOnKey,

    /// <summary>An int printed into the text (LAG offset, NTILE, frame bound, precision).</summary>
    InlineNumber,

    /// <summary>A pagination count bound as a parameter (TOP, LIMIT, OFFSET, FETCH).</summary>
    BoundRowCount,

    /// <summary>OFFSET with no LIMIT before it (IPagination.Offset).</summary>
    BareOffsetCount,

    /// <summary>The right side of INTERVAL's own `+` (`Interval(...) + date`).</summary>
    IntervalPlusOperand,

    /// <summary>BindArray: a PostgreSQL array value (BindArrayTests own its domain).</summary>
    ArrayBind,

    /// <summary>A percentile fraction (double, printed).</summary>
    InlineFraction,

    /// <summary>A SqlArtisan enum argument (DateTimePart, RegexpOptions, SearchModifier).</summary>
    FunctionEnum,

    /// <summary>A string parameter: ADR 0016's identifier / forced-literal / value rows.</summary>
    TextParameter,

    /// <summary>The string-first `+` (`"x" + col`).</summary>
    TextPlusLeft,

    /// <summary>A char parameter (ESCAPE, a JSON key): ADR 0016's forced literal.</summary>
    CharParameter,

    /// <summary>A slot typed ISubquery only.</summary>
    SubqueryOnly,

    /// <summary>A slot typed INTERVAL or a quantified operand only (DateAdd's interval).</summary>
    PositionBoundOnly,

    /// <summary>A SqlExpression-typed operand (the left side of an operator) or a
    /// column-typed list entry: only an expression compiles.</summary>
    ExpressionOnly,

    /// <summary>Configuration, metadata, sizes, the Build(Dbms) target: no SQL text.</summary>
    NotSql,
}

// Each cell cites a record clause, an ADR or an engine manual; a cell with no such ground
// is a Questions entry, never a guess.
public static class ValueReadingModel
{
    public static readonly Lane[] Lanes = Enum.GetValues<Lane>();

    public static readonly Context[] Contexts = Enum.GetValues<Context>();

    public static readonly PositionClass[] Classes = Enum.GetValues<PositionClass>();

    public static Dbms DbmsOf(Lane lane) => lane switch
    {
        Lane.MySql => Dbms.MySql,
        Lane.Oracle or Lane.Oracle23ai => Dbms.Oracle,
        Lane.PostgreSql or Lane.PostgreSql17 => Dbms.PostgreSql,
        Lane.Sqlite => Dbms.Sqlite,
        _ => Dbms.SqlServer,
    };

    // ---- Record clauses the cells cite (short keys; full text in the report) ----
    internal const string Adr0004 = "ADR 0004 (values are bound) + ADR 0016 row 1";
    internal const string FnEnum = "guards § A function-argument enum is no value (#667/#696)";
    internal const string UserEnumBinds = "#696 scope: user enums untouched; public-api-design § "
        + "Separator/format-string parameters";
    internal const string NullValue = "guards § Null arguments (NullValueMessage)";
    internal const string NullCompared = "guards § Null arguments (ResolveCompared, #614)";
    internal const string NullKey = "guards § Null arguments (NullKey, #673/#693)";
    internal const string InvalidType = "guards § Message grammar (Invalid type for <X> family)";
    internal const string Incomplete = "guards § Incomplete (ANY/ALL/SOME, INTERVAL; #666/#707)";
    internal const string SqlNullCompare = "guards § Null arguments (Sql.Null is SQLA0303's, #742)";
    internal const string BoundKey = "guards § A bound value is no sort, group, partition or "
        + "DISTINCT ON key (#678/#673/#693)";
    internal const string BindDeliberate = "guards § bound-value paragraph: Bind(value) keeps a "
        + "deliberate constant writable";
    internal const string ZeroOrdinal = "guards § Incomplete: a zero ORDER BY ordinal (#521)";
    internal const string NegOrdinal = "guards § Bounded exception: negative ORDER BY ordinal "
        + "(ADR 0011 later instances)";
    internal const string FracOrderBy = "guards § Bounded exception: non-integer ORDER BY key";
    internal const string Ordinals = "guards § bound-value paragraph: statement ORDER BY keeps "
        + "its ordinals (ADR 0011); engine manuals: ORDER BY <integer> is an output position";
    internal const string ExprOrderNumber = "guards § bound-value paragraph (#693/#694)";
    internal const string GroupOrdinal = "ADR 0022 fourth domain + guards § GROUP BY column "
        + "ordinal (#521/#537/#684)";
    internal const string GroupOrdinalLow = "guards § GROUP BY ordinal below 1 (#684)";
    internal const string GroupSubquery = "guards § bound-value paragraph: GroupBy(subquery) "
        + "stays rejected (ADR 0010)";
    internal const string DistinctOnRec =
        "guards § bound-value paragraph: DistinctOn ordinal (#692)";
    internal const string StringPlus = "ADR 0022 sixth domain (#742)";
    internal const string RoundScaleRec = "ADR 0022 fifth domain (#742)";
    internal const string RowCount = "ADR 0022 + guards § negative row count (#523/#529)";
    internal const string OffsetRec = "ADR 0022 § Scope this rule does not claim (#532)";
    internal const string LagRec = "guards § negative Lag/Lead offset (#523)";
    internal const string ValueDomain = "guards § Value-domain list (ADR 0012)";
    internal const string MatrixAvailability = "ADR 0003 dialect matrix (SQLA0100)";
    internal const string Adr0016 = "ADR 0016 (delegated to ForcedLiteralSweepTests)";
    internal const string DatepartTable = "ADR 0022 DatepartValidity / ArgumentValueValidity "
        + "(delegated to DatepartCells / ArgumentValueValidityParityTests)";
    internal const string CSharpTypes = "C# parameter type (no implicit conversion)";
    internal const string CompoundOrderBy = "PostgreSQL select.sgml (UNION ORDER BY takes "
        + "output column names or numbers); SQLite lang_select.html (compound ORDER BY terms)";
    internal const string SqlNullKey = "guards § Null arguments: Sql.Null as a grouping, "
        + "partition or DISTINCT ON key (PARTITION BY NULL one partition on PostgreSQL 16 and "
        + "SQLite; rejected by PostgreSQL 16 in GROUP BY, a grouping element, DISTINCT ON)";
    internal const string GroupingSubqueryRejected = "Oracle ORA-22818 (subquery expressions "
        + "not allowed here); SQL Server Msg 144 (no subquery in a GROUP BY list) — both engine "
        + "error references; first seen in this trial's run 41159339490";
    internal const string NotSqlRec = "no SQL text: configuration or metadata";
    internal const string LoudNull = "guards § Null arguments: loud failure on a single "
        + "non-nullable reference parameter (CS8625/CS8604 is the contract)";
    internal const string WindowAlias = "guards § sort key emitted as a bare name (#687)";
    internal const string Partition = "guards § bound-value paragraph: PARTITION BY reads a "
        + "literal integer as a constant (#673)";
    internal const string GroupingElementRec = "guards § bound-value paragraph: grouping "
        + "element rejects every value (#692)";

    public static readonly IReadOnlyDictionary<string, string> Questions =
        new Dictionary<string, string>
        {
            ["Q1"] = "The brief cites sql-building-style.md rule 8; that file ends at rule 7 "
                + "(main ce84e78). Which record was meant?",
            ["Q2"] = "OrderBy(-1) runs as a constant sort on MySQL and Oracle (bounded "
                + "exception), while GroupBy(-1) is rejected on every dialect because 'an "
                + "engine that reads one reads another meaning' (#684). Should the ORDER BY "
                + "negative ordinal follow GROUP BY's rule, or is the asymmetry intended?",
            ["Q3"] = "OrderBy(2.5) runs as a constant sort on MySQL, Oracle and SQLite with no "
                + "diagnostic; the record says the value 'is invisible to the analyzer', yet a "
                + "literal is a call-site constant and GroupBy(2.5) has a SQLA0104 cell. Is "
                + "a SQLA0104 cell for the ORDER BY key wanted?",
            ["Q4"] = "Bind(n) as a sort/group/partition key: the records keep it as 'a "
                + "deliberate constant'. MySqlConnector substitutes parameters client-side, so "
                + "on MySQL the text becomes a literal integer. What is the intended meaning "
                + "on MySQL — the constant the record promises, or whatever the driver yields?",
            ["Q5"] = "SQL Server: ORDER BY @0 / GROUP BY @0 / OVER (ORDER BY @0) with a bound "
                + "constant. #678 expected Msg 1008 and left it unverified; no record states "
                + "the result.",
            ["Q6"] = "ORDER BY NULL / GROUP BY NULL (Sql.Null) on MySQL, Oracle and SQL Server: "
                + "the records measure PostgreSQL 16 and SQLite only.",
            ["Q7"] = "OrderBy(subquery) throws 'Invalid type for OrderByItem'; GroupBy's "
                + "rejection of a subquery is recorded (addition left to demand), ORDER BY's "
                + "is not. Is the ORDER BY rejection the same decision?",
            ["Q8"] = "A number in an expression-position ORDER BY (OVER / WITHIN GROUP): how "
                + "MySQL, Oracle and SQL Server read it is recorded as unverified (#693). The "
                + "library rejects it everywhere, so only Bind(n) reaches the engine.",
            ["Q9"] = "Function arguments whose integer meaning differs per engine (SUBSTR "
                + "start 0 or negative, INSTR position, REGEXP_* position/occurrence): are "
                + "they in this hazard type (as ROUND's negative scale was taken into SQLA0104 "
                + "by #742), or 'engine-defined semantics, a docs matter' (ADR 0003)?",
            ["Q10"] = "Value-type mapping per driver (bool on Oracle 21c, Guid / TimeSpan / "
                + "DateTimeOffset on SQLite and MySQL) in a value position: in scope for this "
                + "type, or the driver's (ADR 0004)?",
            ["Q12"] = "A C# null in an INSERT / MERGE / VALUES row inserts NULL (#169), while "
                + "every other object-typed value position rejects it (guards § Null "
                + "arguments, 'pass Sql.Null'). Is #169 the recorded exception? No rule clause "
                + "names it.",
            ["Q13"] = "A compound's trailing ORDER BY given a bound value, Sql.Null, a "
                + "qualified column or an expression: no record states what MySQL, Oracle and "
                + "SQL Server do (PostgreSQL and SQLite refuse it per their manuals).",
        };

    /// <summary>The class a slot belongs to, decided by member and parameter names read
    /// from the resolver each factory calls. Null means unclassified (a gate failure).</summary>
    public static PositionClass? ClassOf(PositionInventory.Slot s)
    {
        string m = s.Member;
        string p = s.Parameter;

        switch (s.Kind)
        {
            case PositionInventory.SlotKind.Text:
                return m == "op_Addition" ? PositionClass.TextPlusLeft
                    : s.DeclaringType is "DbmsResolver" or "SqlParameters"
                        ? PositionClass.NotSql
                        : PositionClass.TextParameter;
            case PositionInventory.SlotKind.Character:
                return PositionClass.CharParameter;
            case PositionInventory.SlotKind.Boolean:
                return PositionClass.NotSql;
            case PositionInventory.SlotKind.Subquery:
                return PositionClass.SubqueryOnly;
            case PositionInventory.SlotKind.PositionBound:
                return PositionClass.PositionBoundOnly;
            case PositionInventory.SlotKind.Enum:
                return s.ParameterType is "Dbms" or "DbTypeCategory"
                    ? PositionClass.NotSql : PositionClass.FunctionEnum;
            case PositionInventory.SlotKind.Expression:
                return PositionClass.ExpressionOnly;
            case PositionInventory.SlotKind.Numeric:
                return m switch
                {
                    _ when s.DeclaringType is "BindValue" or "OutputParameter"
                        => PositionClass.NotSql,
                    "Offset" when s.DeclaringType == "IPagination" => PositionClass.BareOffsetCount,
                    "Limit" or "Offset" or "OffsetRows" or "FetchFirst" or "FetchNext" or "Top"
                        => PositionClass.BoundRowCount,
                    "PercentileCont" or "PercentileDisc" => PositionClass.InlineFraction,
                    _ => PositionClass.InlineNumber,
                };
        }

        // Object, ObjectArray, ObjectPair, ObjectRows, Generic
        return (s.DeclaringType, m, p) switch
        {
            ("Sql", "BindArray", _) => PositionClass.ArrayBind,
            ("Sql", "Bind", _) => PositionClass.ExplicitBind,
            ("BindValue", ".ctor", _) => PositionClass.ExplicitBindConstructor,
            ("IntervalExpression", "op_Addition", _) => PositionClass.IntervalPlusOperand,
            ("Sql", "OrderBy", _) or ("PartitionByClause", "OrderBy", _)
                => PositionClass.ExpressionOrderByKey,
            (_, "OrderBy", _) => PositionClass.StatementOrderByKey,
            (_, "GroupBy", _) => PositionClass.StatementGroupByKey,
            ("Sql", "Rollup" or "Cube" or "Group", _) => PositionClass.GroupingElement,
            ("Sql", "PartitionBy", _) => PositionClass.PartitionKey,
            ("Sql", "DistinctOn", _) => PositionClass.DistinctOnKey,
            ("Sql", "Round", "decimals") => PositionClass.RoundScale,
            (_, "Select", _) => PositionClass.SelectItem,
            (_, "Output" or "Returning", _) => PositionClass.OutputItem,
            (_, "Values", _) => PositionClass.InsertValue,
            ("SqlExpression", "op_Equality" or "op_Inequality" or "op_LessThan"
                or "op_LessThanOrEqual" or "op_GreaterThan" or "op_GreaterThanOrEqual"
                or "Like" or "NotLike", _) => PositionClass.QuantifiableOperand,
            ("Sql", "ArrayContains" or "ArrayContainedBy" or "ArrayOverlaps", "rightArray")
                => PositionClass.QuantifiableOperand,
            ("Sql", "JsonbContains", "jsonValue") or ("Sql", "JsonbExists", "key")
                or ("Sql", "TsMatch", "query") => PositionClass.QuantifiableOperand,
            ("SqlExpression", "Between" or "NotBetween" or "In" or "NotIn", _)
                => PositionClass.ComparedOperand,
            ("Sql", "RegexpLike", "source" or "pattern") => PositionClass.ComparedOperand,
            ("Sql", "JsonbExistsAll" or "JsonbExistsAny", "keys") => PositionClass.ComparedOperand,
            ("SqlExpression", "op_Addition", _)
                => PositionClass.PlusOperand,
            ("SqlExpression", "op_Subtraction", _) => PositionClass.MinusOperand,
            ("SqlExpression", "op_Multiply" or "op_Division" or "op_Modulus", _)
                => PositionClass.ArithmeticOperand,
            ("Sql", "Case", "expr") or ("Sql", "When", "whenExpr")
                => PositionClass.SimpleCaseOperand,
            (_, "Then", _) or ("Sql", "Else", _) => PositionClass.CaseResult,
            ("MatchFunction", "Against" or "AgainstScore", _) => PositionClass.AgainstText,
            ("Sql", _, _) => PositionClass.ValueArgument,
            _ => null,
        };
    }

    /// <summary>The expected cell for a class, value kind, lane and context. Context
    /// only refines the key classes; every other class reads the same in each.</summary>
    public static Cell Expect(
        PositionClass c,
        ValueKind v,
        Lane lane,
        Context ctx = Context.TopLevel)
    {
        Dbms d = DbmsOf(lane);

        // A short or byte reads as the int it holds wherever it compiles (added in step 4).
        if (v is ValueKind.Short or ValueKind.Byte)
        {
            v = ValueKind.IntPositive;
        }

        if (v == ValueKind.CSharpNull && c is PositionClass.ExpressionOnly
            or PositionClass.SubqueryOnly or PositionClass.TextPlusLeft
            or PositionClass.PositionBoundOnly)
        {
            return new(Outcome.BuildRejects, Shape.None, LoudNull);
        }

        if (v == ValueKind.CSharpNull && c == PositionClass.InsertValue)
        {
            return new(Outcome.Question, Shape.Literal, "#169 (InsertValueResolver comment): "
                + "a C# null inserts NULL; no rule clause", QuestionId: "Q12");
        }

        if (v == ValueKind.Char && c is PositionClass.InlineNumber or PositionClass.BoundRowCount
            or PositionClass.BareOffsetCount
            or PositionClass.InlineFraction)
        {
            return new(Outcome.Delegated, Shape.None, CSharpTypes + " (char widens to a number)");
        }

        switch (c)
        {
            case PositionClass.NotSql:
                return new(Outcome.NotASqlSlot, Shape.None, NotSqlRec);
            case PositionClass.TextParameter:
            case PositionClass.CharParameter:
                return new(Outcome.Delegated, Shape.None, Adr0016);
            case PositionClass.FunctionEnum:
                return v == ValueKind.FunctionArgEnum
                    ? new(Outcome.Delegated, Shape.None, DatepartTable)
                    : new(Outcome.CompileStops, Shape.None, CSharpTypes);
            case PositionClass.SubqueryOnly:
                return v == ValueKind.Subquery
                    ? new(Outcome.Delegated, Shape.AsWritten, "ISubquery slot: the subquery's "
                        + "own items are positions of their own classes")
                    : new(Outcome.CompileStops, Shape.None, CSharpTypes);
            case PositionClass.ArrayBind:
                return new(Outcome.Delegated, Shape.None, "BindArrayTests (element-type domain)");
            case PositionClass.BareOffsetCount:
                return d is Dbms.MySql or Dbms.Sqlite && v is ValueKind.IntZero
                    or ValueKind.IntPositive or ValueKind.IntNegative
                    ? new(Outcome.BuildRejects, Shape.None, "ADR 0011: bare OFFSET on MySQL/SQLite")
                    : RowCountCell(v, lane);
            case PositionClass.IntervalPlusOperand:
                return v == ValueKind.Interval
                    ? new(Outcome.BuildRejects, Shape.None, Incomplete)
                    : ValueCell(PositionClass.ArithmeticOperand, v, lane, ctx);
            case PositionClass.PositionBoundOnly:
                return v is ValueKind.Interval or ValueKind.Quantified
                    ? new(Outcome.Delegated, Shape.PositionBound,
                        Incomplete + " / " + MatrixAvailability)
                    : new(Outcome.CompileStops, Shape.None, CSharpTypes);
            case PositionClass.ExpressionOnly:
                return v is ValueKind.Column or ValueKind.Expression or ValueKind.BindInt
                    or ValueKind.BindText or ValueKind.BindNull or ValueKind.SqlNull
                    ? new(Outcome.Delegated, Shape.AsWritten,
                        "SqlExpression-typed operand: the value's own class decides")
                    : new(Outcome.CompileStops, Shape.None, CSharpTypes);
            case PositionClass.TextPlusLeft:
                return v is ValueKind.Text
                    ? StringPlusCell(d)
                    : new(Outcome.CompileStops, Shape.None, CSharpTypes);
            case PositionClass.InlineNumber:
                return InlineNumberCell(v);
            case PositionClass.InlineFraction:
                return v is ValueKind.IntZero or ValueKind.IntPositive or ValueKind.IntNegative
                    or ValueKind.Long or ValueKind.Double or ValueKind.DoubleWhole
                    ? new(Outcome.Delegated, Shape.Literal, ValueDomain + ": fraction 0..1")
                    : new(Outcome.CompileStops, Shape.None, CSharpTypes);
            case PositionClass.BoundRowCount:
                return RowCountCell(v, lane);
            case PositionClass.StatementOrderByKey:
                return StatementOrderByCell(v, lane, ctx);
            case PositionClass.ExpressionOrderByKey:
                return ExpressionOrderByCell(v, lane);
            case PositionClass.StatementGroupByKey:
                return StatementGroupByCell(v, lane);
            case PositionClass.GroupingElement:
                return KeyCell(v, lane, GroupingElementRec, "GroupByItem", allowSubquery: true,
                    available: d is Dbms.Oracle or Dbms.PostgreSql or Dbms.SqlServer);
            case PositionClass.PartitionKey:
                return KeyCell(v, lane, Partition, "PartitionByItem", allowSubquery: true,
                    available: true);
            case PositionClass.DistinctOnKey:
                return DistinctOnCell(v, lane);
            case PositionClass.ExplicitBind:
                return ExplicitBindCell(v, constructor: false);
            case PositionClass.ExplicitBindConstructor:
                return ExplicitBindCell(v, constructor: true);
        }

        return ValueCell(c, v, lane, ctx);
    }

    private static Cell StringPlusCell(Dbms d) => d == Dbms.SqlServer
        ? new(Outcome.Executes, Shape.Marker, StringPlus, Meaning: "concatenation")
        : new(Outcome.AnalyzerWarns, Shape.Marker, StringPlus, "SQLA0104");

    private static Cell ValueCell(PositionClass c, ValueKind v, Lane lane, Context ctx)
    {
        Dbms d = DbmsOf(lane);

        switch (v)
        {
            case ValueKind.FunctionArgEnum:
                return new(Outcome.BuildRejects, Shape.None, FnEnum);
            case ValueKind.CSharpNull:
                return new(Outcome.BuildRejects, Shape.None,
                    c is PositionClass.QuantifiableOperand or PositionClass.ComparedOperand
                        or PositionClass.SimpleCaseOperand ? NullCompared : NullValue);
            case ValueKind.DbNull:
                return new(Outcome.BuildRejects, Shape.None, InvalidType);
            case ValueKind.Quantified:
                return c == PositionClass.QuantifiableOperand && ctx != Context.SetClause
                    ? d == Dbms.Sqlite
                        ? new(Outcome.AnalyzerWarns, Shape.PositionBound, MatrixAvailability,
                            "SQLA0100")
                        : new(Outcome.Executes, Shape.PositionBound, Incomplete)
                    : new(Outcome.BuildRejects, Shape.None, Incomplete);
            case ValueKind.Interval:
                return c is PositionClass.PlusOperand or PositionClass.MinusOperand
                    ? d == Dbms.MySql
                        ? new(Outcome.Executes, Shape.PositionBound, Incomplete)
                        : new(Outcome.AnalyzerWarns, Shape.PositionBound, MatrixAvailability,
                            "SQLA0100")
                    : new(Outcome.BuildRejects, Shape.None, Incomplete);
            case ValueKind.SqlNull:
                return c is PositionClass.QuantifiableOperand or PositionClass.SimpleCaseOperand
                    && ctx != Context.SetClause
                    ? new(Outcome.AnalyzerWarns, Shape.Literal, SqlNullCompare, "SQLA0303")
                    : new(Outcome.Executes, Shape.Literal,
                        "Sql.Null emits NULL as written (ADR 0001)");
            case ValueKind.Column:
            case ValueKind.Expression:
                return new(Outcome.Executes, Shape.AsWritten, "ADR 0001");
            case ValueKind.Subquery:
                return new(Outcome.Executes, Shape.ScalarSubquery,
                    "ExpressionResolver: ISubquery becomes a scalar subquery");
            case ValueKind.UserEnum:
                return new(Outcome.Executes, Shape.Marker, UserEnumBinds);
        }

        // ODP.NET reading back a bound bool from Oracle XE 21 throws InvalidCastException
        // (run 41159686928); 23ai returns it. A driver mapping, the Q10 class.
        if (v == ValueKind.Bool && lane == Lane.Oracle && c == PositionClass.SelectItem)
        {
            return new(Outcome.Question, Shape.Marker, "driver mapping, unrecorded",
                QuestionId: "Q10");
        }

        if (c == PositionClass.PlusOperand && v is ValueKind.Text or ValueKind.Char)
        {
            return StringPlusCell(d);
        }

        if (c == PositionClass.RoundScale && v == ValueKind.IntNegative)
        {
            return d == Dbms.Sqlite
                ? new(Outcome.AnalyzerWarns, Shape.Marker, RoundScaleRec, "SQLA0104",
                    Meaning: "scale read as 0")
                : new(Outcome.Executes, Shape.Marker, RoundScaleRec);
        }

        if (v is ValueKind.Double or ValueKind.DoubleWhole or ValueKind.Decimal or ValueKind.Long
            or ValueKind.IntZero
            or ValueKind.IntPositive or ValueKind.IntNegative or ValueKind.Text or ValueKind.Char
            or ValueKind.Bool or ValueKind.DateTime or ValueKind.Guid or ValueKind.DateTimeOffset
            or ValueKind.TimeSpan or ValueKind.BindInt or ValueKind.BindText
            or ValueKind.BindNull)
        {
            return new(Outcome.Executes, Shape.Marker, Adr0004);
        }

        return new(Outcome.Question, Shape.None, "no record", QuestionId: "Q10");
    }

    private static Cell InlineNumberCell(ValueKind v) => v switch
    {
        ValueKind.IntZero or ValueKind.IntPositive or ValueKind.IntNegative
            => new(Outcome.Delegated, Shape.Literal,
                ValueDomain + " / " + LagRec + " (per-member domain, FactoryGuardSweep)"),
        ValueKind.Char => new(Outcome.Delegated, Shape.Literal, CSharpTypes + " (char widens)"),
        _ => new(Outcome.CompileStops, Shape.None, CSharpTypes),
    };

    private static Cell RowCountCell(ValueKind v, Lane lane)
    {
        Dbms d = DbmsOf(lane);
        return v switch
        {
            ValueKind.IntZero or ValueKind.IntPositive
                => new(Outcome.Executes, Shape.Marker, RowCount),
            ValueKind.IntNegative => new(Outcome.Delegated, Shape.Marker,
                RowCount + " / " + OffsetRec + " (ArgumentValueValidity row-count cells)"),
            ValueKind.Char => new(Outcome.Executes, Shape.Marker, CSharpTypes + " (char widens)"),
            _ => new(Outcome.CompileStops, Shape.None, CSharpTypes),
        };
    }

    private static Cell StatementOrderByCell(ValueKind v, Lane lane, Context ctx)
    {
        Dbms d = DbmsOf(lane);

        // A compound ORDER BY: PostgreSQL takes only an output name or number (select.sgml),
        // SQLite also a term identical to a result-column expression (lang_select.html).
        if (ctx == Context.SetOperationBranch && v is ValueKind.BindInt or ValueKind.BindText
            or ValueKind.BindNull or ValueKind.SqlNull or ValueKind.Column or ValueKind.Expression
            or ValueKind.Double or ValueKind.DoubleWhole or ValueKind.Decimal)
        {
            return d == Dbms.Sqlite && v == ValueKind.Column
                ? new(Outcome.Executes, Shape.AsWritten, CompoundOrderBy)
                : d is Dbms.PostgreSql or Dbms.Sqlite
                    ? v is ValueKind.Double or ValueKind.DoubleWhole or ValueKind.Decimal
                        && d == Dbms.PostgreSql
                        ? new(Outcome.BuildRejects, Shape.None, FracOrderBy)
                        : new(Outcome.EngineRejects, Shape.None, CompoundOrderBy)
                    : v is ValueKind.Double or ValueKind.DoubleWhole or ValueKind.Decimal
                        && d == Dbms.SqlServer
                        ? new(Outcome.BuildRejects, Shape.None, FracOrderBy)
                        : new(Outcome.Question, Shape.None, "not recorded", QuestionId: "Q13");
        }

        switch (v)
        {
            case ValueKind.IntPositive:
            case ValueKind.Long:
                return new(Outcome.Executes, Shape.Ordinal, Ordinals, Meaning: "column 2");
            case ValueKind.IntZero:
                return new(Outcome.BuildRejects, Shape.None, ZeroOrdinal);
            case ValueKind.IntNegative:
                return d is Dbms.PostgreSql or Dbms.Sqlite or Dbms.SqlServer
                    ? new(Outcome.BuildRejects, Shape.None, NegOrdinal)
                    : ctx == Context.SetOperationBranch && d == Dbms.Oracle
                        ? new(Outcome.EngineRejects, Shape.None, "Oracle ORA-01785 (a compound's "
                            + "ORDER BY item must be a select-list number); run 41159686928")
                    : new(Outcome.Executes, Shape.ConstantKey, NegOrdinal,
                        QuestionId: "Q2", Meaning: "constant (unsorted)");
            case ValueKind.Double:
            case ValueKind.DoubleWhole:
            case ValueKind.Decimal:
                return d is Dbms.PostgreSql or Dbms.SqlServer
                    ? new(Outcome.BuildRejects, Shape.None, FracOrderBy)
                    : new(Outcome.Executes, Shape.ConstantKey, FracOrderBy,
                        QuestionId: "Q3", Meaning: "constant (unsorted)");
            case ValueKind.Text:
            case ValueKind.Char:
            case ValueKind.Bool:
            case ValueKind.DateTime:
            case ValueKind.Guid:
            case ValueKind.DateTimeOffset:
            case ValueKind.TimeSpan:
            case ValueKind.UserEnum:
            case ValueKind.FunctionArgEnum:
            case ValueKind.DbNull:
                return new(Outcome.BuildRejects, Shape.None, BoundKey);
            case ValueKind.CSharpNull:
                return new(Outcome.BuildRejects, Shape.None, NullKey);
            case ValueKind.Subquery:
                return new(Outcome.BuildRejects, Shape.None, "not recorded", QuestionId: "Q7");
            case ValueKind.Quantified:
            case ValueKind.Interval:
                return new(Outcome.BuildRejects, Shape.None, Incomplete);
            case ValueKind.Column:
            case ValueKind.Expression:
                return new(Outcome.Executes, Shape.AsWritten, "ADR 0001");
            case ValueKind.BindInt:
            case ValueKind.BindText:
            case ValueKind.BindNull:
                return d == Dbms.SqlServer
                    ? new(Outcome.Question, Shape.Marker, BindDeliberate, QuestionId: "Q5")
                    : new(Outcome.Executes, Shape.ConstantKey, BindDeliberate,
                        QuestionId: d == Dbms.MySql ? "Q4" : null,
                        Meaning: "constant (unsorted)");
            case ValueKind.SqlNull:
                return d == Dbms.PostgreSql
                    ? new(Outcome.EngineRejects, Shape.Literal, NullKey + " (PostgreSQL 16 live)")
                    : d == Dbms.Sqlite
                        ? new(Outcome.Executes, Shape.ConstantKey, NullKey + " (SQLite live)",
                            Meaning: "constant (unsorted)")
                        : new(Outcome.Question, Shape.Literal, "unmeasured", QuestionId: "Q6");
        }

        throw new ArgumentOutOfRangeException(nameof(v));
    }

    private static Cell ExpressionOrderByCell(ValueKind v, Lane lane)
    {
        Dbms d = DbmsOf(lane);

        return v switch
        {
            ValueKind.IntZero or ValueKind.IntPositive or ValueKind.IntNegative or ValueKind.Long
                or ValueKind.Double or ValueKind.DoubleWhole or ValueKind.Decimal
                => new(Outcome.BuildRejects, Shape.None, ExprOrderNumber),
            ValueKind.CSharpNull => new(Outcome.BuildRejects, Shape.None, NullKey),
            ValueKind.Quantified or ValueKind.Interval
                => new(Outcome.BuildRejects, Shape.None, Incomplete),
            ValueKind.Column or ValueKind.Expression
                => new(Outcome.Executes, Shape.AsWritten, "ADR 0001"),
            ValueKind.SqlNull => d is Dbms.PostgreSql or Dbms.Sqlite
                ? new(Outcome.Executes, Shape.Literal, NullKey + " (OVER frame spelling, live)")
                : new(Outcome.Question, Shape.Literal, "unmeasured", QuestionId: "Q6"),
            ValueKind.BindInt or ValueKind.BindText or ValueKind.BindNull => d switch
            {
                Dbms.SqlServer => new(Outcome.Question, Shape.Marker, BindDeliberate,
                    QuestionId: "Q5"),
                Dbms.MySql => new(Outcome.Executes, Shape.ConstantKey, BindDeliberate,
                    QuestionId: "Q8", Meaning: "constant (input order)"),
                _ => new(Outcome.Executes, Shape.ConstantKey, BindDeliberate,
                    Meaning: "constant (input order)"),
            },
            ValueKind.Subquery => new(Outcome.BuildRejects, Shape.None, "not recorded",
                QuestionId: "Q7"),
            _ => new(Outcome.BuildRejects, Shape.None, BoundKey),
        };
    }

    private static Cell StatementGroupByCell(ValueKind v, Lane lane)
    {
        Dbms d = DbmsOf(lane);

        switch (v)
        {
            case ValueKind.IntPositive:
            case ValueKind.Long:
                return d is Dbms.Oracle or Dbms.SqlServer
                    ? new(Outcome.AnalyzerWarns, Shape.Ordinal, GroupOrdinal, "SQLA0104",
                        Meaning: d == Dbms.Oracle ? "one constant group" : "engine rejects")
                    : new(Outcome.Executes, Shape.Ordinal, GroupOrdinal, Meaning: "column 1");
            case ValueKind.IntZero:
            case ValueKind.IntNegative:
                return new(Outcome.BuildRejects, Shape.None, GroupOrdinalLow);
            case ValueKind.Double:
            case ValueKind.DoubleWhole:
            case ValueKind.Decimal:
                return d is Dbms.PostgreSql or Dbms.SqlServer
                    ? new(Outcome.AnalyzerWarns, Shape.Literal, GroupOrdinal, "SQLA0104",
                        Meaning: "engine rejects")
                    : new(Outcome.Executes, Shape.ConstantKey, GroupOrdinal,
                        Meaning: "one constant group");
            case ValueKind.CSharpNull:
                return new(Outcome.BuildRejects, Shape.None, NullKey);
            case ValueKind.Subquery:
                return new(Outcome.BuildRejects, Shape.None, GroupSubquery);
            case ValueKind.Quantified:
            case ValueKind.Interval:
                return new(Outcome.BuildRejects, Shape.None, Incomplete);
            case ValueKind.Column:
            case ValueKind.Expression:
                return new(Outcome.Executes, Shape.AsWritten, "ADR 0001");
            case ValueKind.BindInt:
            case ValueKind.BindText:
            case ValueKind.BindNull:
                return d == Dbms.SqlServer
                    ? new(Outcome.Question, Shape.Marker, BindDeliberate, QuestionId: "Q5")
                    : new(Outcome.Executes, Shape.ConstantKey, BindDeliberate,
                        QuestionId: d == Dbms.MySql ? "Q4" : null,
                        Meaning: "one constant group");
            case ValueKind.SqlNull:
                return d == Dbms.Sqlite
                    ? new(Outcome.Executes, Shape.ConstantKey, NullKey + " (SQLite live)",
                        Meaning: "one constant group")
                    : d == Dbms.PostgreSql
                        ? new(Outcome.EngineRejects, Shape.Literal, SqlNullKey)
                        : new(Outcome.Question, Shape.Literal, "unmeasured", QuestionId: "Q6");
            default:
                return new(Outcome.BuildRejects, Shape.None, BoundKey);
        }
    }

    private static Cell KeyCell(
        ValueKind v,
        Lane lane,
        string basis,
        string position,
        bool allowSubquery,
        bool available)
    {
        Dbms d = DbmsOf(lane);
        Cell runs = v switch
        {
            ValueKind.Column or ValueKind.Expression
                => new(Outcome.Executes, Shape.AsWritten, "ADR 0001"),
            ValueKind.Subquery when allowSubquery && position == "GroupByItem"
                && d is Dbms.Oracle or Dbms.SqlServer
                => new(Outcome.EngineRejects, Shape.ScalarSubquery, GroupingSubqueryRejected),
            ValueKind.Subquery when allowSubquery
                => new(Outcome.Executes, Shape.ScalarSubquery, basis),
            ValueKind.BindInt or ValueKind.BindText or ValueKind.BindNull => d switch
            {
                Dbms.SqlServer => new(Outcome.Question, Shape.Marker, BindDeliberate,
                    QuestionId: "Q5"),
                Dbms.MySql => new(Outcome.Executes, Shape.ConstantKey, BindDeliberate,
                    QuestionId: "Q4", Meaning: "constant key"),
                _ => new(Outcome.Executes, Shape.ConstantKey, BindDeliberate,
                    Meaning: "constant key"),
            },
            ValueKind.SqlNull => position == "PartitionByItem"
                && d is Dbms.PostgreSql or Dbms.Sqlite
                ? new(Outcome.Executes, Shape.ConstantKey, SqlNullKey, Meaning: "constant key")
                : position != "PartitionByItem" && d == Dbms.PostgreSql
                    ? new(Outcome.EngineRejects, Shape.Literal, SqlNullKey)
                    : new(Outcome.Question, Shape.Literal, "unmeasured", QuestionId: "Q6"),
            ValueKind.CSharpNull => new(Outcome.BuildRejects, Shape.None, NullKey),
            ValueKind.Quantified or ValueKind.Interval
                => new(Outcome.BuildRejects, Shape.None, Incomplete),
            _ => new(Outcome.BuildRejects, Shape.None, $"{basis}; Invalid type for {position}"),
        };

        return !available && runs.Outcome is Outcome.Executes or Outcome.Question
            ? new(Outcome.AnalyzerWarns, runs.Shape, MatrixAvailability, "SQLA0100")
            : runs;
    }

    private static Cell DistinctOnCell(ValueKind v, Lane lane)
    {
        bool available = DbmsOf(lane) == Dbms.PostgreSql;
        Cell cell = v switch
        {
            ValueKind.IntPositive or ValueKind.Long
                => new(Outcome.Executes, Shape.Ordinal, DistinctOnRec, Meaning: "column 2"),
            ValueKind.IntZero or ValueKind.IntNegative
                => new(Outcome.BuildRejects, Shape.None, DistinctOnRec),
            _ => KeyCell(v, lane, DistinctOnRec, "DistinctOnItem", allowSubquery: true,
                available: true),
        };

        return !available && cell.Outcome is Outcome.Executes or Outcome.Question
            ? new(Outcome.AnalyzerWarns, cell.Shape, MatrixAvailability, "SQLA0100")
            : cell;
    }

    private static Cell ExplicitBindCell(ValueKind v, bool constructor) => v switch
    {
        ValueKind.CSharpNull => new(Outcome.BuildRejects, Shape.None,
            "guards § Null arguments (new BindValue(null))"),
        ValueKind.DbNull => constructor
            ? new(Outcome.Executes, Shape.Marker, "Sql.BindNull() is new BindValue(DBNull.Value)")
            : new(Outcome.BuildRejects, Shape.None, "#757 (names Sql.BindNull)"),
        ValueKind.FunctionArgEnum or ValueKind.UserEnum => new(Outcome.Executes, Shape.Marker,
            FnEnum + ": Bind keeps one writable"),
        ValueKind.Quantified or ValueKind.Interval or ValueKind.Column or ValueKind.Expression
            or ValueKind.SqlNull or ValueKind.Subquery or ValueKind.BindInt
            or ValueKind.BindText or ValueKind.BindNull
            => new(Outcome.BuildRejects, Shape.None, InvalidType + " (Bind takes a value)"),
        _ => new(Outcome.Executes, Shape.Marker, Adr0004),
    };
}
