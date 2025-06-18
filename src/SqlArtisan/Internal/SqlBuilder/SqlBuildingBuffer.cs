using System.Buffers;
using System.Text;

namespace SqlArtisan.Internal;

internal sealed class SqlBuildingBuffer : IDisposable
{
    private const int InitialCapacity = 2048;
    private readonly IDbmsDialect _dialect;
    private char[] _buffer;
    private int _position;
    private Dictionary<string, BindValue> _parameters = [];
    private bool _disposed = false;

    internal SqlBuildingBuffer(IDbmsDialect dialect)
    {
        _dialect = dialect;
        _buffer = ArrayPool<char>.Shared.Rent(InitialCapacity);
        _position = 0;
    }

    ~SqlBuildingBuffer()
    {
        Dispose(false);
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
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
        Append(" ");
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

        StringBuilder builder = new();

        for (int i = 0; i < text.Length; i++)
        {
            char c = text[i];

            if (i > 0 && char.IsUpper(c))
            {
                builder.Append('_');
            }

            builder.Append(char.ToUpper(c));
        }

        Append(builder.ToString());
        return this;
    }

    internal SqlBuildingBuffer CloseParenthesis(SqlPart? part = null)
    {
        part?.Format(this);
        Append(")");
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
        Append("(");
        part.Format(this);
        Append(")");
        return this;
    }

    internal SqlBuildingBuffer EncloseInSpaces(string value)
    {
        Append(" ");
        Append(value);
        Append(" ");
        return this;
    }

    internal SqlBuildingBuffer OpenParenthesis(SqlPart? part = null)
    {
        Append("(");
        part?.Format(this);
        return this;
    }

    internal SqlBuildingBuffer PrependComma(SqlPart part)
    {
        Append(", ");
        part.Format(this);
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

    internal SqlBuildingBuffer AddParameter(BindValue bindValue)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        string name = $"{_dialect.ParameterMarker}{_parameters.Count}";
        Append(name);
        _parameters.Add(name, bindValue);
        return this;
    }

    internal SqlStatement ToSqlStatement()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        // Pass a copy of _parameter to ensure
        // the original reference is not retained by the caller.
        string sql = new(_buffer, 0, _position);
        Dictionary<string, BindValue> parametersCopy = new(_parameters);
        return new(sql, parametersCopy);
    }

    private void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                if (_buffer != null)
                {
                    ArrayPool<char>.Shared.Return(_buffer);
                    _buffer = null!;
                }

                _parameters.Clear();
                _parameters = null!;
            }

            _disposed = true;
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
