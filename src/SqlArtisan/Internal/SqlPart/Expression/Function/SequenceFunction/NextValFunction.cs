namespace SqlArtisan.Internal;

public sealed class NextValFunction : SqlExpression
{
    private readonly string _sequenceName;

    internal NextValFunction(string sequenceName)
    {
        _sequenceName = sequenceName;
    }

    internal override void Format(SqlBuildingBuffer buffer) => buffer
        .Append($"{Keywords.NextVal}('{_sequenceName}')");
}
