using SqlArtisan.Internal;

namespace SqlArtisan.Typed;

// ─────────────────────────────────────────────────────────────────────────────
// Phantom-type decision experiment: the DBMS is a *type parameter*, not a
// namespace. This is the "safety ceiling" — it can make cross-DBMS mixing a
// COMPILE-TIME error (the one thing namespaces/affinity-guard only catch at
// runtime), AND it keeps a SINGLE namespace/facade (the "earlier form" the
// maintainer wants to keep on the table). The cost being measured is ergonomic:
// pervasive generic type arguments, a capability-interface matrix, and
// portability friction.
//
// Marker types (pure phantoms — never instantiated). Each declares which
// divergent functions it supports via capability interfaces.
// ─────────────────────────────────────────────────────────────────────────────
public interface ISupportsCeil { }

public interface ISupportsCeiling { }

public abstract class Oracle : ISupportsCeil
{
    private Oracle() { }
}

public abstract class SqlServer : ISupportsCeiling
{
    private SqlServer() { }
}

public abstract class PostgreSql : ISupportsCeil, ISupportsCeiling
{
    private PostgreSql() { }
}

// Maps a phantom marker type to the runtime Dbms enum.
internal static class DbmsTag
{
    internal static Dbms Of<TDbms>() =>
        typeof(TDbms) == typeof(Oracle) ? Dbms.Oracle
        : typeof(TDbms) == typeof(SqlServer) ? Dbms.SqlServer
        : typeof(TDbms) == typeof(PostgreSql) ? Dbms.PostgreSql
        : throw new NotSupportedException($"Unknown DBMS marker: {typeof(TDbms).Name}");
}

// A SQL expression tagged at the type level with the DBMS it belongs to. The
// phantom TDbms carries no data — it only constrains what can be combined.
public readonly struct Expr<TDbms>
{
    internal SqlExpression Node { get; }

    internal Expr(SqlExpression node) => Node = node;
}

// A query whose DBMS is fixed by TDbms, so Build() takes no argument.
public sealed class TypedQuery<TDbms>
{
    private readonly ISqlBuilder _inner;

    internal TypedQuery(ISqlBuilder inner) => _inner = inner;

    public SqlStatement Build() => _inner.Build(DbmsTag.Of<TDbms>());
}
