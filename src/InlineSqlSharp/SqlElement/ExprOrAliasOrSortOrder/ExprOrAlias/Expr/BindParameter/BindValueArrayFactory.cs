using System.Numerics;

namespace InlineSqlSharp;

internal static class BindValueArrayFactory
{
	public static CharacterBindValue[] Create(char[] values) =>
		CreateCore(values, value => new CharacterBindValue(value));

	public static CharacterBindValue[] Create(string[] values) =>
		CreateCore(values, value => new CharacterBindValue(value));

	public static DateTimeBindValue[] Create(DateTime[] values) =>
		CreateCore(values, value => new DateTimeBindValue(value));

	public static NumericBindValue<TValue>[] Create<TValue>(TValue[] values)
		where TValue : INumber<TValue> =>
		CreateCore(values, value => new NumericBindValue<TValue>(value));

	private static TBindValue[] CreateCore<TValue, TBindValue>(
		TValue[] values,
		Func<TValue, TBindValue> factoryMethod)
	{
		var bindArray = new TBindValue[values.Length];

		for (int i = 0; i < values.Length; i++)
		{
			bindArray[i] = factoryMethod(values[i]);
		}

		return bindArray;
	}
}
