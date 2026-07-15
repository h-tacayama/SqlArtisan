namespace SqlArtisan.Internal;

/// <summary>
/// The state after a MySQL multi-table <c>UPDATE ... JOIN ... SET</c>: narrow
/// with <c>WHERE</c>, add <c>RETURNING</c>, or build. A trailing <c>FROM</c>
/// is intentionally unreachable — that mixes the two dialect spellings.
/// </summary>
public interface IUpdateBuilderJoinedSet : ISqlBuilder, IReturning
{
    /// <summary>
    /// Appends <c>WHERE condition</c> to restrict which rows are updated.
    /// </summary>
    /// <param name="condition">The row filter; literals it contains are auto-parameterized.</param>
    /// <returns>The builder positioned for <c>RETURNING</c> or build.</returns>
    IUpdateBuilderWhere Where(SqlCondition condition);
}
