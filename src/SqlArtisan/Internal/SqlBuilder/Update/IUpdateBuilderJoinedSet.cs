namespace SqlArtisan.Internal;

/// <summary>
/// The state after a MySQL multi-table <c>UPDATE ... JOIN ... SET</c>: narrow
/// with <c>WHERE</c>, or build.
/// </summary>
// No trailing FROM: it would mix the JOIN spelling with the UPDATE ... FROM one.
public interface IUpdateBuilderJoinedSet : ISqlBuilder
{
    /// <summary>
    /// Appends <c>WHERE condition</c> to restrict which rows are updated.
    /// </summary>
    /// <param name="condition">The row filter; literals it contains are auto-parameterized.</param>
    /// <returns>The builder positioned to build.</returns>
    IUpdateBuilderJoinedWhere Where(SqlCondition condition);
}
