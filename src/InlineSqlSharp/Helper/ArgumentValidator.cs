using System.Runtime.CompilerServices;

namespace InlineSqlSharp;

internal static class ArgumentValidator
{
	internal static string ThrowIfNullOrEmpty(
		string value,
		[CallerArgumentExpression(nameof(value))] string? expression = null)
	{
		if (string.IsNullOrEmpty(value))
		{
			throw new ArgumentException(
				$"'{expression}' cannot be empty.",
				expression);
		}

		return value;
	}

	internal static TSecondary ThrowIfPrecedingNull<TPrimary, TSecondary>(
		TPrimary preceding,
		TSecondary self,
		[CallerArgumentExpression(nameof(preceding))] string? precedingExpr = null,
		[CallerArgumentExpression(nameof(self))] string? selfExpr = null)
		where TPrimary : class?
		where TSecondary : class?
	{
		if (self is not null && preceding is null)
		{
			throw new ArgumentException(
				$"'{precedingExpr}' cannot be null when '{selfExpr}' is not null.",
				precedingExpr);
		}
	
		return self;
	}
}
