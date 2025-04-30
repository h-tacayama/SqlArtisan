using System.Diagnostics;

namespace SqlArtisan;

public sealed class SequenceObject
{
    internal SequenceObject(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException(
                "Sequence name cannot be null or empty.",
                nameof(name));
        }

        Name = name;
    }

    public string Name { get; }

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public SequenceCurrVal CurrVal => new(this);

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public SequenceNextVal NextVal => new(this);
}
