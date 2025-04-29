namespace SqlArtisan;

public sealed class OrCondition : AbstractCondition
{
    private readonly AbstractCondition[] _conditions;

    internal OrCondition(AbstractCondition[] conditions)
    {
        _conditions = conditions;
    }

    internal override void FormatSql(SqlBuildingBuffer buffer)
    {
        bool added = false;

        for (int i = 0; i < _conditions.Length; i++)
        {
            if (_conditions[i] is EmptyCondition)
            {
                continue;
            }

            if (added)
            {
                buffer.EncloseInSpaces(Keywords.Or);
            }

            buffer.EncloseInParentheses(_conditions[i]);
            added = true;
        }
    }
}
