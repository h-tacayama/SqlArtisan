using System.Text;

namespace InlineSqlSharp;

[Flags]
public enum RegexpOptions
{
	None = 0,
	CaseSensitive = 1 << 0,
	CaseInsensitive = 1 << 1,
	MultipleLines = 1 << 2,
	NewLine = 1 << 3,
	ExcludingWhiteSpace = 1 << 4,
}

public static class RegexpOptionsExtensions
{
	public static bool IsNone(this RegexpOptions options) =>
		options == RegexpOptions.None;

	public static string ToSql(this RegexpOptions options)
	{
		StringBuilder result = new();
		result.Append("'");

		if (options.HasFlag(RegexpOptions.CaseSensitive))
		{
			result.Append("c");
		}

		if (options.HasFlag(RegexpOptions.CaseInsensitive))
		{
			result.Append("i");
		}

		if (options.HasFlag(RegexpOptions.MultipleLines))
		{
			result.Append("m");
		}

		if (options.HasFlag(RegexpOptions.NewLine))
		{
			result.Append("n");
		}

		if (options.HasFlag(RegexpOptions.ExcludingWhiteSpace))
		{
			result.Append("x");
		}

		result.Append("'");
		return result.ToString();
	}
}
