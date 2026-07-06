using SqlArtisan.Internal;

namespace SqlArtisan;

/// <summary>
/// A SQL boolean predicate — the base type of every <c>WHERE</c> / <c>HAVING</c>
/// / <c>ON</c> condition (comparisons, <c>LIKE</c>, <c>IN</c>, <c>EXISTS</c>, and
/// more). Hold a variable number of conditions as this type — a nullable
/// accumulator, a helper method's return type — without naming any concrete
/// condition class.
/// </summary>
public abstract class SqlCondition : SqlPart
{
    /// <summary>
    /// Combines two conditions with <c>AND</c>: <c>(<paramref name="leftSide"/>)
    /// AND (<paramref name="rightSide"/>)</c>. Chaining three or more with
    /// <c>&amp;</c> flattens into one <c>AND</c> group instead of nesting.
    /// </summary>
    /// <param name="leftSide">The left operand.</param>
    /// <param name="rightSide">The right operand.</param>
    /// <returns>The combined <c>AND</c> condition.</returns>
    public static AndCondition operator &(
        SqlCondition leftSide,
        SqlCondition rightSide)
    {
        if (leftSide is AndCondition andCondition)
        {
            andCondition.Add(rightSide);
            return andCondition;
        }
        else
        {
            return new(leftSide, rightSide);
        }
    }

    /// <summary>
    /// Combines two conditions with <c>OR</c>: <c>(<paramref name="leftSide"/>)
    /// OR (<paramref name="rightSide"/>)</c>. Chaining three or more with
    /// <c>|</c> flattens into one <c>OR</c> group instead of nesting.
    /// </summary>
    /// <param name="leftSide">The left operand.</param>
    /// <param name="rightSide">The right operand.</param>
    /// <returns>The combined <c>OR</c> condition.</returns>
    public static OrCondition operator |(
        SqlCondition leftSide,
        SqlCondition rightSide)
    {
        if (leftSide is OrCondition orCondition)
        {
            orCondition.Add(rightSide);
            return orCondition;
        }
        else
        {
            return new(leftSide, rightSide);
        }
    }
}
