namespace SqlArtisan.Internal;

/// <summary>
/// Marks an unfinished construct kept off <see cref="SqlExpression"/>: one awaiting a
/// clause (<c>.Over(...)</c>), or an operand only one slot completes (<c>ANY</c>). A
/// value position throws <see cref="IncompleteMessage"/> instead of "invalid type".
/// </summary>
internal interface IIncompleteExpression
{
    /// <summary>
    /// The one-sentence guard message: the construct by its SQL spelling, what it
    /// requires, and the call or position that completes it.
    /// </summary>
    string IncompleteMessage { get; }
}
