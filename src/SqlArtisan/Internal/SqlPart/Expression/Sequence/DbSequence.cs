using System.Diagnostics;

namespace SqlArtisan.Internal;

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

    public string Name { get; }

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public DbSequenceCurrval Currval => new(this);

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public DbSequenceNextval Nextval => new(this);
}
