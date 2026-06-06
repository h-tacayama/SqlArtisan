namespace SqlArtisan.Internal;

internal sealed class ReturningIntoClause : SqlPart
{
    private readonly SqlPart[] _returningItems;
    private readonly string[] _variables;

    internal ReturningIntoClause(SqlPart[] returningItems, string[] variables)
    {
        _returningItems = returningItems;
        _variables = variables;
    }

    internal override void Format(SqlBuildingBuffer buffer)
    {
        buffer.Append($"{Keywords.Returning} ");
        buffer.AppendSelectItems(_returningItems);
        buffer.Append($" {Keywords.Into} ");

        for (int i = 0; i < _variables.Length; i++)
        {
            if (i > 0)
            {
                buffer.Append(", ");
            }

            buffer.AddOutParameter(_variables[i]);
        }
    }
}
