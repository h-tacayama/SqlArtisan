namespace SqlArtisan;

public sealed class LastDayFunction : AbstractExpr
{
    private readonly AbstractSqlPart _date;

    internal LastDayFunction(AbstractExpr date)
    {
        _date = date;
    }

    internal override void FormatSql(SqlBuildingBuffer buffer) => buffer
        .Append(Keywords.LastDay)
        .OpenParenthesis()
        .Append(_date)
        .CloseParenthesis();
}
