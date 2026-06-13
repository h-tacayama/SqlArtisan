using System.Buffers;
using System.Data;

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

    internal SqlBuildingBuffer Append(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return this;
        }

        Append(value.AsSpan());
        return this;
    }

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

    internal SqlBuildingBuffer AppendSpaceSeparated(IList<SqlPart> parts)
    {
        if (parts.Count == 0)
        {
            return this;
        }

        parts[0].Format(this);

        for (int i = 1; i < parts.Count; i++)
        {
            AppendSpace();
            parts[i].Format(this);
        }

        return this;
    }

    internal SqlBuildingBuffer AppendUpperSnakeCase(Enum value) =>
        AppendUpperSnakeCase(value.ToString());

    internal SqlBuildingBuffer AppendUpperSnakeCase(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return this;
        }

        for (int i = 0; i < text.Length; i++)
        {
            char c = text[i];

            if (i > 0 && char.IsUpper(c))
            {
                Append('_');
            }

            Append(char.ToUpperInvariant(c));
        }

        return this;
    }

    internal SqlBuildingBuffer CloseParenthesis(SqlPart? part = null)
    {
        part?.Format(this);
        Append(')');
        return this;
    }

    internal SqlBuildingBuffer EncloseInAliasQuotes(string value)
    {
        Append(_dialect.AliasQuote);
        Append(value);
        Append(_dialect.AliasQuote);
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
        string name = $"{_dialect.ParameterMarker}{_parameters.Count}";
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

    private void EnsureCapacity(int additionalChars)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (_position + additionalChars > _buffer.Length)
        {
            int requiredCapacity = _position + additionalChars;
            int newSize = Math.Max(_buffer.Length * 2, requiredCapacity);
            char[] newBuffer = ArrayPool<char>.Shared.Rent(newSize);
            _buffer.AsSpan(0, _position).CopyTo(newBuffer);
            ArrayPool<char>.Shared.Return(_buffer);
            _buffer = newBuffer;
        }
    }
}
