namespace SqlArtisan.Internal;

// Implemented by the ad-hoc relation handles (DerivedTable, Cte) to force them to
// expose the same Column(...) surface: adding an overload here breaks compilation
// of every implementer until it is added. The bodies delegate to DerivedColumn so
// the logic itself lives in one place.
internal interface IColumnAccessor
{
    DbColumn Column(string columnName);

    DbColumn Column(DbColumn sourceColumn);

    DbColumn Column(ExpressionAlias alias);
}
