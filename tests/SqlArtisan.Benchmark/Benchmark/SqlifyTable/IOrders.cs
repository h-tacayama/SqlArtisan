using Sqlify;
using Sqlify.Core;
using Sqlify.Core.Expressions;

namespace SqlArtisan.Benchmark.SqlifyTable;

[Table("orders")]
public interface IOrders : ITable
{
    [Column("id")]
    Column<int> Id { get; }

    [Column("user_id")]
    Column<int> UserId { get; }

    [Column("order_date")]
    Column<DateTime> OrderDate { get; }
}
