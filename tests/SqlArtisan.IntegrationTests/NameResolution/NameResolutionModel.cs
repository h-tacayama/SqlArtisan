using SqlArtisan.IntegrationTests.ValueReading;

namespace SqlArtisan.IntegrationTests.NameResolution;

/// <summary>Where a column reference is rendered. Each builder-stage method maps to one
/// or more of these (<see cref="NameResolutionModel.PositionsOf"/>); the nested ones come
/// from the <c>Sql</c> factories that take a column (OrderBy, PartitionBy, pseudo-rows).</summary>
public enum NrPosition
{
    SelectItem,
    Where,
    JoinOn,
    JoinUsing,
    GroupBy,
    Having,
    OrderBy,
    CompoundOrderBy,
    DistinctOn,
    WindowPartition,
    WindowOrder,
    AggregateOrder,
    ForUpdateOf,
    UpdateSetTarget,
    UpdateSetValue,
    DmlWhere,
    DmlJoinOn,
    InsertColumnList,
    InsertValue,
    InsertSetTarget,
    ConflictTarget,
    UpsertSetTarget,
    UpsertSetValue,
    UpsertWhere,
    MergeOn,
    MergeBranchCondition,
    MergeSetTarget,
    MergeSetValue,
    MergeInsertList,
    MergeInsertValue,
    Returning,
    Output,
    OutputIntoList,
}

/// <summary>How the reference is spelled in C#, which fixes how it renders.</summary>
public enum NrRef
{
    QualifiedColumn, // owner aliased: "t".b
    BareColumn,      // owner unaliased: b
    SelectAlias,     // an ExpressionAlias key: "x"
    PseudoRow,       // Excluded / Inserted / Deleted (col)
    TableName,       // a relation named by its alias or name: Of(table)
}

/// <summary>Where the reference's owner is listed, relative to the block it sits in.</summary>
public enum NrOwner
{
    Own,         // the block's own FROM / join, or the DML target in a target position
    OuterListed, // an enclosing block lists it (a correlation)
    Unlisted,    // no reachable block lists it
    OtherTable,  // a target or pseudo-row position given another table's column
}

/// <summary>What else in scope carries the rendered name.</summary>
public enum NrShadow
{
    None,
    SiblingColumn, // another relation of the same block has a same-named column
    InnerColumn,   // the reference's own (inner) block has a same-named column
    SelectAlias,   // a select alias of the block has the name / an input column has the alias's
    SameAlias,     // another relation (sibling or inner) carries the owner's alias
    SameTableName, // an inner relation is the owner's table, unaliased (SQLite RETURNING)
    UsingColumn,   // the column is merged by the join's USING list
    SameAliasCase, // an inner relation's alias differs from the owner's in case only
    SameNameCte,   // a CTE in scope carries the owner's table name
}

public enum NrBlock
{
    Top,
    PredicateSubquery,
    ScalarSubquery,
    DerivedTable,
    CteBody,
    Lateral,
    CompoundBranch,
}

public enum NrOutcome
{
    Binds,         // runs and reads the owner's column
    BuildRejects,
    CompileStops,
    EngineRejects, // fails loudly on the engine
    AnalyzerWarns,
    Permissive,    // emitted as written by a record's decision; the engine picks the column
    Question,
    Delegated,     // the construct itself is the dialect matrix's (SQLA0100)
    NotApplicable,
}

public sealed record NrCell(
    NrOutcome Outcome,
    string Basis,
    string? QuestionId = null,
    string? Diagnostic = null);

public sealed record NrKey(
    NrPosition Position,
    NrRef Ref,
    NrOwner Owner,
    NrShadow Shadow,
    NrBlock Block);

