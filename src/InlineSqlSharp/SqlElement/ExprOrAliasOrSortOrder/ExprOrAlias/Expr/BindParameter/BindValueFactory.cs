using System.Data;

namespace InlineSqlSharp;

internal static class BindValueFactory
{
    public static IBindValue CreateOrException(
        object value,
        ParameterDirection direction = ParameterDirection.Input) =>
        // Ordered by estimated frequency.
        value switch
        {
            string s => new CharacterBindValue(s),
            int i => new NumericBindValue<int>(i),
            DateTime dt => new DateTimeBindValue(dt),
            long l => new NumericBindValue<long>(l),
            decimal de => new NumericBindValue<decimal>(de),
            double d => new NumericBindValue<double>(d),
            float f => new NumericBindValue<float>(f),
            short sh => new NumericBindValue<short>(sh),
            byte b => new NumericBindValue<byte>(b),
            char c => new CharacterBindValue(c),
            uint ui => new NumericBindValue<uint>(ui),
            ulong ul => new NumericBindValue<ulong>(ul),
            ushort ush => new NumericBindValue<ushort>(ush),
            sbyte sb => new NumericBindValue<sbyte>(sb),
            _ => throw new NotSupportedException(
                $"The type {value.GetType().FullName} is not supported for binding.")
        };
}
