using System.Diagnostics.CodeAnalysis;

namespace SqlArtisan.Internal;

// DML-target shape guards: most reject a target with no valid spelling on the
// resolved dialect (ADR 0011 — the deciding facts are builder state the
// analyzer cannot see); the joined-target alias rule is decided policy (#258).
internal static class DmlTargetGuard
{
    [DoesNotReturn]
    internal static void ThrowCorrelatedUnaliasedTarget() =>
        throw new ArgumentException(
            "The target of a correlated UPDATE, DELETE, or MERGE must be aliased.");

    internal static void ThrowIfAliasedOnSqlServer(DbTableBase table, Dbms dbms)
    {
        if (dbms == Dbms.SqlServer && table.HasAlias)
        {
            throw new ArgumentException(
                "SQL Server does not support aliasing the target of an INSERT, UPDATE, or DELETE "
                + "statement; use an unaliased target table — a correlated UPDATE or DELETE joins "
                + "through From(...) instead.");
        }
    }

    // A decided uniform requirement, not a grammar fact on every dialect
    // (#258; guards-and-empty-states.md, joined-target clause).
    internal static void ThrowIfJoinedTargetUnaliased(DbTableBase target)
    {
        if (!target.HasAlias)
        {
            throw new ArgumentException(
                "The target of a joined UPDATE or DELETE must be aliased.");
        }
    }

    // The joined DELETE ... FROM leads with the target's alias and introduces the
    // target through FROM, so the target must be re-listed there — otherwise the
    // lead keeps `DELETE FROM target` and a second FROM follows, invalid on every
    // dialect. A wrong-dialect joined form is emitted faithfully and left to the
    // database (ADR 0001); only this structurally-broken case throws.
    internal static void ThrowIfJoinedDeleteTargetNotRepeated(DmlJoinState state)
    {
        if (state.HasFrom && !state.TargetRepeatedInFrom)
        {
            throw new ArgumentException(
                "A joined DELETE ... FROM must re-list the target table in the FROM clause.");
        }
    }

    // MySQL's INSERT grammar has no target-alias slot (the 8.0.19+ `AS row_alias`
    // is a post-VALUES construct), so an aliased INSERT target has no MySQL
    // spelling — ADR 0011, scoped to INSERT (aliased UPDATE/DELETE are valid, #255).
    internal static void ThrowIfInsertTargetAliasedOnMySql(DbTableBase table, Dbms dbms)
    {
        if (dbms == Dbms.MySql && table.HasAlias)
        {
            throw new ArgumentException(
                "MySQL does not support aliasing the target of an INSERT statement; use an "
                    + "unaliased target table.");
        }
    }

    // SQL Server's joined UPDATE/DELETE spelling requires the target re-listed in
    // FROM; PostgreSQL's FROM/USING forms legally leave it out, so the requirement
    // is T-SQL's alone and is checked at Build(Dbms) (ADR 0011).
    internal static void ThrowIfSqlServerJoinedTargetNotRepeated(
        DmlJoinState state, Dbms dbms, string statementName)
    {
        if (dbms == Dbms.SqlServer && !state.TargetRepeatedInFrom)
        {
            // The direct-join chain has no FROM to re-list in, so the remedy
            // names the chain that does.
            string remedy = state.HasFrom
                ? "re-list the target table in the FROM clause"
                : "join through From(target, ...), re-listing the target table";

            throw new ArgumentException(
                $"A joined {statementName} on SQL Server must {remedy}.");
        }
    }

    // T-SQL has no DELETE ... USING at all, so the re-list remedy the joined-
    // target guard names is unreachable from a Using(...) chain; this names the
    // way out first. The same ADR 0011 shape: no valid spelling on the dialect.
    internal static void ThrowIfSqlServerDeleteUsing(DeleteUsingClause? usingClause, Dbms dbms)
    {
        if (dbms == Dbms.SqlServer && usingClause is not null)
        {
            throw new ArgumentException(
                "SQL Server has no DELETE ... USING form; join through From(...), "
                + "re-listing the target table.");
        }
    }

    // The mirror of the guard above, UPDATE only: a re-listed target renders the
    // bare-alias lead, T-SQL's spelling alone (live-verified rejection on SQLite);
    // instance identity is invisible to the analyzer (ADR 0011). DELETE stays permissive.
    internal static void ThrowIfUpdateTargetRepeatedOffSqlServer(DmlJoinState state, Dbms dbms)
    {
        if (dbms != Dbms.SqlServer && state.TargetRepeatedInFrom)
        {
            throw new ArgumentException(
                "Only SQL Server supports a joined UPDATE that re-lists the target table in "
                    + "the FROM clause.");
        }
    }

    // Oracle's DML grammars carry no subquery-factoring clause and MySQL's INSERT
    // does not either (live: 8.0.46 ER_PARSE_ERROR), so the CTE belongs inside the
    // feeding SELECT. The leading part is builder state the analyzer cannot see (ADR 0011).
    internal static void ThrowIfLeadingWithUnsupported(
        ReadOnlySpan<SqlPart> parts, Dbms dbms, bool insert)
    {
        if (parts.Length == 0 || parts[0] is not (WithClause or WithRecursiveClause))
        {
            return;
        }

        if (dbms == Dbms.Oracle)
        {
            throw new ArgumentException(
                "Oracle has no leading WITH on INSERT, UPDATE, or DELETE; for an INSERT, put "
                + "the CTE inside the feeding SELECT (InsertInto(...).With(...).Select(...)), "
                + "otherwise inline the subquery.");
        }

        if (dbms == Dbms.MySql && insert)
        {
            throw new ArgumentException(
                "MySQL has no leading WITH on INSERT; put the CTE inside the feeding SELECT "
                + "(InsertInto(...).With(...).Select(...)), otherwise inline the subquery.");
        }
    }

    // OUTPUT ... INTO is SQL Server-only, and its destination is a plain
    // INSERT target (FormatAsDmlTarget) — an alias there renders as
    // `INTO archive AS "a" (...)`, which T-SQL rejects the same way it rejects
    // an aliased primary DML target. The alias is fixed at the call, so this
    // throws eagerly rather than waiting for Build(Dbms).
    internal static void ThrowIfOutputIntoTargetAliased(DbTableBase table)
    {
        if (table.HasAlias)
        {
            throw new ArgumentException(
                "The destination table of OUTPUT ... INTO must not be aliased.");
        }
    }
}
