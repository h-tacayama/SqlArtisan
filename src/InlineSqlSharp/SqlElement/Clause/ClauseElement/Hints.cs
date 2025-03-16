namespace InlineSqlSharp;

public sealed class Hints(string hints) : ISqlElement
{
	private readonly string _hints = hints;

	public static Hints None => new(string.Empty);

	public bool IsSome => !IsNone;

	public bool IsNone => string.IsNullOrEmpty(_hints);

	public void FormatSql(SqlBuildingBuffer buffer) => buffer.Append(_hints);
}
