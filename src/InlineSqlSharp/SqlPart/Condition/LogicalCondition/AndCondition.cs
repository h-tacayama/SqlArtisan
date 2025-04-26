namespace InlineSqlSharp;

public sealed class AndCondition : AbstractCondition
{
    private readonly AbstractCondition[] _conditions;

    internal AndCondition(AbstractCondition[] conditions)
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
                buffer.EncloseInSpaces(Keywords.AND);
            }

            buffer.EncloseInParentheses(_conditions[i]);
            added = true;
        }
    }
}
