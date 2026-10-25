using System.Data;
using SqlArtisan.Internal;

namespace SqlArtisan;

/// <summary>
/// A typed output parameter for an Oracle <c>RETURNING … INTO</c> clause: the
/// variable name paired with the <see cref="System.Data.DbType"/> the driver
/// binds it as, plus an optional <see cref="Size"/> for variable-length types.
/// </summary>
public readonly struct OutputParameter
{
    // The caller supplies the type: a column reference carries a name, not a CLR type.

    /// <summary>
    /// Creates an output parameter binding.
    /// </summary>
    /// <param name="variable">The output variable name, emitted as a bind marker (<c>:name</c>).</param>
    /// <param name="dbType">The data type the output parameter is bound as.</param>
    /// <param name="size">The buffer size for variable-length types (for example strings); omit for fixed-width types.</param>
    /// <exception cref="ArgumentException"><paramref name="variable"/> is <see langword="null"/>, empty or white space, is digits only, or does not start with a letter and hold only letters, digits, <c>_</c>, <c>$</c> and <c>#</c>.</exception>
    public OutputParameter(string variable, DbType dbType, int? size = null)
    {
        StringGuard.ThrowIfNullOrWhiteSpace(variable, "An output variable name is required.");

        // Positional binds render as :0, :1, ...; a digit-only name would
        // collide with them and be reported as a duplicate of the wrong thing.
        if (IsDigitsOnly(variable))
        {
            throw new ArgumentException(
                "An output variable name must not be digits only; "
                + "that namespace belongs to the positional bind markers.");
        }

        // The shape of an Oracle nonquoted identifier: the marker ends at any other character,
        // so "0 " rendered INTO :0 beside a positional :0 (#679).
        if (!IsIdentifierShaped(variable))
        {
            throw new ArgumentException(
                "An output variable name must start with a letter and hold only letters, "
                + "digits, _, $ and #; its marker ends at any other character.");
        }

        Variable = variable;
        DbType = dbType;
        Size = size;
    }

    /// <summary>
    /// Gets the output variable name, emitted as a bind marker (<c>:name</c>).
    /// </summary>
    public string Variable { get; }

    /// <summary>
    /// Gets the data type the output parameter is bound as.
    /// </summary>
    public DbType DbType { get; }

    /// <summary>
    /// Gets the buffer size for variable-length types, or <see langword="null"/> when unset.
    /// </summary>
    public int? Size { get; }

    private static bool IsIdentifierShaped(string value)
    {
        if (!char.IsLetter(value[0]))
        {
            return false;
        }

        foreach (char c in value)
        {
            if (!char.IsLetterOrDigit(c) && c != '_' && c != '$' && c != '#')
            {
                return false;
            }
        }

        return true;
    }

    private static bool IsDigitsOnly(string value)
    {
        foreach (char c in value)
        {
            if (!char.IsAsciiDigit(c))
            {
                return false;
            }
        }

        return true;
    }
}
