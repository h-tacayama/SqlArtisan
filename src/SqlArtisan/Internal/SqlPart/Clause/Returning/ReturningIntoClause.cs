namespace SqlArtisan.Internal;

internal sealed class ReturningIntoClause : SqlPart
{
    private readonly SqlPart[] _returningItems;
    private readonly OutputParameter[] _outputs;

    internal ReturningIntoClause(SqlPart[] returningItems, OutputParameter[] outputs)
    {
        _returningItems = returningItems;
        _outputs = outputs;
    }

    internal override void Format(SqlBuildingBuffer buffer)
    {
        buffer.Append($"{Keywords.Returning} ");
        buffer.AppendSelectItems(_returningItems);
        buffer.Append($" {Keywords.Into} ");

        for (int i = 0; i < _outputs.Length; i++)
        {
            if (i > 0)
            {
                buffer.Append(", ");
            }

            buffer.AddOutParameter(_outputs[i]);
        }
    }
}
