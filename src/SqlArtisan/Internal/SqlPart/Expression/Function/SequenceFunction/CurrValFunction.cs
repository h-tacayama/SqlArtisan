namespace SqlArtisan.Internal;

public sealed class CurrValFunction : SqlExpression
{
    private readonly string _sequenceName;

    internal CurrValFunction(string sequenceName)
    {
        _sequenceName = sequenceName;
    }

    internal override void Format(SqlBuildingBuffer buffer) => buffer
        .Append($"{Keywords.CurrVal}('{_sequenceName}')");
}
