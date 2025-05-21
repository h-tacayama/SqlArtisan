namespace SqlArtisan.Internal;

internal sealed class UpdateBuilder(UpdateClause updateClause) :
    SqlBuilderBase(updateClause),
    IUpdateBuilderSet,
    IUpdateBuilderUpdate,
    IUpdateBuilderWhere
{
    public SqlStatement Build() => BuildCore();

    public IUpdateBuilderSet Set(params EqualityBasedCondition[] assignments)
    {
        AddPart(UpdateSetClause.Parse(assignments));
        return this;
    }

    public IUpdateBuilderWhere Where(SqlCondition condition)
    {
        AddPart(new WhereClause(condition));
        return this;
    }
}
