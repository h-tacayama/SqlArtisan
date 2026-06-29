namespace SqlArtisan.Internal;

/// <summary>
/// Marks a "pending" builder type that is deliberately not a
/// <see cref="SqlExpression"/> because a mandatory trailing clause is still
/// missing — a window function before <c>.Over(...)</c>, or an ordered-set
/// aggregate before <c>.WithinGroup(...)</c>. When such a value reaches a value
/// position, the resolvers surface <see cref="CompletionHint"/> so the caller is
/// told how to complete it instead of getting a generic "invalid type" message.
/// </summary>
internal interface IIncompleteExpression
{
    /// <summary>
    /// A short, actionable sentence naming the call that completes this
    /// expression — e.g. <c>"Complete it with .Over(...)."</c>.
    /// </summary>
    string CompletionHint { get; }
}
