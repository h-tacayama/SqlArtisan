namespace InlineSqlSharp.Tests;

internal sealed class test_table : AbstractTable
{
    public test_table(string alias) : base(alias)
    {
        code = new Column(alias, "code");
        name = new Column(alias, "name");
        created_at = new Column(alias, "created_at");
    }

    public Column code { get; }

    public Column name { get; }

    public Column created_at { get; }
}
