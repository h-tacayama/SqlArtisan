namespace SqlArtisan;

public sealed class NotCondition : SqlCondition
{
    private readonly SqlCondition _condition;

    internal NotCondition(SqlCondition condition)
    {
        _condition = condition;
    }

    internal override void FormatSql(SqlBuildingBuffer buffer) => buffer
        .Append($"{Keywords.Not} ")
        .EncloseInParentheses(_condition);
}
