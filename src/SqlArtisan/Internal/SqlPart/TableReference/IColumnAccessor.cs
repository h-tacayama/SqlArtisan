namespace SqlArtisan.Internal;

// Implemented by the ad-hoc relation handles (DerivedTable, Cte) to force them to
// expose the same Column(...) surface: adding an overload here breaks compilation
// of every implementer until it is added.
internal interface IColumnAccessor
{
    DbColumn Column(string columnName);

    DbColumn Column(DbColumn sourceColumn);

    DbColumn Column(ExpressionAlias expressionAlias);
}
