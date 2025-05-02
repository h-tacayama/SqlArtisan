using System.Text;

namespace SqlArtisan;

internal sealed class SqlBuildingBuffer
{
    private readonly StringBuilder _text = new();
    private readonly Dictionary<string, BindValue> _parameters = new();

    internal SqlBuildingBuffer Append(SqlPart part)
    {
        part.Format(this);
        return this;
    }

    internal SqlBuildingBuffer Append(string? value)
    {
        _text.Append(value);
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
            _text.Append(", ");
            parts[i].Format(this);
        }

        return this;
    }

    internal SqlBuildingBuffer AppendIf(bool when, string? value)
    {
        if (when)
        {
            _text.Append(value);
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
            _text.Append(", ");
            AppendSelectItem(selectItems[i]);
        }

        return this;
    }

    internal SqlBuildingBuffer AppendSpace()
    {
        _text.Append(" ");
        return this;
    }

    internal SqlBuildingBuffer AppendSpace(SqlPart part)
    {
        part.Format(this);
        _text.Append(" ");
        return this;
    }

    internal SqlBuildingBuffer AppendSpaceIfNotNull(SqlPart? part)
    {
        if (part is not null)
        {
            part.Format(this);
            _text.Append(" ");
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
            _text.Append(" ");
            parts[i].Format(this);
        }

        return this;
    }

    internal SqlBuildingBuffer AddParameter(BindValue bindValue)
    {
        string name = $":{_parameters.Count}";
        _text.Append(name);
        _parameters.Add(name, bindValue);
        return this;
    }

    internal SqlBuildingBuffer CloseParenthesis(SqlPart? part = null)
    {
        if (part != null)
        {
            part.Format(this);
        }

        _text.Append(")");
        return this;
    }

    internal SqlBuildingBuffer EncloseInDoubleQuotes(string value)
    {
        _text.Append("\"");
        _text.Append(value);
        _text.Append("\"");
        return this;
    }

    internal SqlBuildingBuffer EncloseInParentheses(SqlPart part)
    {
        _text.Append("(");
        part.Format(this);
        _text.Append(")");
        return this;
    }

    internal SqlBuildingBuffer EncloseInSpaces(string value)
    {
        _text.Append(" ");
        _text.Append(value);
        _text.Append(" ");
        return this;
    }

    internal SqlBuildingBuffer OpenParenthesis(SqlPart? part = null)
    {
        _text.Append("(");

        if (part != null)
        {
            part.Format(this);
        }

        return this;
    }

    internal SqlBuildingBuffer PrependComma(SqlPart part)
    {
        _text.Append(", ");
        part.Format(this);
        return this;
    }

    internal SqlBuildingBuffer PrependCommaIfNotNull(SqlPart? part)
    {
        if (part is not null)
        {
            _text.Append(", ");
            part.Format(this);
        }

        return this;
    }

    internal SqlStatement ToSqlStatement() =>
        new(_text.ToString(), _parameters);

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
}
