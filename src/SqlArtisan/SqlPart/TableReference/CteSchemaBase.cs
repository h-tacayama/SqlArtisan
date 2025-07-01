using SqlArtisan.Internal;

namespace SqlArtisan;

public abstract class CteSchemaBase(string name) : TableReference(name)
{
    private readonly string _name = name;

    public CommonTableExpression As(ISubquery subquery) => new(_name, subquery);
}
