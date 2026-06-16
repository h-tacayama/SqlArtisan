namespace SqlArtisan.Internal;

/// <summary>
/// An <c>INSERT</c>-specific terminal that supports both a <c>RETURNING</c>
/// clause and collection-driven batching. It is distinct from the shared
/// <see cref="IReturning"/> so that <see cref="IBulkInsert.BuildBatches()"/>
/// stays off the SELECT/UPDATE/DELETE surface while remaining reachable after a
/// multi-row UPSERT (<c>ON CONFLICT ...</c> / <c>ON DUPLICATE KEY UPDATE</c>).
/// </summary>
public interface IInsertReturning : IReturning, IBulkInsert
{
}
