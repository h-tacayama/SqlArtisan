using System.Buffers;
using System.Data;
using System.Runtime.CompilerServices;

namespace SqlArtisan.Internal;

internal sealed class SqlBuildingBuffer : IDisposable
{
    private const int InitialCapacity = 2048;

    // Shared, never-mutated instance handed to parameterless statements so they
    // don't each allocate a list. SqlParameters only ever reads it.
    private static readonly List<KeyValuePair<string, BindValue>> s_emptyParameters = new();

    private readonly IDbmsDialect _dialect;
    private char[] _buffer;
    private int _position;
    // Allocated lazily on the first parameter; parameterless statements keep this null.
    // A list (insertion-ordered) is used over a dictionary: parameter counts are
    // small, so linear lookup is cheap and it allocates less than a hash table.
    private List<KeyValuePair<string, BindValue>>? _parameters;
    private bool _disposed;

    internal SqlBuildingBuffer(IDbmsDialect dialect)
    {
        _dialect = dialect;
        _buffer = ArrayPool<char>.Shared.Rent(InitialCapacity);
        _position = 0;
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        if (_buffer != null)
        {
            ArrayPool<char>.Shared.Return(_buffer);
            _buffer = null!;
        }

        _parameters = null;
        _disposed = true;
    }

    internal SqlBuildingBuffer Append(SqlPart part)
    {
        part.Format(this);
        return this;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal SqlBuildingBuffer Append(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return this;
        }

        Append(value.AsSpan());
        return this;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal SqlBuildingBuffer Append(char value)
    {
        EnsureCapacity(1);
        _buffer[_position++] = value;
        return this;
    }

    internal SqlBuildingBuffer AppendCsv(SqlPart[] parts)
    {
        if (parts.Length == 0)
        {
            return this;
        }

        parts[0].Format(this);

        for (int i = 1; i < parts.Length; i++)
        {
            Append(", ");
            parts[i].Format(this);
        }

        return this;
    }

    // Renders a GROUP BY ROLLUP grouping `ROLLUP(a, b)` — the standard function
    // form — on every dialect. MySQL accepts only its `... WITH ROLLUP` suffix,
    // which is a separate construct (`.GroupBy(...).WithRollup()`); availability is
    // the analyzer's concern, not Build's (ADR 0001: never silently rewrite the
    // author's SQL into a different construct).
    internal SqlBuildingBuffer AppendRollup(SqlPart[] items) =>
        Append(Keywords.Rollup)
            .OpenParenthesis()
            .AppendCsv(items)
            .CloseParenthesis();

    // Renders a GROUP BY CUBE grouping `CUBE(a, b)`. Emitted faithfully on every
    // dialect; where CUBE is unavailable (MySQL, SQLite) that is the analyzer's
    // concern (ADR 0003), not Build's.
    internal SqlBuildingBuffer AppendCube(SqlPart[] items) =>
        Append(Keywords.Cube)
            .OpenParenthesis()
            .AppendCsv(items)
            .CloseParenthesis();

    // Renders a GROUP BY GROUPING SETS grouping `GROUPING SETS((a, b), c, ())`.
    // Emitted faithfully on every dialect; where GROUPING SETS is unavailable
    // (MySQL, SQLite) that is the analyzer's concern (ADR 0003), not Build's.
    internal SqlBuildingBuffer AppendGroupingSets(SqlPart[] sets) =>
        Append(Keywords.GroupingSets)
            .OpenParenthesis()
            .AppendCsv(sets)
            .CloseParenthesis();

    // Renders a comma-separated list of assignments (`a = 1, b = 2`) with each
    // target column unqualified. Used by SET / DO UPDATE SET / ON DUPLICATE KEY
    // UPDATE, where the left side must not carry a table-alias qualifier.
    internal SqlBuildingBuffer AppendAssignmentsCsv(EqualityCondition[] assignments)
    {
        if (assignments.Length == 0)
        {
            return this;
        }

        assignments[0].FormatAsAssignment(this);

        for (int i = 1; i < assignments.Length; i++)
        {
            Append(", ");
            assignments[i].FormatAsAssignment(this);
        }

        return this;
    }

    // Renders a comma-separated list of column names with no table-alias
    // qualifier — the INSERT column list and the ON CONFLICT target. DbColumn[]
    // binds here via array covariance.
    internal SqlBuildingBuffer AppendUnqualifiedColumnsCsv(SqlExpression[] columns)
    {
        if (columns.Length == 0)
        {
            return this;
        }

        AppendUnqualifiedColumn(columns[0]);

        for (int i = 1; i < columns.Length; i++)
        {
            Append(", ");
            AppendUnqualifiedColumn(columns[i]);
        }

        return this;
    }

    internal SqlBuildingBuffer AppendIf(bool when, string? value)
    {
        if (when)
        {
            Append(value);
        }

        return this;
    }

    internal SqlBuildingBuffer AppendSelectItems(SqlPart[] selectItems)
    {
        if (selectItems.Length == 0)
        {
            return this;
        }

        AppendSelectItem(selectItems[0]);

        for (int i = 1; i < selectItems.Length; i++)
        {
            Append(", ");
            AppendSelectItem(selectItems[i]);
        }

        return this;
    }

    internal SqlBuildingBuffer AppendSpace()
    {
        Append(' ');
        return this;
    }

    internal SqlBuildingBuffer AppendSpace(SqlPart part)
    {
        part.Format(this);
        AppendSpace();
        return this;
    }

    internal SqlBuildingBuffer AppendSpaceIfNotNull(SqlPart? part)
    {
        if (part is not null)
        {
            part.Format(this);
            AppendSpace();
        }

        return this;
    }

    internal SqlBuildingBuffer AppendSpaceSeparated(ReadOnlySpan<SqlPart> parts)
    {
        if (parts.Length == 0)
        {
            return this;
        }

        parts[0].Format(this);

        for (int i = 1; i < parts.Length; i++)
        {
            AppendSpace();
            parts[i].Format(this);
        }

        return this;
    }

    internal SqlBuildingBuffer CloseParenthesis(SqlPart? part = null)
    {
        part?.Format(this);
        Append(')');
        return this;
    }

    internal SqlBuildingBuffer AppendExcludedName()
    {
        Append(_dialect.ExcludedName);
        return this;
    }

    // Appends the dialect's MERGE terminator directly (no leading space), so a
    // SQL Server MERGE ends in `...;` rather than `... ;`. Other dialects supply
    // an empty terminator, making this a no-op.
    internal SqlBuildingBuffer AppendMergeTerminator()
    {
        Append(_dialect.MergeTerminator);
        return this;
    }

    // Appends a DML table alias to a target already written, e.g. ` AS "x"`
    // (PostgreSQL/SQLite/MySQL/SQL Server) or ` "x"` (Oracle, which rejects AS on
    // table aliases). The presence of AS is a dialect token (ADR 0002).
    internal SqlBuildingBuffer AppendDmlTableAlias(string alias)
    {
        Append(_dialect.DmlTableAliasSeparator);
        EncloseInAliasQuotes(alias);
        return this;
    }

    internal SqlBuildingBuffer EncloseInAliasQuotes(string value)
    {
        Append(_dialect.AliasQuote);
        Append(value);
        Append(_dialect.AliasQuote);
        return this;
    }

    // Emits a single-character string literal (e.g. the LIKE ... ESCAPE char),
    // single-quote delimited. A position whose grammar wants a literal rather than
    // a bind parameter (ADR 0004) — MySQL rejects `ESCAPE ?` as a non-constant. The
    // single quote is doubled on every dialect; the backslash is doubled only where
    // the dialect treats it as a string-literal escape (MySQL).
    internal SqlBuildingBuffer AppendStringLiteral(char value)
    {
        Append('\'');

        if (value == '\'')
        {
            Append("''");
        }
        else if (value == '\\' && _dialect.BackslashEscapesStringLiterals)
        {
            Append("\\\\");
        }
        else
        {
            Append(value);
        }

        Append('\'');
        return this;
    }

    internal SqlBuildingBuffer EncloseInParentheses(SqlPart part)
    {
        Append('(');
        part.Format(this);
        Append(')');
        return this;
    }

    internal SqlBuildingBuffer EncloseInSpaces(string value)
    {
        Append(' ');
        Append(value);
        Append(' ');
        return this;
    }

    internal SqlBuildingBuffer OpenParenthesis(SqlPart? part = null)
    {
        Append('(');
        part?.Format(this);
        return this;
    }

    internal SqlBuildingBuffer PrependComma(SqlPart part)
    {
        Append(", ");
        part.Format(this);
        return this;
    }

    internal SqlBuildingBuffer PrependComma(string value)
    {
        Append(", ");
        Append(value);
        return this;
    }

    internal SqlBuildingBuffer PrependCommaIfNotNull(SqlPart? part)
    {
        if (part is not null)
        {
            Append(", ");
            part.Format(this);
        }

        return this;
    }

    internal SqlBuildingBuffer PrependCommaIfNotNull(string? value)
    {
        if (value is not null)
        {
            Append(", ");
            Append(value);
        }

        return this;
    }

    internal SqlBuildingBuffer PrependSpace(SqlPart part)
    {
        AppendSpace();
        part.Format(this);
        return this;
    }

    internal SqlBuildingBuffer PrependSpaceIfNotNull(SqlPart? part)
    {
        if (part is not null)
        {
            AppendSpace();
            part.Format(this);
        }

        return this;
    }

    internal SqlBuildingBuffer AddParameter(BindValue bindValue)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        _parameters ??= new();
        string name = ParameterNameCache.Get(_dialect.ParameterMarker, _parameters.Count);
        Append(name);
        _parameters.Add(new(name, bindValue));
        return this;
    }

