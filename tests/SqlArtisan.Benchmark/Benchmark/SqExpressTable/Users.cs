using SqExpress;

namespace SqlArtisan.Benchmark.SqExpressTable;

public class Users : TableBase
{
    public readonly Int32TableColumn Id;
    public readonly StringTableColumn Name;
    public readonly DateTimeTableColumn CreatedAt;

    public Users() : this(default) { }

    public Users(Alias alias) : base(null, "users", alias)
    {
        this.Id = this.CreateInt32Column(
            "id",
            ColumnMeta.PrimaryKey().Identity());

        this.Name = this.CreateStringColumn(
            "name",
            size: 255, isUnicode: true);

        this.CreatedAt = this.CreateDateTimeColumn(
            "created_at");
    }
}
