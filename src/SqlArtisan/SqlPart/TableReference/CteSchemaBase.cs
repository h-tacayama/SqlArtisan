using SqlArtisan.Internal;

namespace SqlArtisan;

public abstract class CteSchemaBase(string name) : DerivedTableSchemaBase(name)
{
    private readonly string _name = name;

    public CommonTableExpression As(ISubquery subquery) => new(_name, subquery);
}
