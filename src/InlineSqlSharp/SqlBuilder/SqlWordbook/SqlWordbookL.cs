using System.Numerics;

namespace InlineSqlSharp;

public static partial class SqlWordbook
{
	public static CharacterLiteral L(string value, bool isEscaped = false) =>
		new(value, isEscaped);

	public static NumericLiteral<TValue> L<TValue>(TValue value)
		where TValue : INumber<TValue> => new(value);

	public static LengthFunction LENGTH(CharacterExpr source) => new(source);

	public static LowerFunction LOWER(CharacterExpr source) => new(source);

	public static LpadFunction LPAD(
		CharacterExpr source,
		NumericExpr length) => new(source, length);

	public static LpadFunction LPAD(
		CharacterExpr source,
		NumericExpr length,
		CharacterExpr padding) => new(source, length, padding);

	public static LtrimFunction LTRIM(CharacterExpr source) => new(source);

	public static LtrimFunction LTRIM(
		CharacterExpr source,
		CharacterExpr trimChars) => new(source, trimChars);
}
