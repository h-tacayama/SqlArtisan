namespace SqlArtisan.Internal;

public sealed class AndCondition : SqlCondition
{
    private readonly SqlCondition _first;
    private readonly SqlCondition _second;

    // Third and later operands of a chained `a & b & c` only; the common binary
    // AND keeps this null and allocates no array.
    private readonly SqlCondition[]? _rest;

    internal AndCondition(SqlCondition leftSide, SqlCondition rightSide)
    {
        _first = leftSide;
        _second = rightSide;
    }

    // Copy-on-write extension of an existing AndCondition by one more operand
    // (operator &, #399): a fresh array so `existing` — possibly still held and
    // reused by other code — is never mutated.
    internal AndCondition(AndCondition existing, SqlCondition additionalOperand)
    {
        _first = existing._first;
        _second = existing._second;

        if (existing._rest is null)
        {
            _rest = [additionalOperand];
        }
        else
        {
            _rest = new SqlCondition[existing._rest.Length + 1];
            Array.Copy(existing._rest, _rest, existing._rest.Length);
            _rest[^1] = additionalOperand;
        }
    }

    // An AND group is empty only when every operand is empty; a lone empty operand
    // beside a non-empty one drops out in Format (so no `()`).
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
                for (int i = 0; i < _rest.Length; i++)
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
            for (int i = 0; i < _rest.Length; i++)
            {
                FormatOperand(buffer, _rest[i], ref added);
            }
        }
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
