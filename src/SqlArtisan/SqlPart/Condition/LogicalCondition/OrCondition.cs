namespace SqlArtisan;

public sealed class OrCondition : SqlCondition
{
    private readonly List<SqlCondition> _conditions;

    internal OrCondition(SqlCondition leftSide, SqlCondition rightRide)
    {
        _conditions = new List<SqlCondition>
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
                buffer.EncloseInSpaces(Keywords.Or);
            }

            buffer.EncloseInParentheses(_conditions[i]);
            added = true;
        }
    }

    internal void Add(SqlCondition condition)
    {
        _conditions.Add(condition);
    }
}
