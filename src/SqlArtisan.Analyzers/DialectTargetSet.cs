using System.Collections.Generic;

namespace SqlArtisan.Analyzers;

/// <summary>
/// The resolved <c>sqlartisan_syntax_*</c> family (or its legacy-pair
/// desugaring, #432): which DBMS a file's rules check, and the declared
/// version bound for each — <see langword="null"/> means <c>any</c>, checked
/// with no version floor.
/// </summary>
internal sealed class DialectTargetSet
{
    public static readonly DialectTargetSet Empty = new();

    private readonly bool[] _present = new bool[5];
    private readonly EngineVersion?[] _versions = new EngineVersion?[5];

    internal void Add(TargetDbms dbms, EngineVersion? version)
    {
        _present[(int)dbms] = true;
        _versions[(int)dbms] = version;
    }

    public bool IsEmpty
    {
        get
        {
            foreach (bool present in _present)
            {
                if (present)
                {
                    return false;
                }
            }

            return true;
        }
    }

    public bool Contains(TargetDbms dbms) => _present[(int)dbms];

    public EngineVersion? VersionFor(TargetDbms dbms) => _versions[(int)dbms];

    // Enum declaration order — the DBMS listing order docs-style.md fixes — so
    // every set-valued diagnostic (SQLA0100's join, SQLA0101/0103's per-DBMS
    // reports) is deterministic without a separate sort step.
    public IEnumerable<TargetDbms> Members
    {
        get
        {
            for (int i = 0; i < _present.Length; i++)
            {
                if (_present[i])
                {
                    yield return (TargetDbms)i;
                }
            }
        }
    }
}
