using System.Data;

namespace InlineSqlSharp;

public interface IBoundValue : IExpr
{
	object Value { get; }

	DbType DbType { get; }
}
