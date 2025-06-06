namespace SqlArtisan.Internal;
using static SqlArtisan.Internal.Keywords;

public sealed class NextValueForFunction : SqlExpression
{
    private readonly string _sequenceName;

    internal NextValueForFunction(string sequenceName)
    {
        _sequenceName = sequenceName;
    }

    internal override void Format(SqlBuildingBuffer buffer) => buffer
        .Append($"{Next} {Value} {For} {_sequenceName}");
}
