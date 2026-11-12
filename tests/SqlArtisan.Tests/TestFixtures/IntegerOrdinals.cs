namespace SqlArtisan.Tests;

// Every integral type the ordinal resolvers render as a position; nint and nuint cannot be
// attribute arguments, so the theories read them from here.
internal static class IntegerOrdinals
{
    public static TheoryData<object> One => new()
    {
        (sbyte)1, (byte)1, (short)1, (ushort)1, 1, 1u, (nint)1, (nuint)1, 1L, 1UL,
    };
}
