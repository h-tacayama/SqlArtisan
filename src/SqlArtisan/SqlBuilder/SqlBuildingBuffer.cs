using System.Text;
using Dapper;

namespace InlineSqlSharp;

internal sealed class SqlBuildingBuffer
{
    private readonly StringBuilder _text = new();
    private readonly DynamicParameters _parameters = new();

    internal SqlBuildingBuffer Append(AbstractSqlPart part)
    {
        part.FormatSql(this);
        return this;
    }

    internal SqlBuildingBuffer Append(string? value)
    {
        _text.Append(value);
        return this;
    }

    internal SqlBuildingBuffer AppendCsv(AbstractSqlPart[] parts)
    {
        if (parts.Length == 0)
        {
            return this;
        }

        parts[0].FormatSql(this);

        for (int i = 1; i < parts.Length; i++)
        {
            _text.Append(", ");
            parts[i].FormatSql(this);
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

    internal SqlBuildingBuffer AppendSelectItems(AbstractSqlPart[] selectItems)
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

    internal SqlBuildingBuffer AppendSpace(AbstractSqlPart part)
    {
        part.FormatSql(this);
        _text.Append(" ");
        return this;
    }

    internal SqlBuildingBuffer AppendSpaceIfNotNull(AbstractSqlPart? part)
    {
        if (part is not null)
        {
            part.FormatSql(this);
            _text.Append(" ");
        }

        return this;
    }

    internal SqlBuildingBuffer AppendSpaceSeparated(IList<AbstractSqlPart> parts)
    {
        if (parts.Count == 0)
        {
            return this;
        }

        parts[0].FormatSql(this);

        for (int i = 1; i < parts.Count; i++)
        {
            _text.Append(" ");
            parts[i].FormatSql(this);
        }

        return this;
    }

    internal SqlBuildingBuffer AddParameter(BindValue bindValue)
    {
        string name = $":{_parameters.ParameterNames.Count()}";
        _text.Append(name);

        // Dapper uses default values if DbType and Direction are null.
        _parameters.Add(name, bindValue.Value, bindValue.DbType, bindValue.Direction);

        return this;
    }

    internal SqlBuildingBuffer CloseParenthesis(AbstractSqlPart? part = null)
    {
        if (part != null)
        {
            part.FormatSql(this);
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

    internal SqlBuildingBuffer EncloseInParentheses(AbstractSqlPart part)
    {
        _text.Append("(");
        part.FormatSql(this);
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

    internal SqlBuildingBuffer OpenParenthesis(AbstractSqlPart? part = null)
    {
        _text.Append("(");

        if (part != null)
        {
            part.FormatSql(this);
        }

        return this;
    }

    internal SqlBuildingBuffer PrependComma(AbstractSqlPart part)
    {
        _text.Append(", ");
        part.FormatSql(this);
        return this;
    }

    internal SqlBuildingBuffer PrependCommaIfNotNull(AbstractSqlPart? part)
    {
        if (part is not null)
        {
            _text.Append(", ");
            part.FormatSql(this);
        }

        return this;
    }

    internal SqlStatement ToSqlStatement() =>
        new(_text.ToString(), _parameters);

    private void AppendSelectItem(AbstractSqlPart selectItem)
    {
        if (selectItem is ExprAlias alias)
        {
            alias.FormatAsSelect(this);
        }
        else
        {
            selectItem.FormatSql(this);
        }
    }
}
