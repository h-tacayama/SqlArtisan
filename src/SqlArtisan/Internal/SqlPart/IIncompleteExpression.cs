namespace SqlArtisan.Internal;

/// <summary>
/// Marks a "pending" builder type that is deliberately not a
/// <see cref="SqlExpression"/> because a mandatory trailing clause is still
/// missing — a window function before <c>.Over(...)</c>, or an ordered-set
/// aggregate before <c>.WithinGroup(...)</c>. When such a value reaches a value
/// position, the resolvers throw <see cref="IncompleteMessage"/> so the caller is
/// told how to complete it instead of getting a generic "invalid type" message.
/// </summary>
internal interface IIncompleteExpression
{
    /// <summary>
    /// The one-sentence guard message: the construct by its SQL spelling, the clause it
    /// requires, and the call that completes it.
    /// </summary>
    string IncompleteMessage { get; }
}
