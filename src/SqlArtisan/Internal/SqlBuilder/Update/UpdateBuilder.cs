namespace SqlArtisan.Internal;

internal sealed class UpdateBuilder(params SqlPart[] rootParts) :
    SqlBuilderBase(rootParts),
    IUpdateBuilderSet,
    IUpdateBuilderUpdate,
    IUpdateBuilderWhere
{
    public SqlStatement Build() =>
        BuildCore(SqlArtisanConfig.DefaultDbms);

    public SqlStatement Build(Dbms dbms) =>
        BuildCore(dbms);

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
