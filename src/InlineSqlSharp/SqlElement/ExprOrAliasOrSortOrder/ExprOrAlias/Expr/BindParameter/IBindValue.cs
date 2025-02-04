using System.Data;

namespace InlineSqlSharp;

public interface IBindValue : IExpr
{
	DbType DbType { get; }

	ParameterDirection Direction { get; }

	object Value { get; }
}
