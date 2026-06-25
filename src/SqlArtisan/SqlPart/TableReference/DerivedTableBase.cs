using SqlArtisan.Internal;

namespace SqlArtisan;

/// <summary>
/// Base for a derived table joined with <c>CROSS APPLY</c> / <c>LATERAL</c>.
/// Subclass it to expose the derived table's columns as typed
/// <see cref="DbColumn"/> members; for a one-off, use the inline
/// <see cref="DerivedTable"/> instead.
/// </summary>
public abstract class DerivedTableBase : TableReference
{
    public DerivedTableBase(string name) : base(name)
    {
        Alias = name;
    }

    protected string Alias { get; }
}
