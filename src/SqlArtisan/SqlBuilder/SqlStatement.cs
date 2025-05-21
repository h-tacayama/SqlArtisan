using SqlArtisan.Internal;

namespace SqlArtisan;

public sealed class SqlStatement
{
    internal SqlStatement(string text, Dictionary<string, BindValue> parameters)
    {
        Text = text;
        Parameters = new(parameters);
    }

    public string Text { get; }

    public SqlParameters Parameters { get; }
}
