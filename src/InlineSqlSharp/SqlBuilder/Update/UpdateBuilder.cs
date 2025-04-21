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
        AddElement(UpdateSetClause.Parse(assignments));
        return this;
    }

    public IUpdateBuilderWhere WHERE(AbstractCondition condition)
    {
        AddElement(new WhereClause(condition));
        return this;
    }
}
