using Sqlify.Core;
using Sqlify.Core.Expressions;

namespace SqlArtisan.Benchmark.SqlifyTable;

public interface IAuthors : ITable
{
    public Column<int> Id { get; }
    public Column<string> Name { get; }
}