/// <summary>Trial step 3: the name-resolution hazard model. One cell per position x
/// reference x owner x shadow x block x lane; the expected result follows the records
/// (§ 14), and a cell no record decides is a question (trials/name-resolution/REPORT.md).</summary>
public static class NameResolutionModel
{
    private const string Qualify = "DbColumn.Format: qualified only by the owner's correlation "
        + "name (guards § Joined-target alias requirement)";
    private const string Rule677 = "guards § Every unaliased table a nested block reads without "
        + "listing it is rejected (#677)";
    private const string Rule253 = "guards § Each nested block defers the correlated-DML guard "
        + "(#253, #607)";
    private const string Rule674 = "guards § A bare SET target is a column of the statement's "
        + "target table (#674)";
    private const string Rule675 = "guards § same paragraph, pseudo-row accessors (#675, #708)";
    private const string Rule672 = "guards § A sort key emitted as a bare name (#672); "
        + "docs/query-statements.md § ORDER BY [!WARNING]";
    private const string TopPermissive = "guards § #677 paragraph: a top-level column whose "
        + "table no clause lists stays permissive";
    private const string Rule595 = "guards § A same-named relation under SQLite's RETURNING "
        + "(#595)";
    private const string Rule721 = "CHANGELOG/#721: an unaliased upsert target's columns are "
        + "qualified in DO UPDATE";
    private const string RuleJoined = "guards § Joined-target alias requirement (#258)";
    private const string Rule669 = "guards § An OUTPUT item reading the target bare (#669)";
    private const string Ambiguous = "SQL standard: a column reference names exactly one "
        + "column in scope (not checked against each engine's manual)";
    private const string Missing = "SQL standard: a qualifier names a range variable in "
        + "scope (not checked against each engine's manual)";
    private const string ObjectTyped = "C# types: the position takes SqlExpression / "
        + "SqlCondition / DbColumn, which ExpressionAlias is not";

    public static readonly IReadOnlyDictionary<string, string> Questions =
        new Dictionary<string, string>
        {
            ["N1"] = "A bare column of an unaliased table colliding with a select alias in "
                + "GROUP BY / HAVING / WHERE: which one should it read, and should the "
                + "library act? No record covers these positions (#672 covers ORDER BY only).",
            ["N2"] = "The same collision inside OVER / WITHIN GROUP / an aggregate's ORDER BY "
                + "on MySQL, Oracle and SQL Server; #672 records PostgreSQL and SQLite only.",
            ["N3"] = "An inner relation carrying the outer owner's alias (FROM t \"x\" ... "
                + "(SELECT ... FROM s \"x\" WHERE \"x\".b = ...)): the qualified reference "
                + "binds the inner relation. No record.",
            ["N4"] = "Two relations of one block carrying the same alias. No record.",
            ["N5"] = "A qualified reference to a column merged by USING (Oracle ORA-25154) "
                + "or NATURAL. No record.",
            ["N6"] = "Using(col) whose owner is neither joined relation renders USING (b) and "
                + "joins on whatever the two sides call b. No owner check, no record.",
            ["N7"] = "An ExpressionAlias passed to GroupBy / PartitionBy / DistinctOn. "
                + "No record of what the library or engines do.",
            ["N8"] = "A select alias in a compound's trailing ORDER BY that only a later "
                + "branch names: #672 records PostgreSQL (first branch) and SQLite (any).",
            ["N9"] = "FOR UPDATE OF a table the block does not list. No record.",
            ["N11"] = "A bare target column in MySQL's ON DUPLICATE KEY UPDATE value, beside "
                + "the AS new row alias the library always emits. No record.",
            ["N12"] = "A CTE named after a base table: every unqualified table reference in the "
                + "statement reads the CTE. No record (found by the independent review).",
            ["N13"] = "An inner relation aliased with the pseudo-row's name (MySQL `new`) "
                + "captures Excluded(...). No record (found by the independent review).",
            ["N14"] = "A column reference inside an INSERT ... VALUES row. No record.",
            ["N10"] = "A bare column of a top-level compound branch whose table only another "
                + "branch lists: is the branch top-level for #677?",
        };