    internal SqlBuildingBuffer AddOutParameter(string variable)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        _parameters ??= new();
        string name = $"{_dialect.ParameterMarker}{variable}";

        if (ContainsParameterName(name))
        {
            throw new ArgumentException(
                $"Duplicate variable name '{variable}' in RETURNING INTO clause. Each variable name must be unique.");
        }

        Append(name);
        _parameters.Add(new(name, new BindValue(
            DBNull.Value,
            direction: ParameterDirection.Output)));
        return this;
    }

    internal SqlStatement ToSqlStatement()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        // Transfer ownership of the parameter dictionary to the SqlStatement.
        // The buffer relinquishes its reference so the caller's instance is
        // never mutated after this point (Dispose only returns the char buffer).
        string sql = new(_buffer, 0, _position);
        List<KeyValuePair<string, BindValue>> parameters = _parameters ?? s_emptyParameters;
        _parameters = null;
        return new(sql, parameters);
    }

    private bool ContainsParameterName(string name)
    {
        if (_parameters is null)
        {
            return false;
        }

        for (int i = 0; i < _parameters.Count; i++)
        {
            if (_parameters[i].Key == name)
            {
                return true;
            }
        }

        return false;
    }

    // A DbColumn is rendered as its bare name, dropping any table-alias
    // qualifier it carries (column-name positions forbid qualification); any
    // other expression formats normally.
    private void AppendUnqualifiedColumn(SqlExpression column)
    {
        if (column is DbColumn dbColumn)
        {
            dbColumn.FormatUnqualified(this);
        }
        else
        {
            column.Format(this);
        }
    }

    private void AppendSelectItem(SqlPart selectItem)
    {
        if (selectItem is ExpressionAlias alias)
        {
            alias.FormatAsSelect(this);
        }
        else
        {
            selectItem.Format(this);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private SqlBuildingBuffer Append(ReadOnlySpan<char> value)
    {
        if (value.IsEmpty)
        {
            return this;
        }

        EnsureCapacity(value.Length);
        value.CopyTo(_buffer.AsSpan(_position));
        _position += value.Length;
        return this;
    }

    // No disposed check here: this runs on every append (the hottest path) and
    // the buffer's lifecycle is internal (built, finalized via ToSqlStatement,
    // then disposed). Disposal is still guarded at the entry points
    // (AddParameter / AddOutParameter / ToSqlStatement).
    // The common case (no growth) is a single bounds check so it inlines into
    // the Append callers; the rare growth is a separate, non-inlined method.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void EnsureCapacity(int additionalChars)
    {
        if (_position + additionalChars > _buffer.Length)
        {
            Grow(additionalChars);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void Grow(int additionalChars)
    {
        int requiredCapacity = _position + additionalChars;
        int newSize = Math.Max(_buffer.Length * 2, requiredCapacity);
        char[] newBuffer = ArrayPool<char>.Shared.Rent(newSize);
        _buffer.AsSpan(0, _position).CopyTo(newBuffer);
        ArrayPool<char>.Shared.Return(_buffer);
        _buffer = newBuffer;
    }
}
