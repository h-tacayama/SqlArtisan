using LinqToDB.Mapping;

namespace SqlArtisan.Benchmark.Linq2dbTable;

[Table("orders")]
internal sealed class Order
{
    [Column("id")]
    public int Id { get; set; }

    [Column("user_id")]
    public int UserId { get; set; }

    [Column("order_date")]
    public DateTime OrderDate { get; set; }
}
