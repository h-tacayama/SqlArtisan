namespace SqlArtisan.Internal;

/// <summary>
/// The <c>GROUP_CONCAT(expr)</c> string aggregate (MySQL and SQLite). The two
/// dialects diverge on the separator: SQLite takes a positional second argument
/// (<c>GROUP_CONCAT(expr, sep)</c>), while MySQL uses a <c>SEPARATOR</c> keyword
/// supplied via <c>Sql.Separator(...)</c>. MySQL additionally supports
/// <c>DISTINCT</c> and an inline <c>ORDER BY</c> (<see cref="OrderBy(object[])"/>).
/// </summary>
/// <remarks>
/// MySQL silently truncates the result at <c>group_concat_max_len</c> (1024
/// bytes by default); raise that session/global variable for large groups.
/// </remarks>
public sealed class GroupConcatFunction : SqlExpression
{
    private readonly DistinctKeyword? _distinct;
    private readonly SqlExpression _expr;
    private readonly SqlExpression? _positionalSeparator;
    private readonly SeparatorClause? _separatorClause;
    private OrderByClause? _orderBy;

    internal GroupConcatFunction(SqlExpression expr)
    {
        _expr = expr;
    }

    internal GroupConcatFunction(SqlExpression expr, SqlExpression positionalSeparator)
    {
        _expr = expr;
        _positionalSeparator = positionalSeparator;
    }

    internal GroupConcatFunction(SqlExpression expr, SeparatorClause separatorClause)
    {
        _expr = expr;
        _separatorClause = separatorClause;
    }

    internal GroupConcatFunction(DistinctKeyword distinct, SqlExpression expr)
    {
        _distinct = distinct;
        _expr = expr;
    }

    internal GroupConcatFunction(
        DistinctKeyword distinct,
        SqlExpression expr,
        SeparatorClause separatorClause)
    {
        _distinct = distinct;
        _expr = expr;
        _separatorClause = separatorClause;
    }

    /// <summary>
    /// Adds an inline <c>ORDER BY</c> inside the call:
    /// <c>GROUP_CONCAT(expr ORDER BY ... SEPARATOR sep)</c> (MySQL).
    /// </summary>
    public GroupConcatFunction OrderBy(params object[] orderByItems)
    {
        _orderBy = OrderByClause.Parse(orderByItems);
        return this;
    }

    internal override void Format(SqlBuildingBuffer buffer) => buffer
        .Append(Keywords.GroupConcat)
        .OpenParenthesis()
        .AppendSpaceIfNotNull(_distinct)
        .Append(_expr)
        .PrependCommaIfNotNull(_positionalSeparator)
        .PrependSpaceIfNotNull(_orderBy)
        .PrependSpaceIfNotNull(_separatorClause)
        .CloseParenthesis();
}
