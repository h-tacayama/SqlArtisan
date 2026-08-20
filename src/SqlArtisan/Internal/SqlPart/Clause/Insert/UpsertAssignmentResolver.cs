namespace SqlArtisan.Internal;

internal static class UpsertAssignmentResolver
{
    internal static EqualCondition[] Resolve(EqualityCondition[] items, string emptyMessage)
    {
        CollectionGuard.ThrowIfEmpty(items, emptyMessage);

        var assignments = new EqualCondition[items.Length];

        for (int i = 0; i < items.Length; i++)
        {
            if (items[i] is null)
            {
                throw new ArgumentNullException(
                    nameof(items), ExpressionResolver.NullValueMessage);
            }
            else if (items[i] is not EqualCondition)
            {
                throw ExpressionResolver.UnresolvableValue("Assignment", items[i]);
            }

            assignments[i] = (EqualCondition)items[i];
        }

        return assignments;
    }
}
