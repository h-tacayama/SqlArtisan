using SqlArtisan.Analyzers;
using SqlArtisan.IntegrationTests.Schema;
using static SqlArtisan.Sql;

namespace SqlArtisan.IntegrationTests.Infrastructure;

/// <summary>
/// One dialect-sweep entry: a statement exercising exactly one matrix construct,
/// executed on EVERY engine and asserted BOTH ways against the analyzer's
/// <see cref="DialectMatrix"/> — the engines the matrix marks supported must
/// accept it, the ones it marks unsupported must reject it. The second
/// direction is what the smoke catalogs don't cover: an engine accepting a
/// construct the matrix rejects is an analyzer false positive in the wild.
/// </summary>
/// <param name="Key">The matrix entry (member name + optional arity) this exercises.</param>
/// <param name="Build">Produces the statement; receives the engine so union entries (e.g. <c>Match</c>) can pick the dialect-appropriate form.</param>
/// <param name="Mutating">Whether to run inside a rolled-back transaction (DML).</param>
/// <param name="PositiveSkips">Engines where the supported side cannot be exercised here, with the reason (e.g. container lacks the full-text feature).</param>
/// <param name="NegativeSkips">Engines where the unsupported side cannot be asserted — the engine parses the text as something else entirely, or the driver rejects the key's parameter client-side — with the reason.</param>
internal sealed record SweepCase(
    MatrixKey Key,
    Func<Dbms, ISqlBuilder> Build,
    bool Mutating = false,
    IReadOnlyDictionary<Dbms, string>? PositiveSkips = null,
    IReadOnlyDictionary<Dbms, string>? NegativeSkips = null);

/// <summary>
/// The statement-per-matrix-entry catalog behind the dialect sweep (#93 step 2).
/// Every <see cref="DialectMatrix"/> key must appear here or in
/// <see cref="ExcludedEntries"/> — enforced by a completeness test, so a new matrix
/// entry without a sweep statement fails the suite.
/// </summary>
internal static class MatrixSweepCatalog
{
    private const string FieldMarker =
        "An IntervalLiteral field argument with no SQL of its own. Year() and Day() appear "
        + "bare in the IntervalLiteral cases, and each field's arity-1 case sweeps the same "
        + "keyword with a precision.";

    /// <summary>Matrix entries deliberately not swept, with the reason.</summary>
    public static readonly IReadOnlyDictionary<MatrixKey, string> ExcludedEntries =
        new Dictionary<MatrixKey, string>
        {
            [new MatrixKey("Into")] =
            "Oracle RETURNING ... INTO needs output-parameter binding, which this "
            + "generic accept/reject runner cannot do; the positive side is covered by "
                + "OracleTests, and the "
            + "negative side would fail for binding (not grammar) reasons on every other engine.",
            [new MatrixKey("Year")] = FieldMarker,
            [new MatrixKey("Month")] = FieldMarker,
            [new MatrixKey("Day")] = FieldMarker,
            [new MatrixKey("Hour")] = FieldMarker,
            [new MatrixKey("Minute")] = FieldMarker,
            [new MatrixKey("ToSecond")] = FieldMarker,
            [new MatrixKey("ConditionIf")] =
            "A C#-side helper: the emitted SQL is identical to the underlying "
            + "condition (or absent), so there is no distinct construct for an engine to "
                + "accept or reject.",
        };

    public static IReadOnlyList<SweepCase> Cases { get; } = Build();

