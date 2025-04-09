namespace InlineSqlSharp;

public sealed class CharacterLiteral(string value) : CharacterExpr, ILiteral
{
    public CharacterLiteral(char value) : this(value.ToString()) { }

    private readonly string _value = value;

    public override void FormatSql(SqlBuildingBuffer buffer) =>
        buffer.EncloseInSingleQuotes(_value);
}
