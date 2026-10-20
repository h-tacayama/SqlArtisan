using SqlArtisan.Internal;

namespace SqlArtisan;

/// <summary>
/// An optimizer hint comment placed after <c>SELECT</c>, from
/// <see cref="Sql.Hints(string)"/>.
/// </summary>
public sealed class SqlHints : SqlPart
{
    private readonly string _hints;

    internal SqlHints(string hints)
    {
        _hints = hints ?? "";
    }

    // Trailing space rides with the text so empty hints leave no stray separator.
    internal override void Format(SqlBuildingBuffer buffer)
    {
        if (_hints.Length == 0)
        {
            return;
        }

        buffer.Append(_hints).AppendSpace();
    }
}
