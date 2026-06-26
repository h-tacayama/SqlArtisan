namespace SqlArtisan.Internal;

/// <summary>
/// The state after <c>UPDATE ... SET</c>: narrow with <c>WHERE</c>, add <c>RETURNING</c>, or build.
/// </summary>
public interface IUpdateBuilderSet : ISqlBuilder, IReturning
{
    /// <summary>
    /// Appends <c>WHERE condition</c> to restrict which rows are updated.
    /// </summary>
    /// <param name="condition">The row filter; literals it contains are auto-parameterized.</param>
    /// <returns>The builder positioned for <c>RETURNING</c> or build.</returns>
    IUpdateBuilderWhere Where(SqlCondition condition);
}
