using Sqlify.Core;
using Sqlify.Core.Expressions;

namespace InlineSqlSharp.Benchmark.SqlifyTable;

public interface IBooks : ITable
{
	public Column<int> Id { get; }
	public Column<string> Name { get; }
	public Column<int> AuthorId { get; }
	public Column<double> Rating { get; }
}
