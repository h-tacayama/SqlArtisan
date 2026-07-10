using System.Diagnostics.CodeAnalysis;

namespace SqlArtisan.Internal;

// The Build()-time guard shared by InsertBuilder, UpdateBuilder, and
// DeleteBuilder for an aliased INSERT/UPDATE/DELETE target on SQL Server. T-SQL
// cannot alias the target directly (the alias must come from a FROM clause — the
// joined-DML form), so the text SqlArtisan would emit, `UPDATE users AS "cu" ...`,
// is a syntax error there. The exact text is valid on PostgreSQL (an aliased
// INSERT target is how ON CONFLICT is written) and MySQL, which by ADR 0007 makes
// it dialect availability (normally permissive) — but the alias is a value-level
// constructor argument the opt-in analyzer cannot see, and SQL Server has no
// valid spelling at all until joined DML lands (#237), so this narrow case is
// the bounded exception to ADR 0007 recorded in ADR 0011: fail loudly at Build()
// rather than emit SQL that can never be correct on the resolved target.
// The correlated-unaliased throw below is the same family's other half (#253):
// a target column inside a subquery resolves to the inner scope on every
// dialect, so the unaliased correlated form has no correct spelling either.
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
}
