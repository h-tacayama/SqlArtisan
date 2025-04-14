using Dapper;

namespace InlineSqlSharp;

public sealed class SqlStatement(string text, DynamicParameters parameters)
{
    public string Text => text;

    public DynamicParameters Parameters => parameters;

    public int ParameterCount => parameters.ParameterNames.Count();
}
