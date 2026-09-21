using SqlArtisan.Internal;

namespace SqlArtisan;

/// <summary>
/// Base for a derived table — a subquery, <c>VALUES</c> list, or <c>UNNEST</c>
/// call used as a relation in <c>FROM</c> or a lateral / <c>APPLY</c> join.
/// Subclass it to expose the derived table's columns as typed
/// <see cref="DbColumn"/> members; for a one-off, use the inline
/// <see cref="DerivedTable"/> instead.
/// </summary>
public abstract class DerivedTableBase(string name)
    : TableReference(name, "A derived table requires an alias.")
{
    internal override string CorrelationName => _name;

    // The alias is quoted at its definition site (`... ) "x"`) to match how a
    // reference to it renders (`"x".col`). See TableReference.QuoteName.
    private protected override bool QuoteName => true;
}
