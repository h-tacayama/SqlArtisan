using System.Diagnostics.CodeAnalysis;

namespace SqlArtisan.Internal;

// The guards for a DML target that has no correct spelling on the resolved
// dialect — an aliased target on SQL Server (the ADR 0011 bounded exception to
// ADR 0007's permissive default, since the alias is a value the analyzer cannot
// see) and an unaliased correlated target, whose columns resolve to the inner
// scope on every dialect (#253).
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

    // A joined UPDATE/DELETE qualifies its columns through the target's alias
    // (and SQL Server / MySQL lead with the alias alone), so an unaliased target
    // has no correct spelling in the joined forms.
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
