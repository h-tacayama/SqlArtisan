using System.Text;
using Dapper;

namespace InlineSqlSharp;

internal sealed class SqlBuildingBuffer
{
    private readonly StringBuilder _text = new();
    private readonly DynamicParameters _parameters = new();

    internal SqlBuildingBuffer Append(AbstractSqlPart element)
    {
        element.FormatSql(this);
        return this;
    }

    internal SqlBuildingBuffer Append(string? value)
    {
        _text.Append(value);
        return this;
    }

    internal SqlBuildingBuffer AppendCsv(AbstractSqlPart[] elements)
    {
        if (elements.Length == 0)
        {
            return this;
        }

        elements[0].FormatSql(this);

        for (int i = 1; i < elements.Length; i++)
        {
            _text.Append(", ");
            elements[i].FormatSql(this);
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

    internal SqlBuildingBuffer AppendSpace(AbstractSqlPart element)
    {
        element.FormatSql(this);
        _text.Append(" ");
        return this;
    }

    internal SqlBuildingBuffer AppendSpace(string? value = null)
    {
        _text.Append(value);
        _text.Append(" ");
        return this;
    }

    internal SqlBuildingBuffer AppendSpaceIf(bool condition, string? value = null)
    {
        if (condition)
        {
            _text.Append(value);
            _text.Append(" ");
        }

        return this;
    }

    internal SqlBuildingBuffer AppendSpaceIfNotNull(AbstractSqlPart? element)
    {
        if (element is not null)
        {
            element.FormatSql(this);
            _text.Append(" ");
        }

        return this;
    }

    internal SqlBuildingBuffer AppendSpaceSeparated(AbstractSqlPart[] elements)
    {
        if (elements.Length == 0)
        {
            return this;
        }

        elements[0].FormatSql(this);

        for (int i = 1; i < elements.Length; i++)
        {
            _text.Append(" ");
            elements[i].FormatSql(this);
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

    internal SqlBuildingBuffer CloseParenthesis(AbstractSqlPart? element = null)
    {
        if (element != null)
        {
            element.FormatSql(this);
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

    internal SqlBuildingBuffer EncloseInParentheses(AbstractSqlPart element)
    {
        _text.Append("(");
        element.FormatSql(this);
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

    internal SqlBuildingBuffer OpenParenthesis(AbstractSqlPart? element = null)
    {
        _text.Append("(");

        if (element != null)
        {
            element.FormatSql(this);
        }

        return this;
    }

    internal SqlBuildingBuffer PrependComma(AbstractSqlPart element)
    {
        _text.Append(", ");
        element.FormatSql(this);
        return this;
    }

    internal SqlBuildingBuffer PrependCommaIfNotNull(AbstractSqlPart? element)
    {
        if (element is not null)
        {
            _text.Append(", ");
            element.FormatSql(this);
        }

        return this;
    }

    internal SqlBuildingBuffer PrependSpace(string value)
    {
        _text.Append(" ");
        _text.Append(value);
        return this;
    }

    internal SqlBuildingBuffer PrependSpaceIf(bool condition, string value)
    {
        if (condition)
        {
            _text.Append(" ");
            _text.Append(value);
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
