namespace InlineSqlSharp;

public sealed class CharacterNull : CharacterExpr
{
    public override void FormatSql(SqlBuildingBuffer buffer) =>
        buffer.Append(Keywords.NULL);
}
