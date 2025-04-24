namespace InlineSqlSharp;

internal sealed class UpdateBuilder(UpdateClause updateClause) :
    AbstractSqlBuilder(updateClause),
    IUpdateBuilderSet,
    IUpdateBuilderUpdate,
    IUpdateBuilderWhere
{
    public SqlStatement Build() => BuildCore();

    public IUpdateBuilderSet SET(params AbstractEqualityCondition[] assignments)
    {
        AddPart(UpdateSetClause.Parse(assignments));
        return this;
    }

    public IUpdateBuilderWhere WHERE(AbstractCondition condition)
    {
        AddPart(new WhereClause(condition));
        return this;
    }
}
