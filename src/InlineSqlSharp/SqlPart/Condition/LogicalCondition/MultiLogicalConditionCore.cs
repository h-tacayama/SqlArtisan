namespace InlineSqlSharp;

internal sealed class MultiLogicalConditionCore(
    string @operator,
    AbstractCondition[] conditions)
{
    private readonly string _operator = @operator;
    private readonly AbstractCondition[] _conditions = conditions;

    public void FormatSql(SqlBuildingBuffer buffer)
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
                buffer.EncloseInSpaces(_operator);
            }

            buffer.EncloseInParentheses(_conditions[i]);
            added = true;
        }
    }
}
