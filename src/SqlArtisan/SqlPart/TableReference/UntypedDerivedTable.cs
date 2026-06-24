using SqlArtisan.Internal;

namespace SqlArtisan;

/// <summary>
/// An untyped derived-table handle: names a <c>CROSS APPLY</c> / <c>LATERAL</c>
/// source (or any <c>FROM</c> / <c>JOIN</c> relation) without declaring a typed
/// schema class, and renders as that bare name. Its columns are referenced by
/// name through <see cref="Column"/> rather than as statically-typed members.
/// </summary>
public sealed class UntypedDerivedTable : TableReference
{
    private readonly string _name;

    public UntypedDerivedTable(string name) : base(name)
    {
        _name = name;
    }

    /// <summary>Returns the named column of this derived table, qualified by its alias.</summary>
    public DbColumn Column(string columnName) => new(_name, columnName);
}
