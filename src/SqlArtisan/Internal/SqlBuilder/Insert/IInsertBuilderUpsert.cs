namespace SqlArtisan.Internal;

/// <summary>
/// A complete UPSERT statement, ready to build or extend with <c>RETURNING</c>.
/// </summary>
public interface IInsertBuilderUpsert : ISqlBuilder, IReturning
{
}
