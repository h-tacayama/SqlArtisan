namespace SqlArtisan.Internal;

// Shared, mutable shape state for a joined UPDATE/DELETE: the builder sets these
// flags as From/USING/JOIN steps are appended, and the clauses read them at
// Build() — SQL Server calls .Set() before .From(t), so the shape isn't final
// until then.
internal sealed class DmlJoinState
{
    internal bool HasFrom { get; set; }

    internal bool HasJoin { get; set; }

    internal bool HasUsing { get; set; }

    // The target table instance was re-listed in a joined FROM — the SQL Server
    // (UPDATE) / SQL Server & MySQL (DELETE) spelling, where the alias comes from
    // FROM and the lead is the alias alone.
    internal bool TargetRepeatedInFrom { get; set; }

    internal bool IsJoined => HasFrom || HasJoin || HasUsing;

    // SQL Server / MySQL joined forms qualify the SET target (`SET t.col = ...`);
    // PostgreSQL's UPDATE ... FROM rejects a qualified target even when its FROM
    // carries a join between auxiliary tables (HasFrom without a repeated target).
    internal bool QualifiesSetTarget => TargetRepeatedInFrom || (HasJoin && !HasFrom);
}
