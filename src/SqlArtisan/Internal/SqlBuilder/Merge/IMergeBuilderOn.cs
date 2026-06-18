namespace SqlArtisan.Internal;

/// <summary>
/// The step after <c>USING source</c>: supply the match condition with
/// <c>ON (condition)</c>. The condition is always emitted in parentheses.
/// </summary>
public interface IMergeBuilderOn
{
    IMergeBuilderWhen On(SqlCondition condition);
}
