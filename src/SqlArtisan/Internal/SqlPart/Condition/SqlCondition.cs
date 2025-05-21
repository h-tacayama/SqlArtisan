namespace SqlArtisan.Internal;

public abstract class SqlCondition : SqlPart
{
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
