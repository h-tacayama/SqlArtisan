using System.ComponentModel.DataAnnotations.Schema;

namespace SqlArtisan.Benchmark.EfCoreModel;

[Table("users")]
public sealed class User
{
    [Column("id")]
    public int Id { get; set; }

    [Column("name")]
    public string Name { get; set; } = "";

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    public List<Order> Orders { get; set; } = [];
}
