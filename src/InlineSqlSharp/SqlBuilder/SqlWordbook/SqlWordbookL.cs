using System.Numerics;

namespace InlineSqlSharp;

public static partial class SqlWordbook
{
	public static CharacterLiteral L(string value, bool isEscaped = false) =>
		new(value, isEscaped);

	public static NumericLiteral<TValue> L<TValue>(TValue value)
		where TValue : INumber<TValue> => new(value);

	public static LengthFunction LENGTH(CharacterExpr source) => new(source);
}
