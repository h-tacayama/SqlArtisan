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

    // The joined UPDATE spellings are construct-level per-dialect and share the
    // From/Join member names the analyzer keys on, so it cannot tell them apart;
    // reject a shape the resolved target has no valid spelling for at Build (ADR
    // 0011). SQL Server repeats the target in FROM; PostgreSQL/SQLite use
    // UPDATE ... FROM without repeating it; MySQL joins before SET.
    internal static void ThrowIfJoinedUpdateShapeInvalid(DmlJoinState state, Dbms dbms)
    {
        if (state.TargetRepeatedInFrom)
        {
            if (dbms != Dbms.SqlServer)
            {
                throw ShapeError("UPDATE ... FROM with a repeated target", dbms);
            }
        }
        else if (state.HasFrom)
        {
            if (dbms is not (Dbms.PostgreSql or Dbms.Sqlite))
            {
                throw ShapeError("UPDATE ... FROM", dbms);
            }
        }
        else if (dbms != Dbms.MySql)
        {
            throw ShapeError("a multi-table UPDATE ... JOIN", dbms);
        }
    }

    internal static void ThrowIfJoinedDeleteShapeInvalid(DmlJoinState state, Dbms dbms)
    {
        if (state.HasUsing)
        {
            if (dbms != Dbms.PostgreSql)
            {
                throw ShapeError("DELETE ... USING", dbms);
            }
        }
        else if (!state.TargetRepeatedInFrom)
        {
            throw new ArgumentException(
                "A joined DELETE ... FROM must re-list the target table in the FROM clause.");
        }
        else if (dbms is not (Dbms.SqlServer or Dbms.MySql))
        {
            throw ShapeError("a multi-table DELETE ... FROM", dbms);
        }
    }

    private static ArgumentException ShapeError(string construct, Dbms dbms) =>
        new($"{construct} is not supported on {DisplayName(dbms)}.");

    private static string DisplayName(Dbms dbms) => dbms switch
    {
        Dbms.MySql => "MySQL",
        Dbms.Oracle => "Oracle",
        Dbms.PostgreSql => "PostgreSQL",
        Dbms.Sqlite => "SQLite",
        Dbms.SqlServer => "SQL Server",
        _ => dbms.ToString(),
    };
}
