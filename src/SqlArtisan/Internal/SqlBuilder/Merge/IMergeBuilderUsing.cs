namespace SqlArtisan.Internal;

/// <summary>
/// The state after <c>USING source</c>: supply the match condition with
/// <c>ON (condition)</c>. The condition is always emitted in parentheses.
/// </summary>
public interface IMergeBuilderUsing
{
    IMergeBuilderOn On(SqlCondition condition);
}
