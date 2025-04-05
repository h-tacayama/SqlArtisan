namespace InlineSqlSharp;

internal static class DateTimeBindValueArray
{
	public static DateTimeBindValue[] Create(DateTime[] values)
	{
		var bindArray = new DateTimeBindValue[values.Length];

		for (int i = 0; i < values.Length; i++)
		{
			bindArray[i] = new DateTimeBindValue(values[i]);
		}

		return bindArray;
	}
}
