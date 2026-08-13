namespace SqlArtisan.Internal;

public sealed class IfFunction : SqlExpression
{
    private readonly SqlCondition _condition;
    private readonly SqlExpression _then;
    private readonly SqlExpression _else;

    internal IfFunction(SqlCondition condition, SqlExpression then, SqlExpression @else)
    {
        _condition = condition;
        _then = then;
        _else = @else;
    }

    internal override void Format(SqlBuildingBuffer buffer)
    {
        ConditionGuard.ThrowIfEmpty(_condition, "IF(...) requires a condition.");

        buffer
            .Append(Keywords.If)
            .OpenParenthesis()
            .Append(_condition)
            .PrependComma(_then)
            .PrependComma(_else)
            .CloseParenthesis();
    }
}
