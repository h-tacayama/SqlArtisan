namespace SqlArtisan.Internal;

/// <summary>
/// The <c>GROUP_CONCAT(expr)</c> string aggregate (MySQL and SQLite). SQLite takes the
/// separator positionally, MySQL via <c>Sql.Separator(...)</c>; an inline <c>ORDER BY</c>
/// (MySQL and SQLite) is passed as an <c>Sql.OrderBy(...)</c> argument.
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
