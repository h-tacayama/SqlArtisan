namespace SqlArtisan.Internal;

/// <summary>
/// The state after <c>MERGE INTO target</c>: name the data source with
/// <c>USING source</c>. The source is any table reference; alias the target and
/// source so the <c>ON</c> condition can disambiguate their columns.
/// </summary>
public interface IMergeBuilderTarget
{
    /// <summary>
    /// Appends <c>USING source</c>, naming the data source merged against the target.
    /// </summary>
    /// <param name="source">The source table reference (a table, aliased table, derived table, or subquery).</param>
    /// <returns>The builder positioned to supply the match condition with <c>On(...)</c>.</returns>
    IMergeBuilderUsing Using(TableReference source);
}
