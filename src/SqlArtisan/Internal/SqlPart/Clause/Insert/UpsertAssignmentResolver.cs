namespace SqlArtisan.Internal;

internal static class UpsertAssignmentResolver
{
    internal static EqualityCondition[] Resolve(EqualityBasedCondition[] items)
    {
        var assignments = new EqualityCondition[items.Length];

        for (int i = 0; i < items.Length; i++)
        {
            if (items[i] is not EqualityCondition assignment)
            {
                throw new ArgumentException(
                    $"Invalid type for EqualityCondition: {items[i].GetType()}");
            }

            assignments[i] = assignment;
        }

        return assignments;
    }
}
