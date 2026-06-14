namespace SqlArtisan.Internal;

internal interface IDbmsDialect
{
    Dbms Dbms { get; }

    char AliasQuote { get; }

    char ParameterMarker { get; }

    // The pseudo-table exposing the would-be-inserted row inside
    // INSERT ... ON CONFLICT DO UPDATE. PostgreSQL spells it EXCLUDED, SQLite
    // lowercase excluded. Only those two dialects support ON CONFLICT; the other
    // three return the conventional spelling but never emit it (their UPSERT is
    // ON DUPLICATE KEY UPDATE or MERGE). That three-of-five dialects carry an
    // unused member is itself a data point for the namespace-cost write-up.
    string OnConflictExcludedAlias { get; }

    // The statement terminator. SQL Server's MERGE *requires* a trailing
    // semicolon; the other dialects need none. Like OnConflictExcludedAlias,
    // this is a partial-coverage member (meaningful for one dialect, empty for
    // four) — another accreting cost as clause features grow.
    string StatementTerminator { get; }
}
