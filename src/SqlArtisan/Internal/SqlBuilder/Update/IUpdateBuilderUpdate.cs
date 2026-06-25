namespace SqlArtisan.Internal;

/// <summary>The state after <c>UPDATE table</c>: supply the column assignments with <c>SET</c>.</summary>
public interface IUpdateBuilderUpdate : ISqlBuilder
{
    /// <summary>Appends <c>SET col = value, ...</c> from <c>column == value</c> assignments.</summary>
    /// <param name="assignments">The per-column updates; each left side names a column and each right side its new value (literals are auto-parameterized).</param>
    /// <returns>The builder positioned for <c>WHERE</c>, <c>RETURNING</c>, or build.</returns>
    IUpdateBuilderSet Set(params EqualityBasedCondition[] assignments);
}
