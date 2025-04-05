using System.Numerics;

namespace InlineSqlSharp;

internal static class NumericBindValueArray
{
	public static NumericBindValue<TValue>[] Create<TValue>(TValue[] values)
		where TValue : INumber<TValue>
	{
		var bindArray = new NumericBindValue<TValue>[values.Length];

		for (int i = 0; i < values.Length; i++)
		{
			bindArray[i] = new NumericBindValue<TValue>(values[i]);
		}

		return bindArray;
	}
}