    private static List<SweepCase> Build()
    {
        UsersTable u = new();
        OrdersTable o = new();
        List<SweepCase> cases = [];

        void Add(string member, Func<Dbms, ISqlBuilder> build) =>
            cases.Add(new SweepCase(new MatrixKey(member), build));
        void AddArity(string member, int arity, Func<Dbms, ISqlBuilder> build) =>
            cases.Add(new SweepCase(new MatrixKey(member, arity), build));
        void AddMutating(string member, Func<Dbms, ISqlBuilder> build) =>
            cases.Add(new SweepCase(new MatrixKey(member), build, Mutating: true));
        void AddSkips(
            string member,
            Func<Dbms, ISqlBuilder> build,
            params (Dbms Engine, string Reason)[] skips) =>
            cases.Add(new SweepCase(new MatrixKey(member), build,
                PositiveSkips: skips.ToDictionary(s => s.Engine, s => s.Reason)));

        // `SELECT <expr> FROM users WHERE id = 1` — one row, always a FROM (no DUAL needed).
        ISqlBuilder Scalar(object expr) => Select(expr).From(u).Where(u.Id == 1);
        ISqlBuilder Window(object expr) => Select(expr).From(u);
        ISqlBuilder WherePredicate(SqlCondition condition) => Select(Count(u.Id)).From(u).Where(
            condition);

        // --- Statement / clause / builder core ---
        Add("Select", _ => Select(u.Id).From(u));
        Add("Asterisk", _ => Select(Asterisk).From(u).Where(u.Id == 1));
        Add("From", _ => Select(u.Id).From(u));
        Add("Where", _ => Select(u.Id).From(u).Where(u.Id == 1));
        Add("GroupBy", _ => Select(u.DepartmentId).From(u).GroupBy(u.DepartmentId));
        Add("Having", _ => Select(u.DepartmentId).From(u).GroupBy(u.DepartmentId).Having(Count(u.Id) >= 1));
        Add("OrderBy", _ => Select(u.Id).From(u).OrderBy(u.Id));
        Add("On", _ =>
        {
            UsersTable ju = new("u");
            OrdersTable jo = new("o");
            return Select(jo.Amount).From(jo).InnerJoin(ju).On(jo.UserId == ju.Id);
        });
        AddMutating("InsertInto", _ => InsertInto(u, u.Id, u.Name).Values(600, "Sweep"));
        // Duplicate id=1 (already seeded): MySQL ignores the row (0 affected), every other engine
        // rejects INSERT IGNORE.
        AddMutating("InsertIgnoreInto", _ => InsertIgnoreInto(u, u.Id, u.Name).Values(1, "Sweep"));
        AddMutating("Values", _ => InsertInto(u, u.Id, u.Name).Values(601, "Sweep"));
        AddMutating("Update", _ => Update(u).Set(u.Name == "Sweep").Where(u.Id == 1));
        AddMutating("Set", _ => Update(u).Set(u.Name == "Sweep").Where(u.Id == 1));
        AddMutating("DeleteFrom", _ => DeleteFrom(u).Where(u.Id == -1));
        Add("InnerJoin", _ =>
        {
            UsersTable ju = new("u");
            OrdersTable jo = new("o");
            return Select(jo.Amount).From(jo).InnerJoin(ju).On(jo.UserId == ju.Id);
        });
        Add("LeftJoin", _ =>
        {
            UsersTable ju = new("u");
            OrdersTable jo = new("o");
            return Select(jo.Amount).From(jo).LeftJoin(ju).On(jo.UserId == ju.Id);
        });
        Add("RightJoin", _ =>
        {
            UsersTable ju = new("u");
            OrdersTable jo = new("o");
            return Select(jo.Amount).From(jo).RightJoin(ju).On(jo.UserId == ju.Id);
        });
        Add("CrossJoin", _ =>
        {
            UsersTable ju = new("u");
            OrdersTable jo = new("o");
            return Select(Count(ju.Id)).From(ju).CrossJoin(jo);
        });
        // Aliased, so MySQL's rejection is the join's own: after an unaliased table it reads
        // FULL as that table's alias (MySqlTests.FullJoin_UnaliasedLeftTable_ReadsFullAsItsAlias).
        Add("FullJoin", _ =>
        {
            UsersTable ju = new("u");
            OrdersTable jo = new("o");
            return Select(jo.Amount).From(jo).FullJoin(ju).On(jo.UserId == ju.Id);
        });
        // NATURAL JOIN matches on every shared column name; "id" is the only column both
        // UsersTable and OrdersTable declare, so no explicit predicate is needed.
        Add("NaturalJoin", _ =>
        {
            UsersTable ju = new("u");
            OrdersTable jo = new("o");
            return Select(jo.Amount).From(jo).NaturalJoin(ju);
        });
        Add("NaturalLeftJoin", _ =>
        {
            UsersTable ju = new("u");
            OrdersTable jo = new("o");
            return Select(jo.Amount).From(jo).NaturalLeftJoin(ju);
        });
        Add("NaturalRightJoin", _ =>
        {
            UsersTable ju = new("u");
            OrdersTable jo = new("o");
            return Select(jo.Amount).From(jo).NaturalRightJoin(ju);
        });
        Add("NaturalFullJoin", _ =>
        {
            UsersTable ju = new("u");
            OrdersTable jo = new("o");
            return Select(jo.Amount).From(jo).NaturalFullJoin(ju);
        });
        AddArity("Using", 2, _ =>
        {
            UsersTable ju = new("u");
            OrdersTable jo = new("o");
            return Select(jo.Amount).From(jo).InnerJoin(ju).Using(jo.Id);
        });
        Add("With", _ =>
        {
            Cte c = new("c");
            return With(c.As(Select(u.Id).From(u).Where(u.Age >= 40)))
                .Select(Count(c.Column("id"))).From(c);
        });
        // Oracle's recursive WITH needs the column list, so there the case carries one and
        // is rejected at RECURSIVE alone.
        Add("WithRecursive", dbms =>
        {
            UsersTable ru = new("ru");
            Cte c = new("c");
            CommonTableExpression body = c.As(
                Select(ru.Id.As(c.Column("id"))).From(ru).Where(ru.Id == 1)
                .UnionAll
                .Select(ru.Id).From(ru).InnerJoin(c).On(ru.Id == c.Column("id") + 1)
                .Where(ru.Id <= 3));
            return WithRecursive(dbms == Dbms.Oracle ? body.WithColumnList() : body)
                .Select(c.Column("id")).From(c);
        });
        // Non-recursive: MySQL and PostgreSQL require RECURSIVE for a self-reference;
        // StatementCatalog's RecursiveCtePlainWith is the Oracle proof.
        Add("WithColumnList", _ =>
        {
            Cte c = new("c");
            return With(c.As(Select(u.Id).From(u)).WithColumnList())
                .Select(c.Column("id")).From(c);
        });
        Add("Distinct", _ => Select(Distinct, u.DepartmentId).From(u));
        Add("Hints", _ => Select(Hints("/*+ sweep */"), u.Id).From(u));
        Add("Top", _ => Select(Top(1), u.Id).From(u));
        Add("Percent", _ => Select(Top(50).Percent(), u.Id).From(u));
        // WITH TIES needs an ORDER BY (the Build-time guard); the no-workaround form.
        Add("WithTies", _ => Select(Top(1).WithTies(), u.Id).From(u).OrderBy(u.Id));
        AddSkips("Group",
            _ => Select(u.DepartmentId).From(u).GroupBy(
                GroupingSets(Group(u.DepartmentId), Group())),
            (Dbms.MySql, "Group() is only exercisable inside a grouping extension, none of which "
                + "MySQL supports."),
            (Dbms.Sqlite, "Group() is only exercisable inside a grouping extension, none of which "
                + "SQLite supports."));
        Add("Null", _ => Select(Null).From(u).Where(u.Id == 1));
        Add("Bind", _ => Scalar(Bind(1)));
        // A dbType hint, matching real usage: an untyped NULL-only parameter
        // has nothing for the driver to infer a wire type from.
        Add("BindNull", _ => Scalar(BindNull(System.Data.DbType.Int32)));
        Add("As", _ => Select(u.Id.As("i")).From(u));
        Add("Asc", _ => Select(u.Id).From(u).OrderBy(u.Id.Asc));
        Add("Desc", _ => Select(u.Id).From(u).OrderBy(u.Id.Desc));

        // --- Expressions / conditions ---
        Add("Case", _ => Scalar(Case(When(u.Age > 30).Then("old"), Else("young"))));
        Add("When", _ => Scalar(Case(When(u.Age > 30).Then("old"), Else("young"))));
        Add("Then", _ => Scalar(Case(When(u.Age > 30).Then("old"), Else("young"))));
        Add("Else", _ => Scalar(Case(When(u.Age > 30).Then("old"), Else("young"))));
        Add("Cast", _ => Scalar(Cast(u.Age, "DECIMAL(10,2)")));
        Add("Exists", _ => WherePredicate(Exists(Select(o.Id).From(o))));
        Add("NotExists", _ => WherePredicate(NotExists(Select(o.Id).From(o).Where(o.Id == -1))));
        Add("Not", _ => WherePredicate(Not(u.Age > 40)));
        Add("In", _ => WherePredicate(u.Id.In(1, 2)));
        Add("NotIn", _ => WherePredicate(u.Id.NotIn(1, 2)));
        Add("Between", _ => WherePredicate(u.Age.Between(30, 40)));
        Add("NotBetween", _ => WherePredicate(u.Age.NotBetween(30, 40)));
        Add("Like", _ => WherePredicate(u.Name.Like("A%")));
        Add("NotLike", _ => WherePredicate(u.Name.NotLike("A%")));
        Add("Escape", _ => WherePredicate(u.Name.Like("A!%").Escape('!')));
        Add("IsNull", _ => WherePredicate(u.Name.IsNull));
        Add("IsNotNull", _ => WherePredicate(u.Name.IsNotNull));
        Add("All", _ => WherePredicate(u.Age > All(Select(o.UserId).From(o))));
        Add("Any", _ => WherePredicate(u.Age > Any(Select(o.UserId).From(o))));
        Add("Some", _ => WherePredicate(u.Age > Some(Select(o.UserId).From(o))));
        Add("Coalesce", _ => Scalar(Coalesce(u.Name, "x")));
        Add("Nullif", _ => Scalar(Nullif(1, 2)));

        // --- Universal functions ---
        Add("Abs", _ => Scalar(Abs(-5)));
        Add("Floor", _ => Scalar(Floor(1.7)));
        // Oracle-only rounding: its 38-digit NUMBER overflows System.Decimal client-side,
        // which the sweep cannot tell from a rejection; PostgreSQL lacks the overload (#440).
        Add("Exp", dbms => Scalar(dbms == Dbms.Oracle ? Round(Exp(1), 4) : Exp(1)));
        Add("Power", _ => Scalar(Power(2, 3)));
        Add("Sqrt", _ => Scalar(Sqrt(16)));
        Add("Sign", _ => Scalar(Sign(-5)));
        Add("Lower", _ => Scalar(Lower("ABC")));
        Add("Upper", _ => Scalar(Upper("abc")));
        Add("Replace", _ => Scalar(Replace("abc", "b", "X")));
        Add("Avg", _ => Select(Avg(o.Amount)).From(o));
        Add("Count", _ => Select(Count(u.Id), Count(Asterisk)).From(u));
        Add("Max", _ => Select(Max(u.Age)).From(u));
        Add("Min", _ => Select(Min(u.Age)).From(u));
        Add("Sum", _ => Select(Sum(o.Amount)).From(o));
        // The Exp overflow again (#442): the STDDEV family takes a square root, the
        // VARIANCE family does not and needs no rounding.
        Add("StddevPop", dbms => Select(dbms == Dbms.Oracle ? Round(StddevPop(o.Amount), 4) : StddevPop(o.Amount)).From(o));
        Add("StddevSamp", dbms => Select(dbms == Dbms.Oracle ? Round(StddevSamp(o.Amount), 4) : StddevSamp(o.Amount)).From(o));
        Add("Stddev", dbms => Select(dbms == Dbms.Oracle ? Round(Stddev(o.Amount), 4) : Stddev(o.Amount)).From(o));
        Add("Stdev", _ => Select(Stdev(o.Amount)).From(o));
        Add("Stdevp", _ => Select(Stdevp(o.Amount)).From(o));
        Add("VarPop", _ => Select(VarPop(o.Amount)).From(o));
        Add("VarSamp", _ => Select(VarSamp(o.Amount)).From(o));
        Add("Variance", _ => Select(Variance(o.Amount)).From(o));
        Add("Var", _ => Select(Var(o.Amount)).From(o));
        Add("Varp", _ => Select(Varp(o.Amount)).From(o));
        Add("CurrentTimestamp", _ => Scalar(CurrentTimestamp()));
        AddArity("CurrentTimestamp", 1, _ => Scalar(CurrentTimestamp(3)));
        AddArity("Concat", 2, _ => Scalar(Concat("a", "b")));
        AddArity("Concat", 4, _ => Scalar(Concat("a", "b", "c")));
        cases.Add(new SweepCase(new MatrixKey("DoublePipe"),
            _ => Scalar(DoublePipe("a", "b")),
            NegativeSkips: new Dictionary<Dbms, string>
            {
                [Dbms.MySql] = "Under MySQL's default sql_mode, || is logical OR (not string "
                    + "concatenation) — a boolean expression, not a grammar error — so "
                        + "the call text "
                    + "executes there too; acceptance proves nothing about "
                        + "DoublePipe's own support.",
            }));

        // --- Window / analytic ---
        Add("Rank", _ => Window(Rank().Over(OrderBy(u.Age))));
        Add("RowNumber", _ => Window(RowNumber().Over(OrderBy(u.Age))));
        Add("DenseRank", _ => Window(DenseRank().Over(OrderBy(u.Age))));
        Add("CumeDist", _ => Window(CumeDist().Over(OrderBy(u.Age))));
        Add("PercentRank", _ => Window(PercentRank().Over(OrderBy(u.Age))));
        Add("Ntile", _ => Window(Ntile(2).Over(OrderBy(u.Age))));
        Add("Lag", _ => Window(Lag(u.Age).Over(OrderBy(u.Id))));
        Add("Lead", _ => Window(Lead(u.Age).Over(OrderBy(u.Id))));
        Add("FirstValue", _ => Window(FirstValue(u.Age).Over(OrderBy(u.Id))));
        Add("LastValue", _ => Window(LastValue(u.Age).Over(OrderBy(u.Id))));
        Add("NthValue", _ => Window(NthValue(u.Age, 2).Over(OrderBy(u.Id))));
        Add("Over", _ => Window(Sum(u.Age).Over(PartitionBy(u.DepartmentId))));
        Add("PartitionBy", _ => Window(Sum(u.Age).Over(PartitionBy(u.DepartmentId))));
        Add("Rows", _ => Window(Avg(u.Age).Over(OrderBy(u.Id).Rows(UnboundedPreceding))));
        Add("Range", _ => Window(Avg(u.Age).Over(OrderBy(u.Id).Range(UnboundedPreceding))));
        Add("RowsBetween", _ => Window(Avg(u.Age).Over(OrderBy(u.Id).RowsBetween(Preceding(1), Following(1)))));
        Add("RangeBetween", _ => Window(Avg(u.Age).Over(OrderBy(u.Id).RangeBetween(UnboundedPreceding, CurrentRow))));
        Add("CurrentRow", _ => Window(Avg(u.Age).Over(OrderBy(u.Id).RangeBetween(UnboundedPreceding, CurrentRow))));
        Add("Preceding", _ => Window(Avg(u.Age).Over(OrderBy(u.Id).RowsBetween(Preceding(1), Following(1)))));
        Add("Following", _ => Window(Avg(u.Age).Over(OrderBy(u.Id).RowsBetween(Preceding(1), Following(1)))));
        Add("UnboundedPreceding", _ => Window(Avg(u.Age).Over(OrderBy(u.Id).Rows(UnboundedPreceding))));
        Add("UnboundedFollowing", _ => Window(Avg(u.Age).Over(OrderBy(u.Id).RowsBetween(CurrentRow, UnboundedFollowing))));

        // --- Character / numeric with dialect gaps ---
        Add("Mod", _ => Scalar(Mod(10, 3)));
        Add("Ln", _ => Scalar(Ln(1)));
        Add("Log10", _ => Scalar(Log10(1000)));
        AddArity("Log", 1, _ => Scalar(Log(100)));
        // Oracle-only rounding, same reason as Exp above; PostgreSQL rejects
        // round(double precision, int), so it stays bare.
        cases.Add(new SweepCase(new MatrixKey("Log", 2),
            dbms => Scalar(dbms == Dbms.Oracle ? Round(Log(2, 8), 4) : Log(2, 8)),
            NegativeSkips: new Dictionary<Dbms, string>
            {
                [Dbms.SqlServer] = "T-SQL's LOG takes the value first and the base second "
                    + "(LOG(float_expression [, base])), so the two-argument call text "
                        + "executes there "
                    + "too — it just computes a different logarithm. Acceptance proves "
                        + "nothing about "
                    + "the base-first form this entry describes.",
            }));
        Add("Length", _ => Scalar(Length("abc")));
        Add("Substr", _ => Scalar(Substr("abcdef", 2, 3)));
        Add("Substring", _ => Scalar(Substring("abcdef", 2, 3)));
        Add("Lpad", _ => Scalar(Lpad("x", 3, "0")));
        AddArity("Lpad", 2, _ => Scalar(Lpad("x", 3)));
        Add("Rpad", _ => Scalar(Rpad("x", 3, "0")));
        AddArity("Rpad", 2, _ => Scalar(Rpad("x", 3)));
        Add("Ltrim", _ => Scalar(Ltrim(" x")));
        AddArity("Ltrim", 2, _ => Scalar(Ltrim("xxabc", "x")));
        Add("Rtrim", _ => Scalar(Rtrim("x ")));
        AddArity("Rtrim", 2, _ => Scalar(Rtrim("abcxx", "x")));
        Add("Trim", _ => Scalar(Trim(" x ")));
        AddArity("Trim", 2, _ => Scalar(Trim("xxaxx", "x")));
        Add("Round", _ => Select(Round(o.Amount, 1)).From(o).Where(o.Id == 1));
        AddArity("Round", 1, _ => Scalar(Round(1.7)));
        Add("Trunc", _ => Select(Trunc(o.Amount)).From(o).Where(o.Id == 1));
        AddArity("Trunc", 2, _ => Select(Trunc(o.Amount, 1)).From(o).Where(o.Id == 1));
        Add("Instr", _ => Scalar(Instr("abc", "b", 1)));
        AddArity("Instr", 2, _ => Scalar(Instr("abc", "b")));
        Add("ConcatWs", _ => Scalar(ConcatWs("-", "a", "b")));
        Add("CharLength", _ => Scalar(CharLength("abc")));
        Add("Left", _ => Scalar(Left("abc", 2)));
        Add("Right", _ => Scalar(Right("abc", 2)));
        Add("Position", _ => Scalar(Position("b", "abc")));
        Add("Strpos", _ => Scalar(Strpos("abc", "b")));
        Add("Greatest", _ => Scalar(Greatest(1, 2, 3)));
        Add("Least", _ => Scalar(Least(1, 2, 3)));
        Add("Ceil", _ => Scalar(Ceil(1.2)));
        Add("Ceiling", _ => Scalar(Ceiling(1.2)));
        Add("Lengthb", _ => Scalar(Lengthb("abc")));
        Add("Substrb", _ => Scalar(Substrb("abcdef", 2)));
        Add("Decode", _ => Scalar(Decode(u.Age, (30, "thirty"), "other")));
        Add("Nvl", _ => Scalar(Nvl(u.Name, "x")));
        Add("Ifnull", _ => Scalar(Ifnull(u.Name, "x")));
        // MySQL's ISNULL is the 1-argument predicate, so the 2-argument call rejects on
        // arity rather than on the name — still a clean rejection for the sweep.
        Add("Isnull", _ => Scalar(Isnull(u.Name, "x")));
        Add("If", _ => Scalar(If(u.Age > 1, "adult", "minor")));
        Add("Iif", _ => Scalar(Iif(u.Age > 1, "adult", "minor")));
        Add("Numtodsinterval", _ => Scalar(Numtodsinterval(30, DateTimePart.Day)));
        Add("Numtoyminterval", _ => Scalar(Numtoyminterval(3, DateTimePart.Month)));

        // --- Overloaded C# operators (#219) ---
        Add("op_Addition", _ => Scalar(u.Age + 1));
        Add("op_Subtraction", _ => Scalar(u.Age - 1));
        Add("op_Multiply", _ => Scalar(u.Age * 2));
        Add("op_Division", _ => Scalar(u.Age / 2));
        // The one negative cell: Oracle rejects % as a grammar error (no parses-as-something-else
        // hazard, unlike DoublePipe on MySQL), so no skips are needed.
        Add("op_Modulus", _ => Scalar(u.Age % 2));
        Add("op_Equality", _ => WherePredicate(u.Id == 1));
        Add("op_Inequality", _ => WherePredicate(u.Id != -1));
        Add("op_LessThan", _ => WherePredicate(u.Id < 100));
        Add("op_GreaterThan", _ => WherePredicate(u.Id > -1));
        Add("op_LessThanOrEqual", _ => WherePredicate(u.Id <= 100));
        Add("op_GreaterThanOrEqual", _ => WherePredicate(u.Id >= -1));
        Add("op_BitwiseAnd", _ => WherePredicate((u.Id == 1) & (u.Age >= 0)));
        Add("op_BitwiseOr", _ => WherePredicate((u.Id == 1) | (u.Id == 2)));

        // --- Date / time ---
        Add("CurrentDate", _ => Scalar(CurrentDate));
        Add("CurrentTime", _ => Scalar(CurrentTime()));
        AddArity("CurrentTime", 1, _ => Scalar(CurrentTime(3)));
        Add("Extract", _ => Scalar(Extract(DateTimePart.Year, u.CreatedAt)));
        Add("Datepart", _ => Scalar(Datepart(DateTimePart.Year, u.CreatedAt)));
        Add("Dateadd", _ => Scalar(Dateadd(DateTimePart.Day, 1, u.CreatedAt)));
        Add("Datediff", _ => Scalar(Datediff(DateTimePart.Day, u.CreatedAt, u.CreatedAt)));
        Add("DateTrunc", _ => Scalar(DateTrunc(DateTimePart.Month, u.CreatedAt)));
        Add("Datetrunc", _ => Scalar(Datetrunc(DateTimePart.Month, u.CreatedAt)));
        Add("DateFormat", _ => Scalar(DateFormat(u.CreatedAt, "%Y-%m")));
        Add("AddMonths", _ => Scalar(AddMonths(u.CreatedAt, 1)));
        // Swept as a date-arithmetic operand: MySQL's INTERVAL has no standalone value
        // form, and IntervalLiteral is embedded the same way for the same reason.
        Add("Interval", _ => Scalar(u.CreatedAt + Interval(30, DateTimePart.Day)));
        AddArity("IntervalLiteral", 1, _ => Scalar(u.CreatedAt + IntervalLiteral("30 days")));
        AddArity("IntervalLiteral", 2, _ => Scalar(u.CreatedAt + IntervalLiteral("30", Day())));
        AddArity(
            "IntervalLiteral",
            3,
            _ => Scalar(u.CreatedAt + IntervalLiteral("1-2", Year(), ToMonth)));
        AddArity("Year", 1, _ => Scalar(u.CreatedAt + IntervalLiteral("5", Year(3))));
        AddArity("Month", 1, _ => Scalar(u.CreatedAt + IntervalLiteral("5", Month(3))));
        AddArity("Day", 1, _ => Scalar(u.CreatedAt + IntervalLiteral("5", Day(3))));
        AddArity("Hour", 1, _ => Scalar(u.CreatedAt + IntervalLiteral("5", Hour(3))));
        AddArity("Minute", 1, _ => Scalar(u.CreatedAt + IntervalLiteral("5", Minute(3))));
        AddArity(
            "ToSecond",
            1,
            _ => Scalar(u.CreatedAt + IntervalLiteral("1 02:03:04.5", Day(), ToSecond(2))));
        // The literal runs on MySQL, Oracle and PostgreSQL, so their verdict is the function's own;
        // SQLite and SQL Server have no INTERVAL literal, so no statement isolates it there.
        Add("DateAdd", _ => Scalar(DateAdd(u.CreatedAt, IntervalLiteral("30", Day()))));
        Add("DateSub", _ => Scalar(DateSub(u.CreatedAt, IntervalLiteral("30", Day()))));
        Add("Timestampadd", _ => Scalar(Timestampadd(DateTimePart.Day, 1, u.CreatedAt)));
        Add("Timestampdiff", _ => Scalar(Timestampdiff(DateTimePart.Day, u.CreatedAt, u.CreatedAt)));
        AddArity("Date", 1, _ => Scalar(Date(u.CreatedAt)));
        AddArity("Date", 3, _ => Scalar(Date(u.CreatedAt, "+30 days")));
        Add("Datetime", _ => Scalar(Datetime(u.CreatedAt, "+1 hour")));
        Add("Julianday", _ => Scalar(Julianday(u.CreatedAt)));
        Add("Strftime", _ => Scalar(Strftime("%Y-%m", u.CreatedAt)));
        Add("LastDay", _ => Scalar(LastDay(u.CreatedAt)));
        Add("MonthsBetween", _ => Scalar(MonthsBetween(u.CreatedAt, u.CreatedAt)));
        Add("Sysdate", _ => Scalar(Sysdate));
        Add("Systimestamp", _ => Scalar(Systimestamp));
        // Isolated from SYSDATE so MySQL's FROM DUAL support is actually exercised.
        Add("Dual", _ => Select(Abs(-1)).From(Dual));

        // --- Conversion ---
        Add("ToChar", _ => Scalar(ToChar(u.Age, "999")));
        AddArity("ToChar", 1, _ => Scalar(ToChar(u.Age)));
        Add("ToDate", _ => Scalar(ToDate("2020-01-01", "YYYY-MM-DD")));
        Add("ToNumber", _ => Scalar(ToNumber("123", "999")));
        AddArity("ToNumber", 1, _ => Scalar(ToNumber("123")));
        Add("ToTimestamp", _ => Scalar(ToTimestamp("2020-01-01 00:00:00", "YYYY-MM-DD HH24:MI:SS")));
        cases.Add(new SweepCase(new MatrixKey("Format"),
            _ => Scalar(Format(u.CreatedAt, "yyyy-MM")),
            NegativeSkips: new Dictionary<Dbms, string>
            {
                [Dbms.MySql] =
                    "MySQL has its own FORMAT(X, D[, locale]) function (formats a number to "
                    + "D decimal places), so the call text executes there too via implicit "
                        + "type coercion "
                    + "of both arguments — acceptance proves nothing about SQL Server's "
                        + "FORMAT support.",
                [Dbms.Sqlite] =
                    "SQLite 3.38+ has its own printf-style format() function (an alias for "
                    + "printf()), so the call text executes there too, but with "
                        + "incompatible semantics "
                    + "(substitution directives, not .NET date/number format strings) — "
                        + "acceptance proves "
                    + "nothing about SQL Server's FORMAT support.",
            }));

        // --- Regexp ---
        Add("RegexpLike", _ => WherePredicate(RegexpLike(u.Name, "A.*")));
        Add("RegexpCount", _ => Scalar(RegexpCount(u.Name, "a")));
        Add("RegexpReplace", _ => Scalar(RegexpReplace(u.Name, "a", "b")));
        AddArity("RegexpReplace", 4, _ => Scalar(RegexpReplace(u.Name, "a", "b", 1)));
        AddArity("RegexpReplace", 5, _ => Scalar(RegexpReplace(u.Name, "a", "b", 1, 1)));
        AddArity("RegexpReplace", 6, _ => Scalar(RegexpReplace(u.Name, "a", "b", 1, 1, RegexpOptions.None)));
        Add("RegexpSubstr", _ => Scalar(RegexpSubstr(u.Name, "A")));
        AddArity("RegexpSubstr", 6, _ => Scalar(RegexpSubstr(u.Name, "A", 1, 1, RegexpOptions.None, 1)));
        Add("RegexpInstr", _ => Scalar(RegexpInstr(u.Name, "A")));
        AddArity("RegexpInstr", 7, _ => Scalar(RegexpInstr(u.Name, "A", 1, 1, 0, RegexpOptions.None, 1)));

        // --- Aggregate chains ---
        Add("Filter", _ => Select(Count(u.Id).Filter(u.Age > 30)).From(u));
        Add("WithinGroup", dbms => dbms switch
        {
            Dbms.Oracle => Select(Listagg(u.Name, ",").WithinGroup(OrderBy(u.Name))).From(u),
            Dbms.SqlServer => Select(StringAgg(u.Name, ", ").WithinGroup(OrderBy(u.Name))).From(u),
            _ => Select(PercentileCont(0.5).WithinGroup(OrderBy(u.Age))).From(u),
        });
        Add("PercentileCont", dbms => dbms == Dbms.SqlServer
            ? Window(PercentileCont(0.5).WithinGroup(OrderBy(u.Age)).Over())
            : Select(PercentileCont(0.5).WithinGroup(OrderBy(u.Age))).From(u));
        Add("PercentileDisc", dbms => dbms == Dbms.SqlServer
            ? Window(PercentileDisc(0.5).WithinGroup(OrderBy(u.Age)).Over())
            : Select(PercentileDisc(0.5).WithinGroup(OrderBy(u.Age))).From(u));

        // --- String aggregation ---
        Add("StringAgg", _ => Select(StringAgg(u.Name, ", ")).From(u));
        AddArity("StringAgg", 3, _ => Select(StringAgg(u.Name, ", ", OrderBy(u.Name))).From(u));
        Add("Listagg", _ => Select(Listagg(u.Name, ",").WithinGroup(OrderBy(u.Name))).From(u));
        Add("GroupConcat", _ => Select(GroupConcat(u.Name)).From(u));
        Add("Separator", _ => Select(GroupConcat(u.Name, Separator(", "))).From(u));

        // --- GROUP BY extensions ---
        Add("Rollup", _ => Select(u.DepartmentId).From(u).GroupBy(Rollup(u.DepartmentId)));
        Add("Cube", _ => Select(u.DepartmentId).From(u).GroupBy(Cube(u.DepartmentId)));
        Add("GroupingSets", _ => Select(u.DepartmentId).From(u).GroupBy(GroupingSets(Group(u.DepartmentId), Group())));
        Add("WithRollup", _ => Select(u.DepartmentId).From(u).GroupBy(u.DepartmentId).WithRollup());
        // MySQL has no ROLLUP(...) function form (see the Rollup entry above), so
        // GROUPING(...) is exercised there via its native WITH ROLLUP suffix instead.
        AddArity("Grouping", 1, dbms => dbms == Dbms.MySql
            ? Select(u.DepartmentId, Grouping(u.DepartmentId)).From(u).GroupBy(u.DepartmentId)
                .WithRollup()
            : Select(Grouping(u.DepartmentId)).From(u).GroupBy(Rollup(u.DepartmentId)));
        AddArity("Grouping", 3, dbms => dbms == Dbms.MySql
            ? Select(u.DepartmentId, u.Age, Grouping(u.DepartmentId, u.Age)).From(u)
                .GroupBy(u.DepartmentId, u.Age).WithRollup()
            : Select(Grouping(u.DepartmentId, u.Age)).From(u).GroupBy(
                Rollup(u.DepartmentId, u.Age)));
        // MySQL groups through WITH ROLLUP and SQLite has no ROLLUP, so there GROUPING_ID
        // itself is what the engine rejects.
        AddArity("GroupingId", 2, dbms => dbms switch
        {
            Dbms.MySql => Select(GroupingId(u.DepartmentId, u.Age)).From(u)
                .GroupBy(u.DepartmentId, u.Age).WithRollup(),
            Dbms.Sqlite => Select(GroupingId(u.DepartmentId, u.Age)).From(u)
                .GroupBy(u.DepartmentId, u.Age),
            _ => Select(GroupingId(u.DepartmentId, u.Age)).From(u)
                .GroupBy(Rollup(u.DepartmentId, u.Age)),
        });

        // --- Set operators ---
        Add("Union", _ => Select(u.DepartmentId).From(u).Union.Select(u.DepartmentId).From(u));
        Add("UnionAll", _ => Select(u.DepartmentId).From(u).UnionAll.Select(u.DepartmentId).From(u));
        Add("Except", _ => Select(u.DepartmentId).From(u).Except.Select(u.DepartmentId).From(u));
        Add("ExceptAll", _ => Select(u.DepartmentId).From(u).ExceptAll.Select(u.DepartmentId).From(u));
        Add("Intersect", _ => Select(u.DepartmentId).From(u).Intersect.Select(u.DepartmentId).From(u));
        Add("IntersectAll", _ => Select(u.DepartmentId).From(u).IntersectAll.Select(u.DepartmentId).From(u));
        cases.Add(new SweepCase(new MatrixKey("Minus"),
            _ => Select(u.DepartmentId).From(u).Minus.Select(u.DepartmentId).From(u),
            NegativeSkips: new Dictionary<Dbms, string>
            {
                [Dbms.SqlServer] = "T-SQL parses MINUS as a table alias and the second SELECT as a "
                    + "separate batch statement, so the text executes without MINUS "
                        + "acting as a set "
                    + "operator — acceptance proves nothing about MINUS support.",
            }));
        Add("MinusAll", _ => Select(u.DepartmentId).From(u).MinusAll.Select(u.DepartmentId).From(u));

        // --- Pagination ---
        // Limit exercises IN (... LIMIT n) (#240; the Offset case proves top-level LIMIT):
        // MySQL rejects it there, a context restriction the matrix cannot express (SQLA0102).
        AddSkips("Limit",
            _ => Select(Count(u.Id)).From(u)
                .Where(u.Id.In(Select(o.UserId).From(o).OrderBy(o.UserId).Limit(2))),
            (Dbms.MySql, "MySQL rejects LIMIT inside an IN/ALL/ANY/SOME subquery "
                + "(ER_NOT_SUPPORTED_YET), "
                + "though it supports LIMIT itself — proven by the Offset case; covered by context "
                    + "rule SQLA0102 (#264)."));
        // Build() takes OFFSET only after LIMIT on MySQL and SQLite; elsewhere it stands alone.
        Add("Offset", dbms => dbms is Dbms.MySql or Dbms.Sqlite
            ? Select(u.Id).From(u).OrderBy(u.Id).Limit(2).Offset(1)
            : Select(u.Id).From(u).OrderBy(u.Id).Offset(1));
        Add("OffsetRows", _ => Select(u.Id).From(u).OrderBy(u.Id).OffsetRows(1).FetchNext(2));
        Add("FetchNext", _ => Select(u.Id).From(u).OrderBy(u.Id).OffsetRows(1).FetchNext(2));
        // FetchFirst exercises the aliased-scalar-subquery position (#240): the Oracle
        // scalar FETCH FIRST 1 ROW ONLY idiom the #225 audit could not write.
        Add("FetchFirst", _ => Select(
                Select(o.Amount).From(o).OrderBy(o.Amount).FetchFirst(1).As("top_amount"))
            .From(u).Where(u.Id == 1));

        // --- FOR UPDATE ---
        Add("ForUpdate", _ => Select(u.Id).From(u).Where(u.Id == 1).ForUpdate());
        Add("Of", _ => Select(u.Id).From(u).Where(u.Id == 1).ForUpdate(Of(u.Id)));
        cases.Add(new SweepCase(new MatrixKey("Of", 2),
            _ => Select(u.Id).From(u).Where(u.Id == 1).ForUpdate(Of(u))));
        Add("Nowait", _ => Select(u.Id).From(u).Where(u.Id == 1).ForUpdate(Nowait));
        Add("SkipLocked", _ => Select(u.Id).From(u).Where(u.Id == 1).ForUpdate(SkipLocked));
        Add("Wait", _ => Select(u.Id).From(u).Where(u.Id == 1).ForUpdate(Wait(3)));

        // --- NULLS ordering ---
        Add("NullsFirst", _ => Select(u.Id).From(u).OrderBy(u.Age.NullsFirst));
        Add("NullsLast", _ => Select(u.Id).From(u).OrderBy(u.Age.NullsLast));

        // --- DISTINCT ON ---
        Add("DistinctOn", _ => Select(DistinctOn(u.DepartmentId), u.DepartmentId).From(u).OrderBy(u.DepartmentId));

        // --- APPLY / LATERAL ---
        // CrossApply and CrossJoinLateral run the per-group top-N shape — a row-limited
        // lateral subquery (#240). The limiter follows EACH engine's own row-limiting
        // grammar (proven by the plain pagination cases), including on the engines the
        // matrix rejects, so the accept/reject outcome isolates the APPLY / LATERAL
        // construct itself rather than a smuggled-in invalid limiter.
        Add("CrossApply", dbms =>
        {
            UsersTable au = new("u");
            OrdersTable ao = new("o");
            DerivedTable x = new("x");
            ISelectBuilderOrderBy ordered = Select(ao.Amount.As(x.Column("amount")))
                .From(ao).Where(ao.UserId == au.Id).OrderBy(ao.Amount);
            ISubquery topN = dbms is Dbms.MySql or Dbms.Sqlite
                ? ordered.Limit(2)
                : ordered.OffsetRows(0).FetchNext(2);
            return Select(au.Id, x.Column("amount")).From(au).CrossApply(topN, x);
        });
        Add("OuterApply", _ => ApplyShape((b, sub, x) => b.OuterApply(sub, x)));
        Add("CrossJoinLateral", dbms =>
        {
            UsersTable lu = new("u");
            OrdersTable lo = new("o");
            DerivedTable x = new("x");
            ISelectBuilderOrderBy ordered = Select(lo.Amount.As(x.Column("amount")))
                .From(lo).Where(lo.UserId == lu.Id).OrderBy(lo.Amount);
            ISubquery topN = dbms switch
            {
                Dbms.Oracle => ordered.FetchFirst(2),
                Dbms.SqlServer => ordered.OffsetRows(0).FetchNext(2),
                _ => ordered.Limit(2),
            };
            return Select(lu.Id, x.Column("amount")).From(lu).CrossJoinLateral(topN, x);
        });
        Add("LeftJoinLateral", _ => ApplyShape((b, sub, x) => b.LeftJoinLateral(sub, x)));
        Add("JoinLateral", _ =>
        {
            UsersTable lu = new("u");
            OrdersTable lo = new("o");
            DerivedTable x = new("x");
            return Select(lu.Id, x.Column("amount")).From(lu)
                .JoinLateral(
                    Select(lo.Amount.As(x.Column("amount"))).From(lo).Where(lo.UserId == lu.Id),
                    x)
                .On(lu.Id == lu.Id);
        });

        // --- Full-text search ---
        // MySQL needs the sweep's FULLTEXT index on users(name); SQLite its FTS5
        // virtual table — both created by the per-engine PrepareEngine hook.
        Add("Match", dbms => dbms == Dbms.Sqlite
            ? SqliteFtsShape()
            : Select(u.Id).From(u).Where(Match(u.Name).Against("Alice")));
        Add("Against", _ => Select(u.Id).From(u).Where(Match(u.Name).Against("Alice")));
        Add("AgainstScore", _ => Select(Match(u.Name).AgainstScore("Alice")).From(u));
        AddSkips("ContainsScore",
            _ => Select(u.Id).From(u).Where(ContainsScore(u.Name, "Alice") > 0),
            (Dbms.Oracle, "No Oracle Text CONTEXT index in the container schema; the accept "
                + "side needs one."));
        // SCORE alone off Oracle: MySQL would reject the CONTAINS predicate first.
        AddSkips("Score",
            dbms => dbms == Dbms.Oracle
                ? Select(Score(1)).From(u).Where(ContainsScore(u.Name, "Alice", 1) > 0)
                : Select(Score(1)).From(u).Where(u.Id == 1),
            (Dbms.Oracle, "No Oracle Text CONTEXT index in the container schema; the accept "
                + "side needs one."));
        AddSkips("Contains",
            _ => Select(u.Id).From(u).Where(Contains(u.Name, "Alice")),
            (
                Dbms.SqlServer,
                "The mssql container image ships without Full-Text Search installed."));
        AddSkips("Freetext",
            _ => Select(u.Id).From(u).Where(Freetext(u.Name, "Alice")),
            (
                Dbms.SqlServer,
                "The mssql container image ships without Full-Text Search installed."));
        Add("TsMatch", _ => Select(u.Id).From(u)
            .Where(TsMatch(ToTsvector("english", u.Name), ToTsquery("english", "alice"))));
        // Each function alone, so a negative stops at its name rather than at @@.
        Add("ToTsvector", _ => Scalar(ToTsvector("english", u.Name)));
        Add("ToTsquery", _ => Scalar(ToTsquery("english", "alice")));
        Add("PlaintoTsquery", _ => Scalar(PlaintoTsquery("english", "alice")));

        // --- JSON ---
        Add("JsonExtract", _ => Scalar(JsonExtract(u.Data, "$.name")));
        Add("JsonValue", _ => Scalar(JsonValue(u.Data, "$.name")));
        Add("JsonQuery", _ => Scalar(JsonQuery(u.Data, "$.address")));
        Add("JsonArrow", dbms => dbms == Dbms.PostgreSql
            ? Scalar(JsonArrow(u.Data, "address"))
            : Scalar(JsonArrow(u.Data, "$.address")));
        Add("JsonArrowText", dbms => dbms == Dbms.PostgreSql
            ? Scalar(JsonArrowText(u.Data, "city"))
            : Scalar(JsonArrowText(u.Data, "$.city")));
        Add("JsonHashArrow", _ => Scalar(JsonHashArrow(u.Data, Cast("{address}", "text[]"))));
        Add("JsonHashArrowText", _ => Scalar(JsonHashArrowText(u.Data, Cast("{address,zip}", "text[]"))));
        Add("Array", _ => Scalar(Array("a", "b")));
        // Off PostgreSQL two columns stand in for the ARRAY[...] operands, so there the
        // operator is the one construct the engine reads.
        cases.Add(new SweepCase(new MatrixKey("ArrayOverlaps"),
            dbms => dbms == Dbms.PostgreSql
                ? WherePredicate(ArrayOverlaps(Array("a", "b"), Array("b", "c")))
                : WherePredicate(ArrayOverlaps(u.Name, u.Name)),
            NegativeSkips: new Dictionary<Dbms, string>
            {
                [Dbms.MySql] = "MySQL reads && as logical AND "
                    + "(MySqlTests.ArrayOverlaps_ReadsAsLogicalAnd).",
            }));
        Add("ArrayContains", dbms => dbms == Dbms.PostgreSql
            ? WherePredicate(ArrayContains(Array("a", "b"), Array("a")))
            : WherePredicate(ArrayContains(u.Name, u.Name)));
        Add("ArrayContainedBy", dbms => dbms == Dbms.PostgreSql
            ? WherePredicate(ArrayContainedBy(Array("a"), Array("a", "b")))
            : WherePredicate(ArrayContainedBy(u.Name, u.Name)));
        Add("JsonbContains", _ => WherePredicate(JsonbContains(u.Data, Cast("{\"name\":\"n\"}", "jsonb"))));
        // MySqlConnector binds the ? of these operators as a positional parameter, so on MySQL
        // the operator never reaches the engine, and no spelling of the key avoids the ?.
        Add("JsonbExists", _ => WherePredicate(JsonbExists(u.Data, "name")));
        Add("JsonbExistsAll", _ => WherePredicate(JsonbExistsAll(u.Data, "name", "address")));
        Add("JsonbExistsAny", _ => WherePredicate(JsonbExistsAny(u.Data, "name", "address")));
        // pgvector operators (#343): PostgreSQL alone casts the operands. Oracle converts bare
        // strings (docs/expressions.md); elsewhere they leave the operator the one construct.
        Add("L2Distance", dbms => dbms == Dbms.PostgreSql
            ? Scalar(L2Distance(Cast("[1,2]", "vector"), Cast("[3,4]", "vector")))
            : Scalar(L2Distance("[1,2]", "[3,4]")));
        cases.Add(new SweepCase(new MatrixKey("CosineDistance"),
            dbms => dbms == Dbms.PostgreSql
                ? Scalar(CosineDistance(Cast("[1,2]", "vector"), Cast("[3,4]", "vector")))
                : Scalar(CosineDistance("[1,2]", "[3,4]")),
            NegativeSkips: new Dictionary<Dbms, string>
            {
                [Dbms.MySql] = "MySQL reads <=> as NULL-safe equality "
                    + "(MySqlTests.CosineDistance_ReadsAsNullSafeEquality).",
            }));
        Add("NegativeInnerProduct", dbms => dbms == Dbms.PostgreSql
            ? Scalar(NegativeInnerProduct(Cast("[1,2]", "vector"), Cast("[3,4]", "vector")))
            : Scalar(NegativeInnerProduct("[1,2]", "[3,4]")));
        Add("L1Distance", dbms => dbms == Dbms.PostgreSql
            ? Scalar(L1Distance(Cast("[1,2]", "vector"), Cast("[3,4]", "vector")))
            : Scalar(L1Distance("[1,2]", "[3,4]")));
        Add("HammingDistance", dbms => dbms == Dbms.PostgreSql
            ? Scalar(HammingDistance(Cast("101", "bit(3)"), Cast("111", "bit(3)")))
            : Scalar(HammingDistance("101", "111")));
        Add("JaccardDistance", dbms => dbms == Dbms.PostgreSql
            ? Scalar(JaccardDistance(Cast("101", "bit(3)"), Cast("111", "bit(3)")))
            : Scalar(JaccardDistance("101", "111")));
        // The single array-typed bind (= ANY (:0)) doubles as the live proof of the
        // Dapper ArrayQueryParameter path.
        cases.Add(new SweepCase(new MatrixKey("BindArray"),
            _ => WherePredicate(u.Id == Any(BindArray([1, 2]))),
            NegativeSkips: new Dictionary<Dbms, string>
            {
                [Dbms.MySql] = "MySqlConnector rejects the Int32[] parameter itself "
                    + "(NotSupportedException), so the statement never reaches the engine.",
                [Dbms.Oracle] = "Oracle.ManagedDataAccess rejects the Int32[] parameter itself "
                    + "(InvalidCastException), so the statement never reaches the engine.",
                [Dbms.Sqlite] = "This form needs ANY, whose own cell is false, and BindArray "
                    + "alone fails at bind: after SQLite compiles the text, Microsoft.Data.Sqlite "
                    + "rejects the Int32[] parameter (InvalidOperationException, not a "
                    + "DbException).",
                [Dbms.SqlServer] = "The array-typed parameter is rejected client-side "
                    + "(no mapping exists from System.Int32[] to a known managed provider "
                    + "native type), so the statement never reaches the engine's grammar.",
            }));
        // An ARRAY[...] operand on PostgreSQL, not BindArray, so every parameter is a scalar;
        // elsewhere a bound scalar, so UNNEST is what the engine rejects.
        Add("Unnest", dbms =>
        {
            UnnestDerivedTable t = (dbms == Dbms.PostgreSql ? Unnest(Array("a", "b")) : Unnest("a"))
                .AsTable("t");
            return Select(t.Column("t")).From(t);
        });

        // --- Sequences ---

        // Member-level rows serve DbSequence's property on Oracle; the arity-1 rows are
        // the method alone, so Oracle's rejection of NEXTVAL('s') is swept there.
        Add("Nextval", dbms => dbms == Dbms.Oracle
            ? Select(Sequence("test_seq").Nextval).From(u).Where(u.Id == 1)
            : Select(Nextval("test_seq")).From(u).Where(u.Id == 1));
        AddArity("Nextval", 1, _ => Select(Nextval("test_seq")).From(u).Where(u.Id == 1));
        cases.Add(new SweepCase(new MatrixKey("Currval"),
            dbms => dbms == Dbms.Oracle
                ? Select(Sequence("test_seq").Currval).From(u).Where(u.Id == 1)
                : Select(Currval("test_seq")).From(u).Where(u.Id == 1),
            PositiveSkips: new Dictionary<Dbms, string>
            {
                [Dbms.Oracle] = "CURRVAL is session-state-dependent (defined only after NEXTVAL); the dedicated sequence test covers it.",
                [Dbms.PostgreSql] = "currval is session-state-dependent (defined only after nextval); the dedicated sequence test covers it.",
            }));
        cases.Add(new SweepCase(new MatrixKey("Currval", 1),
            _ => Select(Currval("test_seq")).From(u).Where(u.Id == 1),
            PositiveSkips: new Dictionary<Dbms, string>
            {
                [Dbms.PostgreSql] = "currval is session-state-dependent (defined only after nextval); the dedicated sequence test covers it.",
            }));
        Add("NextValueFor", _ => Select(NextValueFor("test_seq")).From(u).Where(u.Id == 1));
        Add("Sequence", _ => Select(Sequence("test_seq").Nextval).From(u).Where(u.Id == 1));

        // --- UPSERT ---
        AddMutating("OnConflict", _ => InsertInto(u, u.Id, u.Name).Values(1, "Sweep").OnConflict(u.Id).DoNothing());
        AddMutating("DoNothing", _ => InsertInto(u, u.Id, u.Name).Values(1, "Sweep").OnConflict(u.Id).DoNothing());
        AddMutating("DoUpdateSet", _ => InsertInto(u, u.Id, u.Name).Values(1, "Sweep").OnConflict(u.Id).DoUpdateSet(u.Name == "Sweep"));
        AddMutating("OnDuplicateKeyUpdate", _ => InsertInto(u, u.Id, u.Name).Values(1, "Sweep").OnDuplicateKeyUpdate(u.Name == Sql.Excluded(u.Name)));
        // Oracle and SQL Server have no ON CONFLICT, so there a MERGE carries the pseudo-row.
        AddMutating("Excluded", dbms => dbms switch
        {
            Dbms.MySql => InsertInto(u, u.Id, u.Name).Values(1, "Sweep").OnDuplicateKeyUpdate(
                u.Name == Sql.Excluded(u.Name)),
            Dbms.Oracle or Dbms.SqlServer => MergeExcludedShape(),
            _ => InsertInto(u, u.Id, u.Name).Values(1, "Sweep").OnConflict(u.Id).DoUpdateSet(
                u.Name == Sql.Excluded(u.Name)),
        });

        // --- MERGE ---
        AddMutating("MergeInto", _ => MergeShape());
        // MySQL and SQLite have no MERGE, so there the key's DELETE ... USING form isolates it.
        AddMutating("Using", dbms => dbms is Dbms.MySql or Dbms.Sqlite
            ? DeleteUsingShape()
            : MergeShape());
        // The literal-row upsert MERGE is the SQL Server hole this closes (also PostgreSQL);
        // Oracle rejects the VALUES source, and MySQL and SQLite, with no MERGE, read it in FROM.
        cases.Add(new SweepCase(new MatrixKey("Values", 3), dbms =>
        {
            UsersTable t = new("t");
            ValuesDerivedTable s = Values("s", ["id", "name"], [[701, "Sweep"]]);
            return dbms is Dbms.MySql or Dbms.Sqlite
                ? Select(s.Column("id")).From(s)
                : MergeInto(t).Using(s).On(t.Id == s.Column("id"))
                    .WhenMatched().ThenUpdateSet(t.Name == s.Column("name"))
                    .WhenNotMatched().ThenInsert(t.Id, t.Name)
                    .Values(s.Column("id"), s.Column("name"));
        }, Mutating: true));
        AddMutating("WhenMatched", _ => MergeUpdateShape());
        // The conditioned branch: Oracle rejects the AND-on-WHEN spelling (its filter is a
        // trailing WHERE on the action), so the arity entry is the negative the sweep proves.
        cases.Add(new SweepCase(new MatrixKey("WhenMatched", 1), _ =>
        {
            UsersTable t = new("t");
            UsersTable s = new("s");
            return MergeInto(t).Using(s).On(t.Id == s.Id)
                .WhenMatched(s.Age > 0).ThenUpdateSet(t.Name == s.Name);
        }, Mutating: true));
        cases.Add(new SweepCase(new MatrixKey("WhenNotMatched", 1), _ =>
        {
            UsersTable t = new("t");
            UsersTable s = new("s");
            return MergeInto(t).Using(s).On(t.Id == s.Id)
                .WhenNotMatched(s.Age > 0).ThenInsert(t.Id, t.Name).Values(s.Id, s.Name);
        }, Mutating: true));
        AddMutating("ThenUpdateSet", _ => MergeUpdateShape());
        AddMutating("WhenNotMatched", _ => MergeShape());
        AddMutating("ThenInsert", _ => MergeShape());
        // A 1:1 self-merge: a source with duplicate join keys (e.g. orders.user_id) makes
        // PostgreSQL fail at runtime with "MERGE command cannot affect row a second time".
        AddMutating("ThenDelete", _ =>
        {
            UsersTable t = new("t");
            UsersTable s = new("s");
            return MergeInto(t).Using(s).On(t.Id == s.Id).WhenMatched().ThenDelete();
        });
        AddMutating("WhenNotMatchedBySource", _ =>
        {
            UsersTable t = new("t");
            OrdersTable src = new("o");
            return MergeInto(t).Using(src).On(t.Id == src.UserId).WhenNotMatchedBySource()
                .ThenDelete();
        });
        AddMutating("DeleteWhere", _ =>
        {
            UsersTable t = new("t");
            UsersTable s = new("s");
            return MergeInto(t).Using(s).On(t.Id == s.Id)
                .WhenMatched().ThenUpdateSet(t.Name == s.Name).DeleteWhere(t.Age >= 50);
        });

        // --- RETURNING ---
        cases.Add(new SweepCase(new MatrixKey("Returning"),
            _ => InsertInto(u, u.Id, u.Name).Values(602, "Sweep").Returning(u.Id),
            Mutating: true,
            PositiveSkips: new Dictionary<Dbms, string>
            {
                [Dbms.Oracle] = "Oracle's RETURNING requires INTO with output-parameter binding; the dedicated Oracle test covers it.",
            }));

        // --- OUTPUT (SQL Server) — the other four engines reject it at the OUTPUT token ---
        AddMutating("Output", _ => InsertInto(u, u.Id, u.Name).Output(Inserted(u.Id)).Values(603, "Sweep"));
        AddMutating("Inserted", _ => InsertInto(u, u.Id, u.Name).Output(Inserted(u.Id)).Values(604, "Sweep"));
        AddMutating("Deleted", _ => DeleteFrom(u).Output(Deleted(u.Id)).Where(u.Id == -1));
        cases.Add(new SweepCase(
            new MatrixKey("Into", 2),
            _ =>
            {
                OutputArchiveTable archive = new();
                return DeleteFrom(u)
                    .Output(Deleted(u.Id), Deleted(u.Name))
                    .Into(archive, archive.Id, archive.Name)
                    .Where(u.Id == -1);
            },
            Mutating: true));

        return cases;

        ISqlBuilder ApplyShape(
            Func<ISelectBuilderFrom,
            ISubquery,
            DerivedTableBase,
            ISqlBuilder> apply)
        {
            UsersTable au = new("u");
            OrdersTable ao = new("o");
            DerivedTable x = new("x");
            ISelectBuilderFrom from = Select(au.Id, x.Column("amount")).From(au);
            return apply(
                from,
                (ISubquery)Select(ao.Amount.As(x.Column("amount"))).From(ao).Where(
                    ao.UserId == au.Id),
                x);
        }

        ISqlBuilder SqliteFtsShape()
        {
            DbTable fts = new("sweep_fts");
            return Select(fts.Column("name")).From(fts).Where(Match(fts, "alice"));
        }

        ISqlBuilder MergeShape()
        {
            UsersTable t = new("t");
            UsersTable s = new("s");
            return MergeInto(t)
                .Using(s)
                .On(t.Id == s.Id)
                .WhenMatched().ThenUpdateSet(t.Name == s.Name)
                .WhenNotMatched().ThenInsert(t.Id, t.Name).Values(s.Id, s.Name);
        }

        ISqlBuilder MergeExcludedShape()
        {
            UsersTable t = new("t");
            UsersTable s = new("s");
            return MergeInto(t).Using(s).On(t.Id == s.Id).WhenMatched().ThenUpdateSet(
                t.Name == Sql.Excluded(s.Name));
        }

        ISqlBuilder DeleteUsingShape()
        {
            OrdersTable d = new("o");
            UsersTable j = new("u");
            return DeleteFrom(d).Using(j).Where((d.UserId == j.Id) & (j.Id < 0));
        }

        ISqlBuilder MergeUpdateShape()
        {
            UsersTable t = new("t");
            UsersTable s = new("s");
            return MergeInto(t).Using(s).On(t.Id == s.Id).WhenMatched().ThenUpdateSet(
                t.Name == s.Name);
        }
    }
}
