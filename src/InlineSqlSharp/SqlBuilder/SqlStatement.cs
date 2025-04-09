namespace InlineSqlSharp;

public sealed class SqlStatement(
    string text,
    IReadOnlyList<BindParameter> parameters)
{
    public string Text => text;

    public IReadOnlyList<BindParameter> Parameters => parameters;
}
