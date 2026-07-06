using SqlArtisan.Internal;

namespace SqlArtisan;

/// <summary>
/// An optimizer-hint block emitted verbatim after <c>SELECT</c>
/// (<c>SELECT /*+ ... */ ...</c>), obtained from <c>Hints("/*+ ... */")</c>.
/// Reusable across statements — e.g. a shared <see langword="static"/>
/// <see langword="readonly"/> field.
/// </summary>
public sealed class SqlHints : SqlPart
{
    private readonly string _hints;

    internal SqlHints(string hints)
    {
        _hints = hints;
    }

    internal override void Format(SqlBuildingBuffer buffer) =>
        buffer.Append(_hints);
}
