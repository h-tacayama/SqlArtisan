using System.Diagnostics;

namespace InlineSqlSharp;

public static partial class SqlWordbook
{
	[DebuggerBrowsable(DebuggerBrowsableState.Never)]
	public static AllOrDistinct DISTINCT => AllOrDistinct.Distinct;
}
