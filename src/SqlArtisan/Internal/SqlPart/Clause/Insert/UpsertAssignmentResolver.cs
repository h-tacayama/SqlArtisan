namespace SqlArtisan.Internal;

internal static class UpsertAssignmentResolver
{
    internal static EqualityCondition[] Resolve(EqualityBasedCondition[] items, string emptyMessage)
    {
        CollectionGuard.ThrowIfEmpty(items, emptyMessage);

        var assignments = new EqualityCondition[items.Length];

        for (int i = 0; i < items.Length; i++)
        {
            if (items[i] is null)
            {
                throw new ArgumentNullException(
                    nameof(items), ExpressionResolver.NullValueMessage);
            }
            else if (items[i] is not EqualityCondition)
            {
                throw new ArgumentException(
                    $"Invalid type for EqualityCondition: {items[i].GetType()}");
            }

            assignments[i] = (EqualityCondition)items[i];
        }

        return assignments;
    }
}
