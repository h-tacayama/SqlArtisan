using System.Data;

namespace InlineSqlSharp;

public sealed class CharacterBindValue(
    string value,
    DbType? dbType = null,
    ParameterDirection? direction = null) :
    CharacterExpr,
    IBindValue
{
    public CharacterBindValue(
        char value,
        DbType? dbType = null,
        ParameterDirection? direction = null)
        : this(value.ToString(), dbType, direction) { }

    public object Value => value;

    public DbType? DbType => dbType;

    public ParameterDirection? Direction => direction;

    public override void FormatSql(SqlBuildingBuffer buffer) =>
        buffer.AddParameter(this);
}
