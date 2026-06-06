namespace SqlArtisan.Internal;

public interface IReturningBuilder : ISqlBuilder
{
    ISqlBuilder Into(params string[] variables);
}
