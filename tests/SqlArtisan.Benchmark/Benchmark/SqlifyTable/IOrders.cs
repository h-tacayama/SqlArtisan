using Sqlify;
using Sqlify.Core;
using Sqlify.Core.Expressions;

namespace SqlArtisan.Benchmark.SqlifyTable;

[Table("orders")]
public interface IOrders : ITable
{
    [Column("id")]
    public Column<int> Id { get; }

    [Column("user_id")]
    public Column<int> UserId { get; }

    [Column("order_date")]
    public Column<DateTime> OrderDate { get; }
}
