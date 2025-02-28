namespace InlineSqlSharp;

public static partial class SqlWordbook
{
	public static InstrFunction INSTR(
		CharacterExpr source,
		CharacterExpr substring) => InstrFunction.Of(source, substring);

	public static InstrFunction INSTR(
		CharacterExpr source,
		CharacterExpr substring,
		NumericExpr position) => InstrFunction.Of(source, substring, position);

	public static InstrFunction INSTR(
		CharacterExpr source,
		CharacterExpr substring,
		NumericExpr position,
		NumericExpr occurrence) => InstrFunction.Of(
			source,
			substring,
			position,
			occurrence);
}
