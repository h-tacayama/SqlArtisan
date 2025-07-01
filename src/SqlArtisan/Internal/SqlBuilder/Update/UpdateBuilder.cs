namespace SqlArtisan.Internal;

internal sealed class UpdateBuilder(params SqlPart[] rootParts) :
    SqlBuilderBase(rootParts),
    IUpdateBuilderSet,
    IUpdateBuilderUpdate,
    IUpdateBuilderWhere
{
    public SqlStatement Build(Dbms dbmsType) =>
        BuildCore(dbmsType);

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
