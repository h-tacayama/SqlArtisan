namespace SqlArtisan.Internal;

// Shared operand storage/rendering for AndCondition/OrCondition (#399):
// first two operands as fixed fields, third-and-later a copy-on-write array.
// A struct, not a class, so it lives inline in the owner (ADR 0006).
internal readonly struct LogicalConditionCore
{
    private readonly SqlCondition _first;
    private readonly SqlCondition _second;
    private readonly SqlCondition[]? _rest;

    internal LogicalConditionCore(SqlCondition first, SqlCondition second)
    {
        _first = first;
        _second = second;
    }

    private LogicalConditionCore(LogicalConditionCore existing, SqlCondition additionalOperand)
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

    // An AND/OR group is empty only when every operand is empty; a lone empty
    // operand beside a non-empty one drops out in Format (so no `()`).
    internal bool IsEmpty
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

    internal LogicalConditionCore Extend(SqlCondition additionalOperand) => new(this, additionalOperand);

    // A parameter, not a field, so this struct stays the same size either way.
    internal void Format(SqlBuildingBuffer buffer, string keyword)
    {
        bool added = false;

        FormatOperand(buffer, _first, keyword, ref added);
        FormatOperand(buffer, _second, keyword, ref added);

        if (_rest is not null)
        {
            for (int i = 0; i < _rest.Length; i++)
            {
                FormatOperand(buffer, _rest[i], keyword, ref added);
            }
        }
    }

    private static void FormatOperand(
        SqlBuildingBuffer buffer,
        SqlCondition condition,
        string keyword,
        ref bool added)
    {
        if (condition.IsEmpty)
        {
            return;
        }

        if (added)
        {
            buffer.EncloseInSpaces(keyword);
        }

        buffer.EncloseInParentheses(condition);
        added = true;
    }
}
