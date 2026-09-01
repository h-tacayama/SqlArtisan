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
            "The target of a correlated UPDATE or DELETE must be aliased.");

    internal static void ThrowIfAliasedOnSqlServer(DbTableBase table, Dbms dbms)
    {
        if (dbms == Dbms.SqlServer && table.HasAlias)
        {
            throw new ArgumentException(
                "SQL Server does not support aliasing the target of an INSERT, UPDATE, or DELETE statement; use an unaliased target table.");
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

    // MySQL's INSERT grammar has no target-alias slot at all (the 8.0.19+
    // `AS row_alias` is a different, post-VALUES construct), so an aliased
    // INSERT target has no valid MySQL spelling — the same ADR 0011 shape as
    // the SQL Server guard above, scoped to INSERT alone because MySQL's
    // aliased UPDATE/DELETE targets are valid (live-verified, #255).
    internal static void ThrowIfInsertTargetAliasedOnMySql(DbTableBase table, Dbms dbms)
    {
        if (dbms == Dbms.MySql && table.HasAlias)
        {
            throw new ArgumentException(
                "MySQL does not support aliasing the target of an INSERT statement; use an unaliased target table.");
        }
    }

    // SQL Server's joined UPDATE/DELETE spelling requires the target re-listed
    // in FROM (the lead is then the alias alone); PostgreSQL's FROM/USING forms
    // legally leave it out, so the requirement is T-SQL's alone and is checked
    // at Build(Dbms) — the same ADR 0011 shape as the aliased-target guard.
    internal static void ThrowIfSqlServerJoinedTargetNotRepeated(
        DmlJoinState state, Dbms dbms, string statementName)
    {
        if (dbms == Dbms.SqlServer && !state.TargetRepeatedInFrom)
        {
            throw new ArgumentException(
                $"A joined {statementName} on SQL Server must re-list the target table in the FROM clause.");
        }
    }

    // The mirror of the guard above, UPDATE only: re-listing the target in FROM
    // makes the lead render as the bare alias, which is T-SQL's spelling alone —
    // no other dialect resolves it (live-verified rejection on SQLite). The
    // re-listing is instance identity, invisible to the analyzer (ADR 0011).
    // A joined DELETE stays permissive: its repeated-FROM form is also MySQL's.
    internal static void ThrowIfUpdateTargetRepeatedOffSqlServer(DmlJoinState state, Dbms dbms)
    {
        if (dbms != Dbms.SqlServer && state.TargetRepeatedInFrom)
        {
            throw new ArgumentException(
                "Only SQL Server supports a joined UPDATE that re-lists the target table in the FROM clause.");
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
