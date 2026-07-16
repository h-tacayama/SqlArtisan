namespace SqlArtisan.Internal;

public sealed class TopClause : SqlPart
{
    private readonly BindValue _count;
    private bool _percent;
    private bool _withTies;

    internal TopClause(int count)
    {
        _count = new BindValue(count);
    }

    internal bool HasWithTies => _withTies;

    /// <summary>
    /// Reads <c>TOP (n) PERCENT</c> — the first n percent of rows rather than a
    /// row count.
    /// </summary>
    /// <returns>This <see cref="TopClause"/>, for chaining.</returns>
    public TopClause Percent()
    {
        _percent = true;
        return this;
    }

    /// <summary>
    /// Reads <c>TOP (n) WITH TIES</c> — also returns rows tied with the last row
    /// under the query's <c>ORDER BY</c> (which the clause then requires).
    /// </summary>
    /// <returns>This <see cref="TopClause"/>, for chaining.</returns>
    public TopClause WithTies()
    {
        _withTies = true;
        return this;
    }

    internal override void Format(SqlBuildingBuffer buffer) => buffer
        .Append($"{Keywords.Top} ")
        .OpenParenthesis(_count)
        .CloseParenthesis()
        .PrependSpaceIfNotNull(_percent ? Keywords.Percent : null)
        .PrependSpaceIfNotNull(_withTies ? $"{Keywords.With} {Keywords.Ties}" : null);
}
