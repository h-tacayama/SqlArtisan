using Sqlify;
using Sqlify.Core;
using Sqlify.Core.Expressions;

namespace SqlArtisan.Benchmark.SqlifyTable;

[Table("users")]
public interface IUsers : ITable
{
    [Column("id")]
    Column<int> Id { get; }

    [Column("name")]
    Column<string> Name { get; }

    [Column("created_at")]
    Column<DateTime> CreatedAt { get; }
}
