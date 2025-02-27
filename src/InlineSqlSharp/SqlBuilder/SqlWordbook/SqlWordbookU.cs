using System.Numerics;

namespace InlineSqlSharp;

public static partial class SqlWordbook
{
	public static UpperFunction UPPER(CharacterExpr source) => new(source);
}
