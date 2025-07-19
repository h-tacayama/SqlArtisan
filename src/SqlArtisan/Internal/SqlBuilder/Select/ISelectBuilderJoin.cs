namespace SqlArtisan.Internal;

public interface ISelectBuilderJoin : ISqlBuilder, IForUpdate
{
    // Subsequent SQL is the same as the FROM clause.
    ISelectBuilderFrom On(SqlCondition condition);
}
