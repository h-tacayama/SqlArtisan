namespace SqlArtisan.Internal;

// The shared assignment-list resolver behind every SET-shaped clause, so the
// null/shape/left-side checks live once.
internal static class AssignmentResolver
{
    internal static EqualCondition[] Resolve(EqualityCondition[] assignments, string emptyMessage)
    {
        CollectionGuard.ThrowIfEmpty(assignments, nameof(assignments), emptyMessage);

        var resolved = new EqualCondition[assignments.Length];

        for (int i = 0; i < assignments.Length; i++)
        {
            if (assignments[i] is null)
            {
                throw new ArgumentNullException(
                    nameof(assignments), "A SET assignment list must not contain a null assignment.");
            }
            else if (assignments[i] is not EqualCondition assignment)
            {
                throw ExpressionResolver.UnresolvableValue("Assignment", assignments[i]);
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

                resolved[i] = assignment;
            }
        }

        return resolved;
    }
}
