namespace SqlArtisan.Internal;

/// <summary>
/// The state after <c>DELETE FROM target USING aux</c> (the PostgreSQL form):
/// narrow with <c>WHERE</c>, add <c>RETURNING</c>, or build. Relate the target
/// to the <c>USING</c> tables in the <c>WHERE</c> predicate.
/// </summary>
public interface IDeleteBuilderUsing : ISqlBuilder, IReturning
{
    /// <summary>
    /// Appends <c>WHERE condition</c> to restrict which rows are deleted.
    /// </summary>
    /// <param name="condition">The row filter, typically relating the target to the <c>USING</c> tables; literals it contains are auto-parameterized.</param>
    /// <returns>The builder positioned for <c>RETURNING</c> or build.</returns>
    IDeleteBuilderWhere Where(SqlCondition condition);
}
