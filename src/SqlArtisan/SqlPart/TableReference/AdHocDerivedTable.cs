using SqlArtisan.Internal;

namespace SqlArtisan;

/// <summary>
/// An ad-hoc derived-table handle: names a <c>CROSS APPLY</c> / <c>LATERAL</c>
/// source (or any <c>FROM</c> / <c>JOIN</c> relation) inline, without declaring a
/// dedicated schema class, and renders as that bare name. Reference its columns
/// ad-hoc with <see cref="Column"/>.
/// </summary>
public sealed class AdHocDerivedTable : TableReference
{
    private readonly string _name;

    public AdHocDerivedTable(string name) : base(name)
    {
        _name = name;
    }

    /// <summary>Returns the named column of this derived table, qualified by its alias.</summary>
    public DbColumn Column(string columnName) => new(_name, columnName);
}