    public static IEnumerable<NrPosition> PositionsOf(string stage, string method, string sig)
    {
        bool select = stage.StartsWith("ISelectBuilder", StringComparison.Ordinal);
        bool merge = stage.StartsWith("IMergeBuilder", StringComparison.Ordinal);
        bool insert = stage.StartsWith("IInsert", StringComparison.Ordinal);
        switch (method)
        {
            case "Select":
                yield return NrPosition.SelectItem;
                yield return NrPosition.DistinctOn;
                break;
            case "Where" when select:
                yield return NrPosition.Where;
                break;
            case "Where" when stage == "IInsertBuilderDoUpdateSet":
                yield return NrPosition.UpsertWhere;
                break;
            case "Where" when merge:
            case "DeleteWhere":
                yield return NrPosition.MergeBranchCondition;
                break;
            case "Where":
                yield return NrPosition.DmlWhere;
                break;
            case "On" when select:
                yield return NrPosition.JoinOn;
                break;
            case "On" when merge:
                yield return NrPosition.MergeOn;
                break;
            case "On":
                yield return NrPosition.DmlJoinOn;
                break;
            case "Using" when stage.EndsWith("JoinOn", StringComparison.Ordinal)
                || stage == "ISelectBuilderJoin":
                yield return NrPosition.JoinUsing;
                break;
            case "GroupBy":
                yield return NrPosition.GroupBy;
                break;
            case "Having":
                yield return NrPosition.Having;
                break;
            case "OrderBy":
                yield return NrPosition.OrderBy;
                yield return NrPosition.CompoundOrderBy;
                break;
            case "WhenMatched" or "WhenNotMatched" or "WhenNotMatchedBySource":
                yield return NrPosition.MergeBranchCondition;
                break;
            case "Set" when insert:
                yield return NrPosition.InsertSetTarget;
                yield return NrPosition.InsertValue;
                break;
            case "Set":
                yield return NrPosition.UpdateSetTarget;
                yield return NrPosition.UpdateSetValue;
                break;
            case "DoUpdateSet" or "OnDuplicateKeyUpdate":
                yield return NrPosition.UpsertSetTarget;
                yield return NrPosition.UpsertSetValue;
                break;
            case "ThenUpdateSet":
                yield return NrPosition.MergeSetTarget;
                yield return NrPosition.MergeSetValue;
                break;
            case "InsertInto" or "InsertIgnoreInto" when sig.Contains("DbColumn"):
                yield return NrPosition.InsertColumnList;
                break;
            case "ThenInsert":
                yield return NrPosition.MergeInsertList;
                break;
            case "OnConflict":
                yield return NrPosition.ConflictTarget;
                break;
            case "Into" when stage.EndsWith("OutputInto", StringComparison.Ordinal):
                yield return NrPosition.OutputIntoList;
                break;
            case "Values" when merge:
                yield return NrPosition.MergeInsertValue;
                break;
            case "Values":
                yield return NrPosition.InsertValue;
                break;
            case "Output":
                yield return NrPosition.Output;
                break;
            case "Returning" when stage != "IReturningBuilder":
                yield return NrPosition.Returning;
                break;
            case "ForUpdate":
                yield return NrPosition.ForUpdateOf;
                break;
            default:
                // FROM / joins / CTEs / pagination / branch keywords introduce or omit
                // names; none renders a column reference of its own.
                break;
        }
    }

    public static bool IsNested(NrBlock b) =>
        b is not (NrBlock.Top or NrBlock.CompoundBranch);

    private static readonly NrPosition[] QueryPositions =
    [
        NrPosition.SelectItem, NrPosition.Where, NrPosition.JoinOn, NrPosition.JoinUsing,
        NrPosition.GroupBy, NrPosition.Having, NrPosition.OrderBy, NrPosition.DistinctOn,
        NrPosition.WindowPartition, NrPosition.WindowOrder, NrPosition.AggregateOrder,
    ];

    private static readonly NrPosition[] TargetPositions =
    [
        NrPosition.UpdateSetTarget, NrPosition.InsertColumnList, NrPosition.InsertSetTarget,
        NrPosition.ConflictTarget, NrPosition.UpsertSetTarget, NrPosition.MergeSetTarget,
        NrPosition.MergeInsertList, NrPosition.OutputIntoList,
    ];

