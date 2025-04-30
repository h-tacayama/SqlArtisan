namespace SqlArtisan;

public abstract class AbstractCondition : AbstractSqlPart
{
    public static AndCondition operator &(
        AbstractCondition leftSide,
        AbstractCondition rightSide)
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
        AbstractCondition leftSide,
        AbstractCondition rightSide)
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
