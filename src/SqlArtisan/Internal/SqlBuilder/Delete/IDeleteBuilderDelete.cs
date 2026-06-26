namespace SqlArtisan.Internal;

/// <summary>
/// The state after <c>DELETE FROM table</c>: narrow with <c>WHERE</c>, add <c>RETURNING</c>, or build.
/// </summary>
public interface IDeleteBuilderDelete : ISqlBuilder, IReturning
{
    /// <summary>
    /// Appends <c>WHERE condition</c> to restrict which rows are deleted.
    /// </summary>
    /// <param name="condition">The row filter; literals it contains are auto-parameterized.</param>
    /// <returns>The builder positioned for <c>RETURNING</c> or build.</returns>
    IDeleteBuilderWhere Where(SqlCondition condition);
}
