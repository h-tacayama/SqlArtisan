namespace SqlArtisan.Internal;

/// <summary>
/// The <c>GROUP_CONCAT(expr)</c> string aggregate (MySQL and SQLite). The two
/// dialects diverge on the separator: SQLite takes a positional second argument
/// (<c>GROUP_CONCAT(expr, sep)</c>), while MySQL uses a <c>SEPARATOR</c> keyword
/// supplied via <c>Sql.Separator(...)</c>. <c>DISTINCT</c> is supported by both
/// (SQLite only in the single-argument form, without a separator). MySQL also
/// accepts an inline <c>ORDER BY</c>, passed as an <c>Sql.OrderBy(...)</c>
/// argument because it sits inside the call.
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
    private readonly OrderByClause? _orderByClause;
    private readonly SeparatorClause? _separatorClause;

    internal GroupConcatFunction(
        SqlExpression expr,
        DistinctKeyword? distinct = null,
        SqlExpression? positionalSeparator = null,
        OrderByClause? orderByClause = null,
        SeparatorClause? separatorClause = null)
    {
        _expr = expr;
        _distinct = distinct;
        _positionalSeparator = positionalSeparator;
        _orderByClause = orderByClause;
        _separatorClause = separatorClause;
    }

    internal override void Format(SqlBuildingBuffer buffer) => buffer
        .Append(Keywords.GroupConcat)
        .OpenParenthesis()
        .AppendSpaceIfNotNull(_distinct)
        .Append(_expr)
        .PrependCommaIfNotNull(_positionalSeparator)
        .PrependSpaceIfNotNull(_orderByClause)
        .PrependSpaceIfNotNull(_separatorClause)
        .CloseParenthesis();
}