    private static readonly NrPosition[] AliasKeyPositions =
    [
        NrPosition.OrderBy, NrPosition.CompoundOrderBy, NrPosition.GroupBy,
        NrPosition.DistinctOn, NrPosition.WindowPartition, NrPosition.WindowOrder,
        NrPosition.AggregateOrder,
    ];

    /// <summary>Whether the combination is a reference that can be written at all; the
    /// rest are no cell (an owner that cannot sit there, a block the position lacks).</summary>
    public static bool Applies(NrKey k)
    {
        bool query = QueryPositions.Contains(k.Position);
        bool target = TargetPositions.Contains(k.Position);

        if ((k.Ref == NrRef.TableName) != (k.Position == NrPosition.ForUpdateOf
            && k.Ref is not (NrRef.QualifiedColumn or NrRef.BareColumn)))
        {
            return false;
        }

        if (k.Ref == NrRef.TableName)
        {
            return k.Owner is NrOwner.Own or NrOwner.Unlisted && k.Shadow == NrShadow.None
                && k.Block == NrBlock.Top;
        }

        if (k.Position == NrPosition.CompoundOrderBy && k.Block != NrBlock.CompoundBranch)
        {
            return false;
        }

        if (!query && k.Position != NrPosition.CompoundOrderBy
            && k.Block is not (NrBlock.Top or NrBlock.PredicateSubquery))
        {
            return false; // a DML position's nested block is a subquery inside it
        }

        if (target && k.Block != NrBlock.Top)
        {
            return false;
        }

        if (k.Owner == NrOwner.OtherTable)
        {
            return (target || k.Ref == NrRef.PseudoRow) && k.Shadow == NrShadow.None;
        }

        if (k.Owner == NrOwner.OuterListed && !IsNested(k.Block))
        {
            return false;
        }

        if (k.Ref == NrRef.PseudoRow && k.Shadow == NrShadow.SameAlias)
        {
            return k.Owner == NrOwner.Own && k.Block == NrBlock.PredicateSubquery
                && k.Position == NrPosition.UpsertSetValue;
        }

        if (k.Ref == NrRef.PseudoRow)
        {
            return k.Owner == NrOwner.Own && k.Shadow == NrShadow.None
                && !target && k.Position is not (NrPosition.JoinUsing or NrPosition.ForUpdateOf);
        }

        if (k.Ref == NrRef.SelectAlias)
        {
            return k.Owner is NrOwner.Own or NrOwner.Unlisted
                && k.Shadow is NrShadow.None or NrShadow.SelectAlias
                && k.Block is NrBlock.Top or NrBlock.CompoundBranch or NrBlock.DerivedTable;
        }

        return k.Shadow switch
        {
            NrShadow.InnerColumn => IsNested(k.Block) && k.Owner != NrOwner.Own,
            NrShadow.SelectAlias => query || k.Position == NrPosition.CompoundOrderBy,
            NrShadow.SameTableName => k.Position == NrPosition.Returning
                && k.Ref == NrRef.QualifiedColumn && k.Block == NrBlock.PredicateSubquery,
            NrShadow.UsingColumn => k.Position is NrPosition.SelectItem or NrPosition.Where
                && k.Owner == NrOwner.Own && k.Block == NrBlock.Top,
            NrShadow.SameAlias or NrShadow.SameAliasCase => k.Ref == NrRef.QualifiedColumn,
            NrShadow.SameNameCte => (query || target || k.Position == NrPosition.DmlWhere)
                && k.Block is NrBlock.Top or NrBlock.PredicateSubquery,
            _ => true,
        };
    }

