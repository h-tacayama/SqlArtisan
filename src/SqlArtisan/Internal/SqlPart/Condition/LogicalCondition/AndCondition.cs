namespace SqlArtisan.Internal;

public sealed class AndCondition : SqlCondition
{
    private readonly SqlCondition _first;
    private readonly SqlCondition _second;

    // Third and later operands of a chained `a & b & c` only; the common binary
    // AND keeps this null and allocates no list.
    private List<SqlCondition>? _rest;

    internal AndCondition(SqlCondition leftSide, SqlCondition rightSide)
    {
        _first = leftSide;
        _second = rightSide;
    }

    // An AND group renders nothing when every operand is empty; the enclosing
    // clause is then elided (or, on DML, rejected) rather than emitting `()`.
    internal override bool IsEmpty
    {
        get
        {
            if (!_first.IsEmpty || !_second.IsEmpty)
            {
                return false;
            }

            if (_rest is not null)
            {
                for (int i = 0; i < _rest.Count; i++)
                {
                    if (!_rest[i].IsEmpty)
                    {
                        return false;
                    }
                }
            }

            return true;
        }
    }

    internal override void Format(SqlBuildingBuffer buffer)
    {
        bool added = false;

        FormatOperand(buffer, _first, ref added);
        FormatOperand(buffer, _second, ref added);

        if (_rest is not null)
        {
            for (int i = 0; i < _rest.Count; i++)
            {
                FormatOperand(buffer, _rest[i], ref added);
            }
        }
    }

    internal void Add(SqlCondition condition)
    {
        _rest ??= [];
        _rest.Add(condition);
    }

    private static void FormatOperand(
        SqlBuildingBuffer buffer,
        SqlCondition condition,
        ref bool added)
    {
        if (condition.IsEmpty)
        {
            return;
        }

        if (added)
        {
            buffer.EncloseInSpaces(Keywords.And);
        }

        buffer.EncloseInParentheses(condition);
        added = true;
    }
}
