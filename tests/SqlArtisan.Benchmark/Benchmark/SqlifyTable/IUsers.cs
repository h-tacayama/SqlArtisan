using Sqlify;
using Sqlify.Core;
using Sqlify.Core.Expressions;

namespace SqlArtisan.Benchmark.SqlifyTable;

[Table("users")]
public interface IUsers : ITable
{
    [Column("id")]
    public Column<int> Id { get; }

    [Column("name")]
    public Column<string> Name { get; }

    [Column("created_at")]
    public Column<DateTime> CreatedAt { get; }
}