    public static NrCell Expect(NrKey k, Lane lane)
    {
        Dbms d = ValueReadingModel.DbmsOf(lane);
        bool target = TargetPositions.Contains(k.Position);

        if (k.Ref == NrRef.SelectAlias)
        {
            return AliasCell(k, d);
        }

        if (k.Shadow == NrShadow.SameNameCte)
        {
            return new(NrOutcome.Question, "not recorded", "N12");
        }

        if (k.Ref == NrRef.PseudoRow && k.Shadow == NrShadow.SameAlias)
        {
            return new(NrOutcome.Question, "not recorded", "N13");
        }

        if (k.Ref == NrRef.PseudoRow)
        {
            return k.Owner == NrOwner.OtherTable
                ? new(NrOutcome.BuildRejects, Rule675)
                : k.Position is NrPosition.UpsertSetValue or NrPosition.Output
                    ? new(NrOutcome.Delegated, Rule675 + "; availability is the dialect matrix's")
                    : new(NrOutcome.AnalyzerWarns, "guards § #675 paragraph: where one sits "
                        + "stays the engine's; SQLA0102 reports Inserted/Deleted outside OUTPUT",
                        Diagnostic: "SQLA0102 / engine");
        }

        if (target)
        {
            return k.Owner == NrOwner.OtherTable
                ? new(NrOutcome.BuildRejects, Rule674)
                : new(NrOutcome.Binds, Rule674);
        }

        if (k.Shadow == NrShadow.SameTableName)
        {
            return d == Dbms.Sqlite
                ? new(NrOutcome.EngineRejects, Rule595)
                : new(NrOutcome.Binds, Qualify);
        }

        if (k.Shadow == NrShadow.UsingColumn)
        {
            return k.Ref == NrRef.QualifiedColumn
                ? new(NrOutcome.Question, "not recorded", "N5")
                : new(NrOutcome.Binds, "a USING column is one column of the join (standard)");
        }

        if (k.Position == NrPosition.JoinUsing)
        {
            return k.Owner == NrOwner.Own
                ? new(NrOutcome.Binds, "JoinUsingClause renders the names unqualified")
                : new(NrOutcome.Question, "not recorded", "N6");
        }

        if (k.Position == NrPosition.ForUpdateOf && k.Ref != NrRef.TableName
            && d != Dbms.Oracle)
        {
            return new(NrOutcome.Delegated, "DialectMatrix: Of(column) is Oracle's form only",
                Diagnostic: "SQLA0100");
        }

        if (k.Ref == NrRef.TableName && d == Dbms.Oracle)
        {
            return new(NrOutcome.Delegated, "DialectMatrix: Of(table) is SQLA0100 on Oracle, "
                + "Of(column) its form", Diagnostic: "SQLA0100");
        }

        if (k.Position == NrPosition.ForUpdateOf && k.Owner != NrOwner.Own)
        {
            return new(NrOutcome.Question, "not recorded", "N9");
        }

        if (k.Position == NrPosition.InsertValue)
        {
            return new(NrOutcome.Question, "not recorded", "N14");
        }

        return k.Ref == NrRef.BareColumn ? BareCell(k, d) : QualifiedCell(k, d);
    }

    private static NrCell BareCell(NrKey k, Dbms d)
    {
        if (k.Position == NrPosition.DmlJoinOn && k.Owner == NrOwner.Own)
        {
            return new(NrOutcome.BuildRejects, RuleJoined);
        }

        if (IsNested(k.Block) && k.Owner != NrOwner.Own)
        {
            bool dmlTarget = k.Position is NrPosition.UpdateSetValue or NrPosition.DmlWhere
                or NrPosition.UpsertSetValue or NrPosition.Returning or NrPosition.Output
                or NrPosition.MergeOn or NrPosition.MergeSetValue;
            return new(NrOutcome.BuildRejects, dmlTarget ? Rule253 : Rule677);
        }

        if (k.Owner == NrOwner.Unlisted)
        {
            return k.Block == NrBlock.CompoundBranch
                ? new(NrOutcome.Question, "not recorded", "N10")
                : k.Shadow == NrShadow.SiblingColumn
                    ? new(NrOutcome.Permissive, TopPermissive)
                    : new(NrOutcome.EngineRejects, TopPermissive + "; no column of the name");
        }

        if (k.Shadow == NrShadow.SiblingColumn)
        {
            return new(NrOutcome.EngineRejects, Ambiguous);
        }

        if (k.Shadow == NrShadow.SelectAlias)
        {
            return k.Position switch
            {
                NrPosition.OrderBy or NrPosition.CompoundOrderBy
                    => new(NrOutcome.Permissive, Rule672),
                NrPosition.WindowOrder or NrPosition.WindowPartition
                    or NrPosition.AggregateOrder when d is Dbms.PostgreSql or Dbms.Sqlite
                    => new(NrOutcome.Binds, Rule672 + " (PostgreSQL and SQLite read no alias)"),
                NrPosition.WindowOrder or NrPosition.WindowPartition
                    or NrPosition.AggregateOrder => new(NrOutcome.Question, "not recorded", "N2"),
                NrPosition.SelectItem or NrPosition.JoinOn
                    => new(NrOutcome.Binds, "a select alias is not in scope of its own list "
                        + "or of FROM (standard)"),
                _ => new(NrOutcome.Question, "not recorded", "N1"),
            };
        }

        if (k.Position == NrPosition.UpsertSetValue)
        {
            // #721 decides ON CONFLICT only; MySQL's ODKU beside its AS new row alias has
            // no record (the first CI run had assumed Binds here: a model error).
            return d == Dbms.MySql
                ? new(NrOutcome.Question, "not recorded", "N11")
                : new(NrOutcome.Binds, Rule721);
        }

        if (k.Position == NrPosition.Output && d == Dbms.SqlServer)
        {
            return new(NrOutcome.EngineRejects, Rule669);
        }

        return new(NrOutcome.Binds, Qualify + "; a bare name binds the block's own relation");
    }

