using System.Data;

namespace SqlArtisan.Internal;

// A SQL NULL a value converter returned. It binds where NULL is a value, such as a VALUES
// row or a SET, but a comparison rejects it: written nowhere the call site or the analyzer
// sees, it would silently match no row, the class a C# null is rejected for (#810).
internal sealed class ConvertedNull(Type source, DbType? dbType, int? size)
    : BindValue(DBNull.Value, dbType, null, size)
{
    internal ArgumentException Compared(string construct) =>
        new($"{construct} never matches the NULL the value converter for {source} returned; "
            + "test for NULL with .IsNull or .IsNotNull.");

    internal ArgumentException InSimpleCase() =>
        new($"A simple CASE never matches the NULL the value converter for {source} returned; "
            + "test for NULL with a searched When(expr.IsNull).");
}
