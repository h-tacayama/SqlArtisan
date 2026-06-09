namespace SqlArtisan.Internal;

internal sealed class WindowFrame : SqlPart
{
    private readonly string _unit;
    private readonly SqlPart _extent;

    internal WindowFrame(string unit, SqlPart extent)
    {
        _unit = unit;
        _extent = extent;
    }

    internal override void Format(SqlBuildingBuffer buffer) => buffer
        .Append(_unit)
        .AppendSpace()
        .Append(_extent);
}
