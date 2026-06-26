namespace SqlArtisan.Internal;

/// <summary>
/// The state after <c>USING source</c>: supply the match condition with
/// <c>ON (condition)</c>. The condition is always emitted in parentheses.
/// </summary>
public interface IMergeBuilderUsing
{
    /// <summary>
    /// Appends <c>ON (condition)</c>, the predicate that matches target rows to source rows. Always emitted in parentheses.
    /// </summary>
    /// <param name="condition">The match condition relating target and source columns; literals it contains are auto-parameterized.</param>
    /// <returns>The builder positioned to add <c>WHEN</c> branches.</returns>
    IMergeBuilderOn On(SqlCondition condition);
}
