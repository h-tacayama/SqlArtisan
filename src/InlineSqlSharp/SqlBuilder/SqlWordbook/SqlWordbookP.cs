using System.Numerics;

namespace InlineSqlSharp;

public static partial class SqlWordbook
{
	public static CharacterBindValue P(string value) => new(value);

	public static DateTimeBindValue P(DateTime value) => new(value);

	public static NumericBindValue<TValue> P<TValue>(TValue value)
		where TValue : INumber<TValue> => new(value);

	public static PartitionByClause PARTITION_BY(
		params IExpr[] expressions) => new(expressions);

	public static AnalyticPercentRankFunction PERCENT_RANK() => new();
}
