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
            string s => new CharacterBindValue(s, direction),
            int i => new NumericBindValue<int>(i, direction),
            DateTime dt => new DateTimeBindValue(dt, direction),
            long l => new NumericBindValue<long>(l, direction),
            decimal de => new NumericBindValue<decimal>(de, direction),
            double d => new NumericBindValue<double>(d, direction),
            float f => new NumericBindValue<float>(f, direction),
            short sh => new NumericBindValue<short>(sh, direction),
            byte b => new NumericBindValue<byte>(b, direction),
            char c => new CharacterBindValue(c, direction),
            uint ui => new NumericBindValue<uint>(ui, direction),
            ulong ul => new NumericBindValue<ulong>(ul, direction),
            ushort ush => new NumericBindValue<ushort>(ush, direction),
            sbyte sb => new NumericBindValue<sbyte>(sb, direction),
            _ => throw new NotSupportedException(
                $"The type {value.GetType().FullName} is not supported for binding.")
        };
}