    private static NrCell QualifiedCell(NrKey k, Dbms d)
    {
        if (k.Owner == NrOwner.Unlisted)
        {
            return new(NrOutcome.EngineRejects, Missing);
        }

        if (k.Shadow is NrShadow.SameAlias or NrShadow.SameAliasCase)
        {
            return k.Owner == NrOwner.OuterListed
                ? new(NrOutcome.Question, "not recorded", "N3")
                : new(NrOutcome.Question, "not recorded", "N4");
        }

        if (k.Position == NrPosition.CompoundOrderBy)
        {
            return d == Dbms.Sqlite
                ? new(NrOutcome.Binds, "value-reading model Q13 cell (SQLite lang_select)")
                : d == Dbms.PostgreSql
                    ? new(NrOutcome.EngineRejects, "value-reading model (PostgreSQL select.sgml)")
                    : new(NrOutcome.Question, "not recorded", "Q13");
        }

        if (k.Position == NrPosition.Output && d == Dbms.SqlServer)
        {
            return new(NrOutcome.EngineRejects, Rule669);
        }

        return new(NrOutcome.Binds, Qualify);
    }

    private static NrCell AliasCell(NrKey k, Dbms d)
    {
        if (!AliasKeyPositions.Contains(k.Position))
        {
            return new(NrOutcome.CompileStops, ObjectTyped);
        }

        if (k.Position is NrPosition.WindowOrder or NrPosition.AggregateOrder)
        {
            return new(NrOutcome.BuildRejects, Rule672);
        }

        if (k.Position is NrPosition.GroupBy or NrPosition.DistinctOn
            or NrPosition.WindowPartition)
        {
            return new(NrOutcome.Question, "not recorded", "N7");
        }

        if (k.Position == NrPosition.CompoundOrderBy)
        {
            return k.Owner == NrOwner.Own
                ? new(NrOutcome.Binds, Rule672 + " (an alias the first branch selects)")
                : new(NrOutcome.Question, "not recorded beyond PostgreSQL / SQLite", "N8");
        }

        return k.Owner == NrOwner.Own
            ? new(NrOutcome.Binds, Rule672 + " (an alias the block selects is valid)")
            : new(NrOutcome.Permissive, Rule672);
    }

    public static IEnumerable<NrKey> AllKeys() =>
        from p in Enum.GetValues<NrPosition>()
        from r in Enum.GetValues<NrRef>()
        from o in Enum.GetValues<NrOwner>()
        from s in Enum.GetValues<NrShadow>()
        from b in Enum.GetValues<NrBlock>()
        let k = new NrKey(p, r, o, s, b)
        where Applies(k)
        select k;
}
