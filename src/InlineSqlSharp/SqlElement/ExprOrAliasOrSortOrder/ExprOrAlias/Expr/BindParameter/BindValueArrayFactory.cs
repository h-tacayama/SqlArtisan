using System.Numerics;

namespace InlineSqlSharp;

internal static class BindValueArrayFactory
{
    public static CharacterBindValue[] FromChar(char[] values) =>
        Create(values, value => new CharacterBindValue(value));

    public static CharacterBindValue[] FromString(string[] values) =>
        Create(values, value => new CharacterBindValue(value));

    public static DateTimeBindValue[] FromDateTime(DateTime[] values) =>
        Create(values, value => new DateTimeBindValue(value));

    public static NumericBindValue<TValue>[] FromNumber<TValue>(TValue[] values)
        where TValue : INumber<TValue> =>
        Create(values, value => new NumericBindValue<TValue>(value));

    public static EnumBindValue[] FromEnum<TEnum>(TEnum[] values)
        where TEnum : Enum =>
        Create(values, value => new EnumBindValue(value));

    private static TBindValue[] Create<TValue, TBindValue>(
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
