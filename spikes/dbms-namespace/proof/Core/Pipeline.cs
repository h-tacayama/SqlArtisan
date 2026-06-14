// Minimal but FAITHFUL mirror of the real SqlArtisan pipeline, just large enough
// to validate ③ end to end. Maps 1:1 onto src/SqlArtisan:
//
//   this proof                     ->  real SqlArtisan
//   ─────────────────────────────────────────────────────────────────
//   SqlExpression.Format(buffer)   ->  SqlExpression.Format(SqlBuildingBuffer)
//   SqlBuildingBuffer + IDbmsDialect -> same names
//   IDbmsDialect.Dbms (NEW)        ->  the one field ③ would add
//   SqlExpression.AuthoredFor (NEW)->  the one field ③ would add
//   buffer.Append(part) affinity check -> the one line ③ would add
//
// The two NEW members and the one NEW line are the entire production-side
// footprint of the tag-validation mechanism.

namespace SqlArtisan.Proof;

public enum Dbms
{
    Oracle,
    PostgreSql,
    SqlServer,
}

// ── dialect: already exists in the real lib; ③ adds the Dbms property ─────────
public interface IDbmsDialect
{
    Dbms Dbms { get; }
    char AliasQuote { get; }
}

public sealed class OracleDialect : IDbmsDialect
{
    public Dbms Dbms => Dbms.Oracle;
    public char AliasQuote => '"';
}

public sealed class PostgreSqlDialect : IDbmsDialect
{
    public Dbms Dbms => Dbms.PostgreSql;
    public char AliasQuote => '"';
}

public sealed class SqlServerDialect : IDbmsDialect
{
    public Dbms Dbms => Dbms.SqlServer;
    public char AliasQuote => '"';
}

internal static class Dialects
{
    internal static IDbmsDialect For(Dbms dbms) => dbms switch
    {
        Dbms.Oracle => new OracleDialect(),
        Dbms.PostgreSql => new PostgreSqlDialect(),
        Dbms.SqlServer => new SqlServerDialect(),
        _ => throw new ArgumentOutOfRangeException(nameof(dbms)),
    };
}

// ── the base node: ③ adds exactly the nullable AuthoredFor tag ────────────────
public abstract class SqlExpression
{
    /// <summary>The DBMS this node was authored for, or null if portable
    /// (e.g. a column or literal, which is valid on every DBMS).</summary>
    internal Dbms? AuthoredFor { get; }

    private protected SqlExpression(Dbms? authoredFor) => AuthoredFor = authoredFor;

    internal abstract void Format(SqlBuildingBuffer buffer);
}

// ── the buffer: the affinity check rides on the traversal that already exists ─
public sealed class SqlBuildingBuffer
{
    private readonly IDbmsDialect _dialect;
    private readonly System.Text.StringBuilder _sb = new();

    internal SqlBuildingBuffer(IDbmsDialect dialect) => _dialect = dialect;

    internal SqlBuildingBuffer Append(string token)
    {
        _sb.Append(token);
        return this;
    }

    internal SqlBuildingBuffer OpenParenthesis() => Append("(");
    internal SqlBuildingBuffer CloseParenthesis() => Append(")");

    internal SqlBuildingBuffer Append(SqlExpression part)
    {
        // ★ The ENTIRE runtime cost of the build-time mixing guarantee: one
        //   comparison, on the traversal Format already performs. No extra walk.
        if (part.AuthoredFor is Dbms authored && authored != _dialect.Dbms)
        {
            throw new InvalidOperationException(
                $"This expression was authored with SqlArtisan.{authored} but the " +
                $"query is being built for {_dialect.Dbms}. Build it through the " +
                $"namespace that matches its DBMS, or rebuild the expression for " +
                $"{_dialect.Dbms}.");
        }

        part.Format(this);
        return this;
    }

    public override string ToString() => _sb.ToString();
}

// ── a portable leaf (column): AuthoredFor == null, valid anywhere ─────────────
public sealed class Column : SqlExpression
{
    private readonly string _name;
    internal Column(string name) : base(authoredFor: null) => _name = name;
    internal override void Format(SqlBuildingBuffer buffer) => buffer.Append(_name);
}

// ── the entry point: a Select bound to ONE Dbms (folded in by the namespace) ──
// This is the "remove Build(Dbms)" part of ③: the DBMS comes from where you
// started the query, so Build() takes no argument.
public sealed class SelectBuilder
{
    private readonly SqlExpression _projection;
    private readonly Dbms _dbms;

    internal SelectBuilder(SqlExpression projection, Dbms dbms)
    {
        _projection = projection;
        _dbms = dbms;
    }

    public string Build()
    {
        SqlBuildingBuffer buffer = new(Dialects.For(_dbms));
        buffer.Append("SELECT ").Append(_projection);
        return buffer.ToString();
    }
}
