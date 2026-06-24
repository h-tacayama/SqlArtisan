namespace SqlArtisan.Internal;

// The single implementation of an ad-hoc relation's Column(...) accessors, shared
// by every IColumnAccessor implementer so their behaviour cannot diverge. Each
// overload qualifies a column with the relation's alias, taking the output name
// from a raw string, a source column, or a SELECT-list alias.
internal static class DerivedColumn
{
    internal static DbColumn Qualify(string schemaName, string columnName) =>
        new(schemaName, columnName);

    internal static DbColumn Qualify(string schemaName, DbColumn sourceColumn) =>
        new(schemaName, sourceColumn.Name);

    internal static DbColumn Qualify(string schemaName, ExpressionAlias alias) =>
        new(schemaName, alias.Alias);
}
