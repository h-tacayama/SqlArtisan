using SqlArtisan.Internal;

namespace SqlArtisan;

/// <summary>
/// Names a derived table inline — a <c>CROSS APPLY</c> / <c>LATERAL</c> source
/// (or any <c>FROM</c> / <c>JOIN</c> relation) — without declaring a dedicated
/// schema class, and renders as that bare name. Its columns are referenced by
/// name through <see cref="Column"/>.
/// </summary>
public sealed class DerivedTable : TableReference
{
    private readonly string _name;

    public DerivedTable(string name) : base(name)
    {
        _name = name;
    }

    /// <summary>Returns the named column of this derived table, qualified by its alias.</summary>
    public DbColumn Column(string columnName) => new(_name, columnName);
}
