namespace InlineSqlSharp;

public sealed class AddMonthsFunction : AbstractExpr
{
    private readonly AbstractSqlPart _dateTime;
    private readonly AbstractSqlPart _months;

    internal AddMonthsFunction(AbstractExpr dateTime, AbstractExpr months)
    {
        _dateTime = dateTime;
        _months = months;
    }

    internal override void FormatSql(SqlBuildingBuffer buffer) => buffer
        .Append(Keywords.ADD_MONTHS)
        .OpenParenthesis()
        .Append(_dateTime)
        .PrependComma(_months)
        .CloseParenthesis();
}
