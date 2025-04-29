using SqExpress;

namespace InlineSqlSharp.Benchmark.SqExpressTable;

public class Books : TableBase
{
    public readonly Int32TableColumn Id;
    public readonly StringTableColumn Name;
    public readonly Int32TableColumn AuthorId;
    public readonly DoubleTableColumn Rating;

    public Books() : this(default) { }

    public Books(Alias alias) : base("dbo", "Books", alias)
    {
        this.Id = this.CreateInt32Column(
            "UserId",
            ColumnMeta.PrimaryKey().Identity());

        this.Name = this.CreateStringColumn(
            "Name",
            size: 255, isUnicode: true);

        this.AuthorId = this.CreateInt32Column("AuthorId");

        this.Rating = this.CreateDoubleColumn("Rating");
    }
}
