using LinqToDB.Mapping;

namespace SqlArtisan.Benchmark.Linq2dbTable;

[Table("users")]
internal sealed class User
{
    [Column("id")]
    public int Id { get; set; }

    [Column("name")]
    public string Name { get; set; } = "";

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }
}
