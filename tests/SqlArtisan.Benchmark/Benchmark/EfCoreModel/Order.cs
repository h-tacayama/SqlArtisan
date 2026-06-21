using System.ComponentModel.DataAnnotations.Schema;

namespace SqlArtisan.Benchmark.EfCoreModel;

[Table("orders")]
public sealed class Order
{
    [Column("id")]
    public int Id { get; set; }

    [Column("user_id")]
    public int UserId { get; set; }

    [Column("order_date")]
    public DateTime OrderDate { get; set; }

    public User User { get; set; } = null!;
}
