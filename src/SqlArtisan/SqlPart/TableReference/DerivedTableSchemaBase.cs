using SqlArtisan.Internal;

namespace SqlArtisan;

/// <summary>
/// Base for a user-defined schema that names a subquery-backed relation and
/// exposes its columns as typed <see cref="DbColumn"/> members. Subclass it to
/// give a derived table (a <c>CROSS APPLY</c> / <c>LATERAL</c> source, or — via
/// <see cref="CteSchemaBase"/> — a CTE) a single source of truth for its alias
/// and columns, instead of repeating an alias string at every reference.
/// </summary>
public abstract class DerivedTableSchemaBase : TableReference
{
    public DerivedTableSchemaBase(string name) : base(name)
    {
    }
}
