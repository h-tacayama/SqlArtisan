namespace SqlArtisan.Internal;

/// <summary>The state after <c>UPDATE ... WHERE</c>: add <c>RETURNING</c> or build.</summary>
public interface IUpdateBuilderWhere : ISqlBuilder, IReturning
{
}
