using SqExpress;

namespace SqlArtisan.Benchmark.SqExpressTable;

public class Authors : TableBase
{
    public readonly Int32TableColumn Id;
    public readonly StringTableColumn Name;

    public Authors() : this(default) { }

    public Authors(Alias alias) : base("dbo", "Authors", alias)
    {
        this.Id = this.CreateInt32Column(
            "Id",
            ColumnMeta.PrimaryKey().Identity());

        this.Name = this.CreateStringColumn(
            "Name",
            size: 255, isUnicode: true);
    }
}
