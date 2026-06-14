// ─────────────────────────────────────────────────────────────────────────────
// SPIKE / ILLUSTRATIVE ONLY — not compiled, not wired in. Sketches how DBMS
// "affinity" tagging closes the one hole that per-namespace IntelliSense
// filtering cannot: reusing a node authored for DBMS A inside a Build for DBMS B
// (e.g. via a shared helper that returns a SqlExpression).
//
// Key feasibility insight from the spike: the validation needs NO new tree walk.
// SqlBuildingBuffer already visits every SqlPart during Format(), and it already
// holds the target dialect. One check inside Append(SqlPart) catches a foreign
// node at near-zero cost. This is the whole mechanism.
// ─────────────────────────────────────────────────────────────────────────────

using SqlArtisan;

namespace SqlArtisan.Spike.Prototype;

// (1) The base node gains one nullable tag. `null` = universal / no affinity.
//     Today's SqlExpression would get this property; the generated factories
//     pass the namespace's Dbms into each constructor (see ../generated/*.g.cs).
public abstract class SqlExpressionSketch
{
    /// <summary>The DBMS this node was authored for, or null if portable.</summary>
    internal Dbms? AuthoredFor { get; }

    protected SqlExpressionSketch(Dbms? authoredFor) => AuthoredFor = authoredFor;
}

// (2) The buffer already knows its dialect — give the dialect a Dbms, then check
//     affinity exactly where parts are already being appended. No extra traversal.
internal sealed class SqlBuildingBufferSketch
{
    private readonly Dbms _target;

    internal SqlBuildingBufferSketch(Dbms target) => _target = target;

    // This mirrors the real SqlBuildingBuffer.Append(SqlPart): the single line
    // marked ★ is the entire runtime cost of the guarantee.
    internal SqlBuildingBufferSketch Append(SqlExpressionSketch part)
    {
        DbmsAffinity.EnsureCompatible(part, _target); // ★ one comparison per node
        // ... existing: part.Format(this) ...
        return this;
    }
}

internal static class DbmsAffinity
{
    internal static void EnsureCompatible(SqlExpressionSketch part, Dbms target)
    {
        // A portable node (AuthoredFor == null) is fine anywhere. A tagged node
        // must match the build target, otherwise the user mixed namespaces.
        if (part.AuthoredFor is Dbms authored && authored != target)
        {
            throw new InvalidOperationException(
                $"This expression was authored with SqlArtisan.{authored} " +
                $"(e.g. via `using SqlArtisan.{authored};`) but the query is " +
                $"being built for {target}. Build the query with the namespace " +
                $"that matches its DBMS, or rebuild the expression for {target}.");
        }
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// What this DOES catch:
//   • A SqlArtisan.SqlServer.Sql.Ceiling(...) node passed into an Oracle Build.
//
// What this does NOT catch (out of scope by design — see README §1):
//   • Semantic divergence behind an identical signature (ROUND half-even vs
//     half-away-from-zero; PG double-precision vs numeric two-arg ROUND).
//
// Why not phantom types (SqlExpression<TDbms>)? They would make this a COMPILE-
// time error instead of a Build-time throw, but the generic parameter becomes
// viral across the entire fluent API (every operator, every clause, every
// helper signature). The ergonomic cost is judged too high; the build-time
// check buys ~the same safety for a fraction of the friction.
// ─────────────────────────────────────────────────────────────────────────────
