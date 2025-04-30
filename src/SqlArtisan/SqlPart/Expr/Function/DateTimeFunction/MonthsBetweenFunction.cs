namespace SqlArtisan;

public sealed class MonthsBetweenFunction : AbstractExpr
{
    private readonly AbstractSqlPart _date1;
    private readonly AbstractSqlPart _date2;

    internal MonthsBetweenFunction(AbstractExpr date1, AbstractExpr date2)
    {
        _date1 = date1;
        _date2 = date2;
    }

    internal override void FormatSql(SqlBuildingBuffer buffer) => buffer
        .Append(Keywords.MonthsBetween)
        .OpenParenthesis()
        .Append(_date1)
        .PrependComma(_date2)
        .CloseParenthesis();
}
