using System.Numerics;

namespace InlineSqlSharp;

internal static class EnumExtensions
{
	public static object ToUnderlyingValue(this Enum value) =>
		Enum.GetUnderlyingType(value.GetType()) switch
		{
			Type type when type == typeof(sbyte) => EnumToNumber<sbyte>(value),
			Type type when type == typeof(byte) => EnumToNumber<byte>(value),
			Type type when type == typeof(short) => EnumToNumber<short>(value),
			Type type when type == typeof(ushort) => EnumToNumber<ushort>(value),
			Type type when type == typeof(int) => EnumToNumber<int>(value),
			Type type when type == typeof(uint) => EnumToNumber<uint>(value),
			Type type when type == typeof(long) => EnumToNumber<long>(value),
			Type type when type == typeof(ulong) => EnumToNumber<ulong>(value),
			_ => throw new ArgumentException("Unsupported enum underlying type."),
		};

	private static TReturn EnumToNumber<TReturn>(Enum value)
		where TReturn : INumber<TReturn> =>
		(TReturn)Convert.ChangeType(value, typeof(TReturn));
}
