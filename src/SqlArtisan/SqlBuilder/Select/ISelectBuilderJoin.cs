namespace SqlArtisan;

public interface ISelectBuilderJoin : ISqlBuilder
{
    // Subsequent SQL is the same as the FROM clause.
    ISelectBuilderFrom On(AbstractCondition condition);
}
