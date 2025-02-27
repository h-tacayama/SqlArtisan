namespace InlineSqlSharp;

public static partial class SqlWordbook
{
	public static InstrFunction INSTR(
		CharacterExpr source,
		CharacterExpr substring,
		int position = 1,
		int occurrence = 1) => new(source, substring, position, occurrence);
}
