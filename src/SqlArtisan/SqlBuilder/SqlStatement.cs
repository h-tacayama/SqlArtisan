namespace SqlArtisan;

/// <summary>
/// A built SQL statement: the rendered text and the parameters it binds.
/// </summary>
public sealed class SqlStatement
{
    internal SqlStatement(string text, List<KeyValuePair<string, BindValue>> parameters)
    {
        Text = text;
        Parameters = new(parameters);
    }

    /// <summary>
    /// Gets the rendered SQL text, with parameter markers standing in for bound literals.
    /// </summary>
    public string Text { get; }

    /// <summary>
    /// Gets the parameters bound by <see cref="Text"/>.
    /// </summary>
    public SqlParameters Parameters { get; }

    /// <summary>
    /// Returns <see cref="Text"/> — the SQL with parameter markers. Parameter
    /// <em>values</em> are not included (they may hold sensitive data); read
    /// <see cref="Parameters"/> explicitly when you need them.
    /// </summary>
    /// <returns>The rendered SQL text.</returns>
    public override string ToString() => Text;
}
