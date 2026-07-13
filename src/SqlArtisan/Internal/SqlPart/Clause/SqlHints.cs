namespace SqlArtisan.Internal;

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
