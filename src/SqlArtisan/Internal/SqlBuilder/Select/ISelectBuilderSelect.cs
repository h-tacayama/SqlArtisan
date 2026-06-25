namespace SqlArtisan.Internal;

/// <summary>The builder state after the select list: name the source tables with <c>FROM</c>.</summary>
public interface ISelectBuilderSelect : ISqlBuilder, ISetOperator, ISubquery
{
    /// <summary>Appends <c>FROM a, b, ...</c>.</summary>
    /// <param name="tables">The table references to read from — base tables, aliased tables, derived tables, or subqueries.</param>
    /// <returns>The builder positioned after <c>FROM</c>, ready for joins, <c>WHERE</c>, grouping, ordering, pagination, or build.</returns>
    ISelectBuilderFrom From(params TableReference[] tables);
}
