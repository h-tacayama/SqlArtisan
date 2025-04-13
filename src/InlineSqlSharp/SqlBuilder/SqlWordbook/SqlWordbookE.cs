using System.Numerics;

namespace InlineSqlSharp;

public static partial class SqlWordbook
{
    public static CaseElseExpr<CharacterExpr> ELSE(CharacterExpr thenExpr) =>
        new(thenExpr);

    public static CaseElseExpr<CharacterExpr> ELSE(char thenExpr) =>
        new(new CharacterBindValue(thenExpr));

    public static CaseElseExpr<CharacterExpr> ELSE(string thenExpr) =>
        new(new CharacterBindValue(thenExpr));

    public static CaseElseExpr<DateTimeExpr> ELSE(DateTimeExpr thenExpr) =>
        new(thenExpr);

    public static CaseElseExpr<DateTimeExpr> ELSE(DateTime thenExpr) =>
        new(new DateTimeBindValue(thenExpr));

    public static CaseElseExpr<NumericExpr> ELSE(NumericExpr thenExpr) =>
        new(thenExpr);

    public static CaseElseExpr<NumericExpr> ELSE<TElse>(TElse thenExpr)
        where TElse : INumber<TElse> =>
        new(new NumericBindValue<TElse>(thenExpr));

    public static ExistsCondition EXISTS(ISubquery subquery) =>
        new(subquery);
}
