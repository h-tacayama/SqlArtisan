using System.Data;

namespace SqlArtisan;

/// <summary>
/// A typed output parameter for an Oracle <c>RETURNING … INTO</c> clause: the
/// variable name paired with the <see cref="System.Data.DbType"/> the driver
/// binds it as, plus an optional <see cref="Size"/> for variable-length types.
/// </summary>
/// <remarks>
/// The type cannot be inferred from the returned column (a column reference
/// carries a name, not a CLR type), so it is supplied here. Variable-length
/// types (for example strings) require a <see cref="Size"/>; fixed-width types
/// (numbers, dates) do not.
/// </remarks>
public readonly struct OutputParameter
{
    /// <summary>Creates an output parameter binding.</summary>
    /// <param name="variable">The output variable name, emitted as a bind marker (<c>:name</c>).</param>
    /// <param name="dbType">The data type the output parameter is bound as.</param>
    /// <param name="size">The buffer size for variable-length types (for example strings); omit for fixed-width types.</param>
    /// <exception cref="ArgumentException"><paramref name="variable"/> is <see langword="null"/> or empty.</exception>
    public OutputParameter(string variable, DbType dbType, int? size = null)
    {
        if (string.IsNullOrEmpty(variable))
        {
            throw new ArgumentException("An output variable name is required.", nameof(variable));
        }

        Variable = variable;
        DbType = dbType;
        Size = size;
    }

    /// <summary>Gets the output variable name, emitted as a bind marker (<c>:name</c>).</summary>
    public string Variable { get; }

    /// <summary>Gets the data type the output parameter is bound as.</summary>
    public DbType DbType { get; }

    /// <summary>Gets the buffer size for variable-length types, or <see langword="null"/> when unset.</summary>
    public int? Size { get; }
}
