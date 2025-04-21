using System.Diagnostics;

namespace InlineSqlSharp;

public sealed class Sequence
{
    internal Sequence(string name)
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
    public SequenceCurrval CURRVAL => new(this);

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public SequenceNextval NEXTVAL => new(this);
}
