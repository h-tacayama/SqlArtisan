using SqlArtisan.Internal;

namespace SqlArtisan;

public abstract class CteSchemaBase(string name) : DerivedTableSchemaBase(name)
{
    public CommonTableExpression As(ISubquery subquery) => new(SchemaName, subquery);
}
