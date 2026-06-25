using SqlArtisan.Internal;

namespace SqlArtisan;

/// <summary>A built SQL statement: the rendered text and the parameters it binds.</summary>
public sealed class SqlStatement
{
    internal SqlStatement(string text, List<KeyValuePair<string, BindValue>> parameters)
    {
        Text = text;
        Parameters = new(parameters);
    }

    /// <summary>Gets the rendered SQL text, with parameter markers standing in for bound literals.</summary>
    public string Text { get; }

    /// <summary>Gets the parameters bound by <see cref="Text"/>.</summary>
    public SqlParameters Parameters { get; }
}
