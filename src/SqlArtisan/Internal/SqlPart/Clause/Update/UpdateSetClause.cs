﻿namespace SqlArtisan.Internal;

internal sealed class UpdateSetClause : SqlPart
{
    private readonly EqualityCondition[] _assignments;

    private UpdateSetClause(params EqualityCondition[] assignments)
    {
        _assignments = assignments;
    }

    internal static UpdateSetClause Parse(EqualityBasedCondition[] items)
    {
        var assignments = new EqualityCondition[items.Length];

        for (int i = 0; i < items.Length; i++)
        {
            if (items[i] is not EqualityCondition)
            {
                throw new ArgumentException(
                    $"Invalid type for EqualityCondition: {items[i].GetType()}");
            }

            assignments[i] = (EqualityCondition)items[i];
        }

        return new(assignments);
    }

    internal override void Format(SqlBuildingBuffer buffer) => buffer
        .Append($"{Keywords.Set} ")
        .AppendCsv(_assignments);
}
