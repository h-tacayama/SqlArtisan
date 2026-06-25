namespace SqlArtisan.Internal;

/// <summary>The state after <c>INSERT INTO table SET</c>-style assignments: add <c>RETURNING</c> or an upsert clause, or build.</summary>
public interface IInsertBuilderSet : ISqlBuilder, IReturning, IUpsert
{
}
