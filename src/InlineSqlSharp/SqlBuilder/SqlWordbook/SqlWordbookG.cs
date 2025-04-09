namespace InlineSqlSharp;

public static partial class SqlWordbook
{
    public static CharacterGreatestFunction GREATEST(params CharacterExpr[] expressions) =>
        new(expressions);

    public static DateTimeGreatestFunction GREATEST(params DateTimeExpr[] expressions) =>
        new(expressions);

    public static NumericGreatestFunction GREATEST(params NumericExpr[] expressions) =>
        new(expressions);
}
