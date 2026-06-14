namespace SqlArtisan.Internal;

// Verb-less builder surface for the EXTENSION-based ③ experiment. The UPSERT
// verbs (OnConflict / DoUpdateSet / DoNothing / OnDuplicateKeyUpdate) are NOT
// declared here on purpose: in C# an instance method shadows a same-named
// extension, so to let each DBMS namespace contribute its own verb as a
// namespace-scoped extension, these interfaces must stay empty.
public interface IExtUpsertColumns
{
    IExtUpsertValues Values(params object[] values);
}

public interface IExtUpsertValues
{
}

public interface IExtConflictAction
{
}

// The single, DBMS-agnostic wrapper shared by every namespace — the entire
// structural cost of the extension approach. (Compare the wrapper approach, which
// needed one type per fluent state per DBMS.) It just forwards to the real
// InsertBuilder, which the extension methods reach via Inner.
internal sealed class ExtUpsertBuilder : IExtUpsertColumns, IExtUpsertValues, IExtConflictAction
{
    internal InsertBuilder Inner { get; }

    internal ExtUpsertBuilder(InsertBuilder inner) => Inner = inner;

    public IExtUpsertValues Values(params object[] values)
    {
        Inner.Values(values);
        return this;
    }
}
