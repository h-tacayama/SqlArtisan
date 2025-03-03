namespace InlineSqlSharp;

public static partial class SqlWordbook
{
	public static InstrFunction INSTR(
		CharacterExpr source,
		CharacterExpr substring) => new(source, substring);

	public static InstrFunction INSTR(
		CharacterExpr source,
		CharacterExpr substring,
		NumericExpr position) => new(source, substring, position);

	public static InstrFunction INSTR(
		CharacterExpr source,
		CharacterExpr substring,
		NumericExpr position,
		NumericExpr occurrence) => new(source, substring, position, occurrence);
}
