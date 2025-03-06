using System.Diagnostics;

namespace InlineSqlSharp;

public static partial class SqlWordbook
{
	[DebuggerBrowsable(DebuggerBrowsableState.Never)]
	public static AllOrDistinct DISTINCT => AllOrDistinct.Distinct;

	public static CharacterDecodeFunction<CharacterExpr> DECODE(
		CharacterExpr expr,
		(CharacterExpr search, CharacterExpr result)[] searchResultPairs,
		CharacterExpr @default) =>
		new(expr, searchResultPairs, @default);

	public static CharacterDecodeFunction<DateTimeExpr> DECODE(
		DateTimeExpr expr,
		(DateTimeExpr search, CharacterExpr result)[] searchResultPairs,
		CharacterExpr @default) =>
		new(expr, searchResultPairs, @default);

	public static CharacterDecodeFunction<NumericExpr> DECODE(
		NumericExpr expr,
		(NumericExpr search, CharacterExpr result)[] searchResultPairs,
		CharacterExpr @default) =>
		new(expr, searchResultPairs, @default);

	public static DateTimeDecodeFunction<CharacterExpr> DECODE(
		CharacterExpr expr,
		(CharacterExpr search, DateTimeExpr result)[] searchResultPairs,
		DateTimeExpr @default) =>
		new(expr, searchResultPairs, @default);

	public static DateTimeDecodeFunction<DateTimeExpr> DECODE(
		DateTimeExpr expr,
		(DateTimeExpr search, DateTimeExpr result)[] searchResultPairs,
		DateTimeExpr @default) =>
		new(expr, searchResultPairs, @default);

	public static DateTimeDecodeFunction<NumericExpr> DECODE(
		NumericExpr expr,
		(NumericExpr search, DateTimeExpr result)[] searchResultPairs,
		DateTimeExpr @default) =>
		new(expr, searchResultPairs, @default);

	public static NumericDecodeFunction<CharacterExpr> DECODE(
		CharacterExpr expr,
		(CharacterExpr search, NumericExpr result)[] searchResultPairs,
		NumericExpr @default) =>
		new(expr, searchResultPairs, @default);

	public static NumericDecodeFunction<DateTimeExpr> DECODE(
		DateTimeExpr expr,
		(DateTimeExpr search, NumericExpr result)[] searchResultPairs,
		NumericExpr @default) =>
		new(expr, searchResultPairs, @default);

	public static NumericDecodeFunction<NumericExpr> DECODE(
		NumericExpr expr,
		(NumericExpr search, NumericExpr result)[] searchResultPairs,
		NumericExpr @default) =>
		new(expr, searchResultPairs, @default);
}
