using SqlArtisan.Internal;

namespace SqlArtisan;

/// <summary>
/// SQL Server's <c>TOP (n)</c>, from <see cref="Sql.Top(int)"/>; add
/// <see cref="Percent"/> or <see cref="WithTies"/>.
/// </summary>
public sealed class TopClause : SqlPart
{
    private readonly BindValue _count;
    private readonly bool _percent;
    private readonly bool _withTies;

    internal TopClause(int count) : this(new BindValue(count), false, false)
    {
    }

    private TopClause(BindValue count, bool percent, bool withTies)
    {
        _count = count;
        _percent = percent;
        _withTies = withTies;
    }

    internal bool HasWithTies => _withTies;

    /// <summary>
    /// Reads <c>TOP (n) PERCENT</c> — the first n percent of rows rather than a
    /// row count.
    /// </summary>
    /// <returns>A new <see cref="TopClause"/>; this one is unchanged.</returns>
    /// <remarks>SQL Server syntax.</remarks>
    public TopClause Percent() => new(_count, true, _withTies);

    /// <summary>
    /// Reads <c>TOP (n) WITH TIES</c> — also returns rows tied with the last row
    /// under the query's <c>ORDER BY</c> (which the clause then requires).
    /// </summary>
    /// <returns>A new <see cref="TopClause"/>; this one is unchanged.</returns>
    /// <remarks>SQL Server syntax.</remarks>
    public TopClause WithTies() => new(_count, _percent, true);

    internal override void Format(SqlBuildingBuffer buffer) => buffer
        .Append($"{Keywords.Top} ")
        .OpenParenthesis(_count)
        .CloseParenthesis()
        .PrependSpaceIfNotNull(_percent ? Keywords.Percent : null)
        .PrependSpaceIfNotNull(_withTies ? $"{Keywords.With} {Keywords.Ties}" : null);
}
