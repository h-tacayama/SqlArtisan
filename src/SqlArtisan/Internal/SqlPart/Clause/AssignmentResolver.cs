namespace SqlArtisan.Internal;

// The shared assignment-list resolver behind every SET-shaped clause, so the
// null/shape/left-side checks live once. Each list message names the caller's own
// clause: ON DUPLICATE KEY UPDATE has no SET token to name.
internal static class AssignmentResolver
{
    internal static EqualCondition[] Resolve(EqualityCondition[] assignments, string clause)
    {
        // Inline rather than CollectionGuard.ThrowIfEmpty, which takes the message
        // eagerly: interpolating it would allocate on every call (ADR 0006).
        ArgumentNullException.ThrowIfNull(assignments);
        if (assignments.Length == 0)
        {
            throw new ArgumentException($"{clause} requires at least one assignment.");
        }

        var resolved = new EqualCondition[assignments.Length];

        for (int i = 0; i < assignments.Length; i++)
        {
            if (assignments[i] is null)
            {
                throw new ArgumentNullException(
                    nameof(assignments),
                    $"The {clause} assignment list must not contain a null assignment.");
            }
            else if (assignments[i] is not EqualCondition assignment)
            {
                throw ExpressionResolver.UnresolvableValue("Assignment", assignments[i]);
            }
            else
            {
                // `Set(Abs(t.Code) == 5)` compiles (`==` is overloaded on every expression),
                // but no dialect accepts a computed assignment target: fail here, not at the DB.
                if (assignment.LeftSide is not DbColumn)
                {
                    throw new ArgumentException(
                        $"The left side of each {clause} assignment must be a column.");
                }

                // `==` takes ANY / ALL / SOME as a comparison's right side; a SET value is no
                // comparison (#666).
                if (assignment.RightSide is PositionBoundOperand)
                {
                    throw new ArgumentException(
                        $"The {clause} assignment list cannot take ANY, ALL or SOME; "
                            + "assign a value or a scalar subquery.");
                }

                resolved[i] = assignment;
            }
        }

        return resolved;
    }

    internal static void ThrowIfNotTargetColumn(
        EqualCondition[] assignments,
        DbTableBase target,
        string clause)
    {
        for (int i = 0; i < assignments.Length; i++)
        {
            if (!ColumnListGuard.IsColumnOf((DbColumn)assignments[i].LeftSide, target))
            {
                throw new ArgumentException(
                    $"The {clause} assignment list must assign only columns of the target table.");
            }
        }
    }

    // These arms qualify a column by its owner's alias, so an unaliased owner, whichever
    // table it is, renders bare beside the joined tables and may bind the wrong one (#674).
    internal static void ThrowIfUnaliasedOwner(EqualCondition[] assignments)
    {
        for (int i = 0; i < assignments.Length; i++)
        {
            if (((DbColumn)assignments[i].LeftSide).Owner.CorrelationName.Length == 0)
            {
                throw new ArgumentException(
                    "In a joined UPDATE, each SET column must belong to an aliased table.");
            }
        }
    }

    // Keyed on the token the target renders as, never on its owner: the
    // always-unqualified clauses drop owner and alias, so two handles — or two
    // tables — sharing a column name emit `SET a = 1, a = 2` all the same.
    internal static void ThrowIfDuplicateTarget(
        EqualCondition[] assignments, bool qualified, string clause)
    {
        for (int i = 1; i < assignments.Length; i++)
        {
            DbColumn target = (DbColumn)assignments[i].LeftSide;
            for (int j = 0; j < i; j++)
            {
                DbColumn earlier = (DbColumn)assignments[j].LeftSide;
                if (RendersSameToken(target, earlier, qualified))
                {
                    throw new ArgumentException(
                        $"The {clause} assignment list must not assign the same column twice.");
                }
            }
        }
    }

    private static bool RendersSameToken(DbColumn target, DbColumn earlier, bool qualified)
    {
        if (!string.Equals(target.Name, earlier.Name, StringComparison.Ordinal))
        {
            return false;
        }

        return !qualified
            || string.Equals(
                target.Owner.CorrelationName,
                earlier.Owner.CorrelationName,
                StringComparison.Ordinal);
    }
}
