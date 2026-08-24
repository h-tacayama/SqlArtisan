namespace SqlArtisan.Internal;

// The shared assignment-list resolver behind every SET-shaped clause — UPDATE
// and SET-like INSERT, DO UPDATE SET, ON DUPLICATE KEY UPDATE, and MERGE's
// UPDATE SET — so the null/shape/left-side checks live once.
internal static class AssignmentResolver
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
            else if (items[i] is not EqualCondition assignment)
            {
                throw ExpressionResolver.UnresolvableValue("Assignment", items[i]);
            }
            else
            {
                // `Set(Abs(t.Code) == 5)` compiles — `==` is overloaded on every
                // expression — but every dialect rejects a computed assignment
                // target, so the shape fails here instead of at the database.
                if (assignment.LeftSide is not DbColumn)
                {
                    throw new ArgumentException(
                        "The left side of a SET assignment must be a column.");
                }

                assignments[i] = assignment;
            }
        }

        return assignments;
    }
}
