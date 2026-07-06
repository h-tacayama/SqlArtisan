using System.Diagnostics;
using SqlArtisan.Internal;

namespace SqlArtisan;

/// <summary>
/// A handle to a database sequence, obtained from <c>Sequence("name")</c>.
/// Like a table class, define one per schema sequence and reuse it — e.g. a
/// <see langword="static"/> <see langword="readonly"/> field — reading
/// <see cref="Currval"/> / <see cref="Nextval"/> where a value is needed.
/// </summary>
public sealed class DbSequence
{
    internal DbSequence(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException(
                "DbSequence name cannot be null or empty.",
                nameof(name));
        }

        Name = name;
    }

    /// <summary>
    /// Gets the sequence name as it appears in SQL.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the sequence's current value: <c>name.CURRVAL</c> (Oracle syntax).
    /// </summary>
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public DbSequenceCurrval Currval => new(this);

    /// <summary>
    /// Gets the sequence's next value: <c>name.NEXTVAL</c> (Oracle syntax).
    /// </summary>
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public DbSequenceNextval Nextval => new(this);
}
