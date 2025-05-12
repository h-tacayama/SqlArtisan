using SqExpress;

namespace SqlArtisan.Benchmark.SqExpressTable;

public class Orders : TableBase
{
    public readonly Int32TableColumn Id;
    public readonly Int32TableColumn UserId;
    public readonly DateTimeTableColumn OrderDate;

    public Orders() : this(default) { }

    public Orders(Alias alias) : base(null, "orders", alias)
    {
        this.Id = this.CreateInt32Column(
            "id",
            ColumnMeta.PrimaryKey().Identity());

        this.UserId = this.CreateInt32Column(
            "user_id",
            ColumnMeta.PrimaryKey().Identity());

        this.OrderDate = this.CreateDateTimeColumn(
            "order_date");
    }
}
