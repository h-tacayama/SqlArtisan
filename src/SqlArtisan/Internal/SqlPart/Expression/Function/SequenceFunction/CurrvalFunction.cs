namespace SqlArtisan.Internal;

public sealed class CurrvalFunction : SqlExpression
{
    private readonly string _sequenceName;

    internal CurrvalFunction(string sequenceName)
    {
        _sequenceName = sequenceName;
    }

    internal override void Format(SqlBuildingBuffer buffer) => buffer
        .Append($"{Keywords.Currval}('{_sequenceName}')");
}
