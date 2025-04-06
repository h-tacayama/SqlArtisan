namespace InlineSqlSharp;

internal static class CharacterBindValueArray
{
	public static CharacterBindValue[] Create(char[] values)
	{
		var bindArray = new CharacterBindValue[values.Length];

		for (int i = 0; i < values.Length; i++)
		{
			bindArray[i] = new CharacterBindValue(values[i]);
		}

		return bindArray;
	}

	public static CharacterBindValue[] Create(string[] values)
	{
		var bindArray = new CharacterBindValue[values.Length];

		for (int i = 0; i < values.Length; i++)
		{
			bindArray[i] = new CharacterBindValue(values[i]);
		}

		return bindArray;
	}
}
