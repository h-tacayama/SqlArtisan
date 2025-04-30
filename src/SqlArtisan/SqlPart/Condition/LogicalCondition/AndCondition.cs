namespace SqlArtisan;

public sealed class AndCondition : AbstractCondition
{
    private readonly List<AbstractCondition> _conditions;

    internal AndCondition(AbstractCondition leftSide, AbstractCondition rightRide)
    {
        _conditions = new List<AbstractCondition>
        {
            leftSide,
            rightRide
        };
    }

    internal override void FormatSql(SqlBuildingBuffer buffer)
    {
        bool added = false;

        for (int i = 0; i < _conditions.Count; i++)
        {
            if (_conditions[i] is EmptyCondition)
            {
                continue;
            }

            if (added)
            {
                buffer.EncloseInSpaces(Keywords.And);
            }

            buffer.EncloseInParentheses(_conditions[i]);
            added = true;
        }
    }

    internal void Add(AbstractCondition condition)
    {
        _conditions.Add(condition);
    }
}
