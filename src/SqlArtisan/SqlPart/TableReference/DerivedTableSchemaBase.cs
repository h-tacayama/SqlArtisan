using SqlArtisan.Internal;

namespace SqlArtisan;

/// <summary>
/// Base for a schema that names a subquery-backed relation — a CTE or a
/// <c>CROSS APPLY</c> / <c>LATERAL</c> derived table — and renders as that bare
/// name in <c>FROM</c> / <c>JOIN</c> positions. Subclass it to expose the
/// relation's columns as typed <see cref="DbColumn"/> members; for an untyped,
/// ad-hoc handle use the concrete <see cref="DerivedTable"/> / <see cref="Cte"/>,
/// which add a <c>Column(name)</c> accessor instead.
/// </summary>
public abstract class DerivedTableSchemaBase : TableReference
{
    public DerivedTableSchemaBase(string name) : base(name)
    {
        SchemaName = name;
    }

    protected string SchemaName { get; }
}
