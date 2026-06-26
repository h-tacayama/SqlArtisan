namespace SqlArtisan.Internal;

/// <summary>
/// The state after <c>DELETE ... WHERE</c>: add <c>RETURNING</c> or build.
/// </summary>
public interface IDeleteBuilderWhere : ISqlBuilder, IReturning
{
}
