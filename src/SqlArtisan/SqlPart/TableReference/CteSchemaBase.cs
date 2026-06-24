using SqlArtisan.Internal;

namespace SqlArtisan;

public abstract class CteSchemaBase : TableReference
{
    public CteSchemaBase(string name) : base(name)
    {
        SchemaName = name;
    }

    protected string SchemaName { get; }

    public CommonTableExpression As(ISubquery subquery) => new(SchemaName, subquery);
}
