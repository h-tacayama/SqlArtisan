namespace SqlArtisan.Internal;

/// <summary>
/// The state after <c>MERGE INTO target</c>: name the data source with
/// <c>USING source</c>. The source is any table reference; alias the target and
/// source so the <c>ON</c> condition can disambiguate their columns.
/// </summary>
public interface IMergeBuilderTarget
{
    IMergeBuilderUsing Using(TableReference source);
}
