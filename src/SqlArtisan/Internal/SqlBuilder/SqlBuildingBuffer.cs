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
    // Correlated-DML guard state (#253): a bare target column rendered inside a
    // subquery resolves to the inner scope — a silent tautology — so
    // DbColumn's Format fails loudly instead. Holds an aliased target too (#595).
    private TableReference? _correlatedDmlTarget;
    private int _subqueryDepth;
    // Fits the padding beside _disposed, so it costs no build an allocation.
    private bool _qualifyReturningByTableName;
    // Inside a CTE body the guard defers to each query block's end: a target
    // column is legitimate only where its own block lists the target (#607).
    // A byte flags enum, so it shares that padding.
    private CteGuard _cteGuard;

    internal SqlBuildingBuffer(Dbms dbms)
    {
        Dbms = dbms;
        _dialect = DbmsDialectFactory.Create(dbms);
        _buffer = ArrayPool<char>.Shared.Rent(InitialCapacity);
        _position = 0;
    }

    // For the nested Validate(Dbms) hook alone (SqlBuilderBase.FormatCore) — a
    // SqlPart reads the dialect's own members, never this value (ADR 0002).
    internal Dbms Dbms { get; }

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

    internal SqlBuildingBuffer AppendCube(SqlPart[] items) =>
        Append(Keywords.Cube)
            .OpenParenthesis()
            .AppendCsv(items)
            .CloseParenthesis();

    internal SqlBuildingBuffer AppendGroupingSets(SqlPart[] sets) =>
        Append($"{Keywords.Grouping} {Keywords.Sets}")
            .OpenParenthesis()
            .AppendCsv(sets)
            .CloseParenthesis();

    // Renders a comma-separated list of assignments (`a = 1, b = 2`) with each
    // target column unqualified, for the positions that forbid a qualifier.
    internal SqlBuildingBuffer AppendAssignmentsCsv(EqualCondition[] assignments)
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

    // Renders a comma-separated list of bare column names, for the positions
    // that forbid a table-alias qualifier. DbColumn[] binds here via array
    // covariance.
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

    // Appends a DML table alias to a target already written: the dialect's
    // separator, then the quoted alias. Whether AS appears (Oracle rejects it
    // on table aliases) is a dialect token (ADR 0002).
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

    // Emits a single-quote-delimited string literal for a position whose grammar
    // takes a constant (ADR 0004): the LIKE ... ESCAPE char, GROUP_CONCAT ... SEPARATOR.
    internal SqlBuildingBuffer AppendStringLiteral(char value)
    {
        Append('\'');
        AppendEscaped(value);
        Append('\'');
        return this;
    }

    internal SqlBuildingBuffer AppendStringLiteral(string value)
    {
        Append('\'');

        foreach (char c in value)
        {
            AppendEscaped(c);
        }

        Append('\'');
        return this;
    }

    // Doubles the single quote on every dialect; doubles the backslash only where
    // the dialect treats it as a string-literal escape (MySQL).
    private void AppendEscaped(char value)
    {
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
    }

    internal SqlBuildingBuffer EncloseInParentheses(SqlPart part)
    {
        Append('(');
        part.Format(this);
        Append(')');
        return this;
    }

    // ISubquery marks a builder state, not a SqlPart, so it gets its own overload
    // rather than an adapter allocation. Every embedding but a CTE body funnels
    // through here — the correlated-DML guard's boundary (#253).
    internal SqlBuildingBuffer EncloseInParentheses(ISubquery subquery)
    {
        // An INSERT ... SELECT chain is an ISubquery through its SELECT stages, but
        // no dialect takes an INSERT here.
        if (subquery is InsertBuilder)
        {
            throw new ArgumentException(
                "An INSERT statement cannot be embedded as a subquery; embed its SELECT instead.");
        }

        TableReference? target = _correlatedDmlTarget;
        if (HoldsReturningTarget && RebindsReturningTarget(subquery))
        {
            _correlatedDmlTarget = null;
        }

        // Inside a CTE body a subquery is its own block for the deferred guard.
        CteGuard outer = _cteGuard;
        _cteGuard &= CteGuard.InBody;

        Append('(');
        _subqueryDepth++;
        subquery.Format(this);
        ThrowIfBlockCorrelates();
        _subqueryDepth--;
        Append(')');
        _correlatedDmlTarget = target;
        _cteGuard = outer;
        return this;
    }

    internal void SetCorrelatedDmlGuardTarget(DbTableBase? target) =>
        _correlatedDmlTarget = target;

    internal SqlBuildingBuffer AppendReturningItems(SqlPart[] items)
    {
        _qualifyReturningByTableName = _dialect.ReturningIgnoresTargetAlias;
        AppendSelectItems(items);
        _qualifyReturningByTableName = false;
        return this;
    }

    private bool HoldsReturningTarget =>
        _qualifyReturningByTableName && _correlatedDmlTarget is DbTableBase { HasAlias: true };

    // A scope exposing the target's table name would capture `users.id`; one
    // exposing its alias owns the `"u".id` the caller wrote, which a swap would
    // send to the target. Either keeps the alias; set-operator blocks share it.
    private bool RebindsReturningTarget(ISubquery subquery) =>
        subquery is not SelectBuilder select
        || select.BindsAnyRelation((DbTableBase)_correlatedDmlTarget!);

    // SQLite 3.50.4 resolves the target in RETURNING by table name alone; a
    // subquery or CTE body that rebinds the name or the alias clears the slot.
    internal bool QualifiesByTableName(TableReference owner) =>
        _qualifyReturningByTableName
        && ReferenceEquals(owner, _correlatedDmlTarget)
        && owner is DbTableBase { HasAlias: true };

    // Matched by name, not instance: the capture comes from any relation so
    // named, the target's own instance or another (#595).
    internal static bool BindsName(TableReference relation, DbTableBase target)
    {
        ReadOnlySpan<char> exposed = relation.ExposedName;
        return SameIdentifier(exposed, target.ExposedName)
            || SameIdentifier(exposed, target.NameWithoutSchema);
    }

    // SQLite folds identifier case and ignores quoting when it resolves a name,
    // so both are dropped; matching too much only keeps the alias.
    private static bool SameIdentifier(ReadOnlySpan<char> a, ReadOnlySpan<char> b) =>
        Unquote(a).Equals(Unquote(b), StringComparison.OrdinalIgnoreCase);

    private static ReadOnlySpan<char> Unquote(ReadOnlySpan<char> name) =>
        name.Length >= 2
            && ((name[0] is '"' or '`' && name[^1] == name[0])
                || (name[0] == '[' && name[^1] == ']'))
            ? name[1..^1]
            : name;

    // A body may list the target (#253), so the guard defers to each block's end;
    // elsewhere a bare column can bind the body's same-named one, top level too
    // (#607). An aliased RETURNING target stays unless the body rebinds it (#595).
    internal void FormatCteBody(ISubquery body)
    {
        TableReference? target = _correlatedDmlTarget;
        bool guardsTarget = target is DbTableBase { HasAlias: false };
        if (!guardsTarget && (!HoldsReturningTarget || RebindsReturningTarget(body)))
        {
            _correlatedDmlTarget = null;
        }

        CteGuard outer = _cteGuard;
        _cteGuard = guardsTarget ? CteGuard.InBody : 0;

        try
        {
            body.Format(this);
            ThrowIfBlockCorrelates();
        }
        finally
        {
            _correlatedDmlTarget = target;
            _cteGuard = outer;
        }
    }

    internal void ThrowIfCorrelatedDmlColumn(TableReference owner)
    {
        if (!ReferenceEquals(owner, _correlatedDmlTarget))
        {
            return;
        }

        if ((_cteGuard & CteGuard.InBody) != 0)
        {
            _cteGuard |= CteGuard.TargetColumnSeen;
        }
        else if (_subqueryDepth > 0)
        {
            DmlTargetGuard.ThrowCorrelatedUnaliasedTarget();
        }
    }

    // A block listing the guarded target reads it as its own relation (#253).
    internal void NoteRelation(TableReference relation)
    {
        if ((_cteGuard & CteGuard.InBody) != 0 && ReferenceEquals(relation, _correlatedDmlTarget))
        {
            _cteGuard |= CteGuard.TargetListed;
        }
    }

    // Each set-operator branch is its own block (#611).
    internal SqlBuildingBuffer EndSetOperatorBranch()
    {
        ThrowIfBlockCorrelates();
        _cteGuard &= CteGuard.InBody;
        return this;
    }

    // A compound's trailing ORDER BY names its result columns, so a bare target
    // column there reaches no outer scope: the last branch is checked first, and
    // what the ORDER BY reads is dropped after it.
    internal SqlBuildingBuffer BeginCompoundOrderBy() => EndSetOperatorBranch();

    internal void EndCompoundOrderBy() => _cteGuard &= CteGuard.InBody;

    // Checked once a block is fully rendered, since its FROM follows its SELECT
    // list. A listing elsewhere does not count: an enclosing block's relation
    // can be shadowed by one in between, which is #253's tautology again.
    private void ThrowIfBlockCorrelates()
    {
        if (_cteGuard == (CteGuard.InBody | CteGuard.TargetColumnSeen))
        {
            DmlTargetGuard.ThrowCorrelatedUnaliasedTarget();
        }
    }

    [Flags]
    private enum CteGuard : byte
    {
        InBody = 1,
        TargetColumnSeen = 2,
        TargetListed = 4,
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

    internal SqlBuildingBuffer PrependSpaceIfNotNull(string? value)
    {
        if (value is not null)
        {
            AppendSpace();
            Append(value);
        }

        return this;
    }

    internal SqlBuildingBuffer AddParameter(BindValue bindValue)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        // The same instance formatted twice (SELECT and GROUP BY) reuses its marker:
        // engines matching GROUP BY syntactically reject distinct markers (#241).
        if (_parameters is not null)
        {
            // Linear by design (ADR 0006's best effort): 37 ms at 2,100 binds, and
            // the driver's own parameter cap bounds the scan.
            foreach (KeyValuePair<string, BindValue> parameter in _parameters)
            {
                if (ReferenceEquals(parameter.Value, bindValue))
                {
                    Append(parameter.Key);
                    return this;
                }
            }
        }

        _parameters ??= new();
        string name = ParameterNameCache.Get(_dialect.ParameterMarker, _parameters.Count);
        Append(name);
        _parameters.Add(new(name, bindValue));
        return this;
    }

    internal SqlBuildingBuffer AddOutParameter(OutputParameter output)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        // default(OutputParameter) bypasses the constructor guard (a struct cannot
        // suppress its default instance); Into rejects it and a duplicate first (#569).
        if (string.IsNullOrEmpty(output.Variable))
        {
            throw new ArgumentException("An output variable name is required.");
        }

        _parameters ??= new();
        string name = $"{_dialect.ParameterMarker}{output.Variable}";

        if (ContainsParameterName(name))
        {
            throw new ArgumentException(
                "A RETURNING INTO clause requires a distinct name for every variable; "
                    + $"'{output.Variable}' is duplicated.");
        }

        Append(name);
        _parameters.Add(new(name, new BindValue(
            DBNull.Value,
            dbType: output.DbType,
            direction: ParameterDirection.Output,
            size: output.Size)));
        return this;
    }

    internal SqlStatement ToSqlStatement()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

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
    internal SqlBuildingBuffer Append(ReadOnlySpan<char> value)
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
