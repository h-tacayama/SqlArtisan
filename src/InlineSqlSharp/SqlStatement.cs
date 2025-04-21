using Dapper;

namespace InlineSqlSharp;

public sealed class SqlStatement
{
    internal SqlStatement(string text, DynamicParameters parameters)
    {
        Text = text;
        Parameters = parameters;
    }

    public string Text { get; }

    public DynamicParameters Parameters { get; }

    public int ParameterCount => Parameters.ParameterNames.Count();
}
