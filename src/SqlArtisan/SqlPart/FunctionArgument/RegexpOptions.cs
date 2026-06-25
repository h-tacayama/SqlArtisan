namespace SqlArtisan;

/// <summary>
/// Match options for the <c>REGEXP_*</c> functions, emitted as the match-parameter
/// string (e.g. <c>'i'</c>). Combine flags with <c>|</c> — their letters concatenate
/// (<see cref="CaseInsensitive"/> | <see cref="MultipleLines"/> emits <c>'im'</c>).
/// </summary>
[Flags]
public enum RegexpOptions
{
    /// <summary>No options; emits an empty match parameter (<c>''</c>).</summary>
    None = 0,

    /// <summary>Case-sensitive matching (<c>'c'</c>). Mutually exclusive with <see cref="CaseInsensitive"/>.</summary>
    CaseSensitive = 1 << 0,

    /// <summary>Case-insensitive matching (<c>'i'</c>). Mutually exclusive with <see cref="CaseSensitive"/>.</summary>
    CaseInsensitive = 1 << 1,

    /// <summary>Multi-line mode (<c>'m'</c>): <c>^</c> and <c>$</c> match at line breaks within the source.</summary>
    MultipleLines = 1 << 2,

    /// <summary>Newline mode (<c>'n'</c>): the <c>.</c> metacharacter also matches the newline character.</summary>
    NewLine = 1 << 3,

    /// <summary>Extended mode (<c>'x'</c>): whitespace in the pattern is ignored.</summary>
    ExcludingWhiteSpace = 1 << 4,
}
