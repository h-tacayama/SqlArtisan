namespace SqlArtisan;

internal sealed class SqlPartAgent(Action<SqlBuildingBuffer> formatAction) : SqlPart
{
    private readonly Action<SqlBuildingBuffer> _formatAction = formatAction;

    internal override void Format(SqlBuildingBuffer buffer) =>
        _formatAction.Invoke(buffer);
}
