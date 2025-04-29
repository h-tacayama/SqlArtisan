namespace SqlArtisan;

internal sealed class UpdateBuilder(UpdateClause updateClause) :
    AbstractSqlBuilder(updateClause),
    IUpdateBuilderSet,
    IUpdateBuilderUpdate,
    IUpdateBuilderWhere
{
    public SqlStatement Build() => BuildCore();

    public IUpdateBuilderSet Set(params AbstractEqualityCondition[] assignments)
    {
        AddPart(UpdateSetClause.Parse(assignments));
        return this;
    }

    public IUpdateBuilderWhere Where(AbstractCondition condition)
    {
        AddPart(new WhereClause(condition));
        return this;
    }
}
