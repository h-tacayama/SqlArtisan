using System.Numerics;

namespace InlineSqlSharp.Oracle;

public static partial class SqlWordbook
{
	public static NumericLiteral<TValue> L<TValue>(TValue value)
		where TValue : INumber<TValue> => new(value);
}
