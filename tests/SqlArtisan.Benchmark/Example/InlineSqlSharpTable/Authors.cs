namespace InlineSqlSharp.Benchmark.InlineSqlSharpTable;

internal sealed class Authors : AbstractTable
{
    public Authors(string alias) : base(alias)
    {
        Id = new Column(alias, "Id");
        Name = new Column(alias, "Name");
    }

    public Column Id { get; }

    public Column Name { get; }
}
