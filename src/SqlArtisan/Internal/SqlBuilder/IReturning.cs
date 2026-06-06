namespace SqlArtisan.Internal;

public interface IReturning : ISqlBuilder
{
    IReturningBuilder Returning(params object[] expressions);
}
