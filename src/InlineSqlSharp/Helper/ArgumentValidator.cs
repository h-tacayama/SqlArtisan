using System.Runtime.CompilerServices;

namespace InlineSqlSharp;

internal static class ArgumentValidator
{
	public static string NotEmpty(
		string value,
		[CallerArgumentExpression(nameof(value))] string? expression = null)
	{
		if (!string.IsNullOrEmpty(value))
		{
			return value;
		}

		throw new ArgumentException($"'{expression}' cannot be empty");
	}
}
