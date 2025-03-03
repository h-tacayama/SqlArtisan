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
