using System.Diagnostics;

namespace InlineSqlSharp;

public static partial class SqlWordbook
{
	public static ISelectBuilderSelect SELECT(params IExprOrAlias[] selectList) =>
		new SelectBuilder(new SelectClause(Hints.None, AllOrDistinct.All, selectList));

	public static ISelectBuilderSelect SELECT(
		AllOrDistinct allOrAistinct,
		params IExprOrAlias[] selectList) =>
		new SelectBuilder(new SelectClause(Hints.None, allOrAistinct, selectList));

	public static ISelectBuilderSelect SELECT(
		Hints hints,
		params IExprOrAlias[] selectList) =>
		new SelectBuilder(new SelectClause(hints, AllOrDistinct.All, selectList));

	public static ISelectBuilderSelect SELECT(
		Hints hints,
		AllOrDistinct allOrAistinct,
		params IExprOrAlias[] selectList) =>
		new SelectBuilder(new SelectClause(hints, allOrAistinct, selectList));

	public static SubstrFunction SUBSTR(
		CharacterExpr source,
		NumericExpr position) => new(source, position);

	public static SubstrFunction SUBSTR(
		CharacterExpr source,
		NumericExpr position,
		NumericExpr length) => new(source, position, length);

	public static SubstrbFunction SUBSTRB(
		CharacterExpr source,
		NumericExpr position) => new(source, position);

	public static SubstrbFunction SUBSTRB(
		CharacterExpr source,
		NumericExpr position,
		NumericExpr length) => new(source, position, length);

	public static SumFunction SUM(IExpr expr) => new(AllOrDistinct.All, expr);

	public static SumFunction SUM(AllOrDistinct allOrDistinct, NumericExpr expr) =>
		new(allOrDistinct, expr);

	[DebuggerBrowsable(DebuggerBrowsableState.Never)]
	public static SysdateFunction SYSDATE => new();
}
