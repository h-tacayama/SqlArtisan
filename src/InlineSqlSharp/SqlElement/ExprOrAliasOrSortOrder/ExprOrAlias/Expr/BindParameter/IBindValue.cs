using System.Data;

namespace InlineSqlSharp;

public interface IBindValue : IExpr
{
    object Value { get; }

    DbType? DbType { get; }

    ParameterDirection? Direction { get; }
}
