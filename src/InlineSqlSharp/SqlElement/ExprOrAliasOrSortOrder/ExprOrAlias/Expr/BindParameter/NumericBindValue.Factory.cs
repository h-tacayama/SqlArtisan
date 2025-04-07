using System.Numerics;

namespace InlineSqlSharp;

internal static class NumericBindValue
{
	internal static NumericExpr Of(Enum value) =>
		Enum.GetUnderlyingType(value.GetType()) switch
		{
			Type type when type == typeof(sbyte) => Create<sbyte>(value),
			Type type when type == typeof(byte) => Create<byte>(value),
			Type type when type == typeof(short) => Create<short>(value),
			Type type when type == typeof(ushort) => Create<ushort>(value),
			Type type when type == typeof(int) => Create<int>(value),
			Type type when type == typeof(uint) => Create<uint>(value),
			Type type when type == typeof(long) => Create<long>(value),
			Type type when type == typeof(ulong) => Create<ulong>(value),
			_ => throw new ArgumentException("Unsupported enum underlying type."),
		};

	private static NumericExpr Create<T>(Enum value) where T : INumber<T> =>
		new NumericBindValue<T>((T)Convert.ChangeType(value, typeof(T)));
}
