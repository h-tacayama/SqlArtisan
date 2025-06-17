namespace SqlArtisan.Internal;

public sealed class NextvalFunction : SqlExpression
{
    private readonly string _sequenceName;

    internal NextvalFunction(string sequenceName)
    {
        _sequenceName = sequenceName;
    }

    internal override void Format(SqlBuildingBuffer buffer) => buffer
        .Append($"{Keywords.Nextval}('{_sequenceName}')");
}
