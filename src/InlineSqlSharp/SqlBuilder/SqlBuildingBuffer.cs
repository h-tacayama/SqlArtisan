using System.Text;

namespace InlineSqlSharp;

public sealed class SqlBuildingBuffer()
{
    private readonly StringBuilder _text = new();
    private readonly List<BindParameter> _parameters = [];

    internal SqlBuildingBuffer Append(ISqlElement element)
    {
        element.FormatSql(this);
        return this;
    }

    internal SqlBuildingBuffer Append(string? value)
    {
        _text.Append(value);
        return this;
    }

    internal SqlBuildingBuffer AppendComma(ISqlElement element)
    {
        element.FormatSql(this);
        _text.Append(", ");
        return this;
    }

    internal SqlBuildingBuffer AppendComma(string? value = null)
    {
        _text.Append(value);
        _text.Append(", ");
        return this;
    }

    internal SqlBuildingBuffer AppendCsv(ISqlElement[] elements)
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

    internal SqlBuildingBuffer AppendIf(bool condition, string? value)
    {
        if (condition)
        {
            _text.Append(value);
        }

        return this;
    }

    internal SqlBuildingBuffer AppendSelectList(IExprOrAlias[] selectList)
    {
        if (selectList.Length == 0)
        {
            return this;
        }

        selectList[0].FormatAsSelect(this);

        for (int i = 1; i < selectList.Length; i++)
        {
            _text.Append(", ");
            selectList[i].FormatAsSelect(this);
        }

        return this;
    }

    internal SqlBuildingBuffer AppendSpace(ISqlElement element)
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

    internal SqlBuildingBuffer AppendSpaceIf(bool condition, ISqlElement element)
    {
        if (condition)
        {
            element.FormatSql(this);
            _text.Append(" ");
        }

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

    internal SqlBuildingBuffer AppendSpaceIfNotNull(ISqlElement? element)
    {
        if (element is not null)
        {
            element.FormatSql(this);
            _text.Append(" ");
        }

        return this;
    }

    internal SqlBuildingBuffer AppendSpaceSeparated(ISqlElement[] elements)
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

    internal SqlBuildingBuffer AddParameter(IBindValue bindValue)
    {
        int index = _parameters.Count;
        BindParameter parameter = new($":{index}", bindValue);
        _parameters.Add(parameter);
        _text.Append(parameter.Name);
        return this;
    }

    internal SqlBuildingBuffer CloseParenthesis(ISqlElement? element = null)
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

    internal SqlBuildingBuffer EncloseInParentheses(ISqlElement element)
    {
        _text.Append("(");
        element.FormatSql(this);
        _text.Append(")");
        return this;
    }

    internal SqlBuildingBuffer EncloseInSingleQuotes(ISqlElement element)
    {
        _text.Append("'");
        element.FormatSql(this);
        _text.Append("'");
        return this;
    }

    internal SqlBuildingBuffer EncloseInSingleQuotes(string value)
    {
        _text.Append("'");
        _text.Append(value);
        _text.Append("'");
        return this;
    }

    internal SqlBuildingBuffer EncloseInSpaces(ISqlElement element)
    {
        _text.Append(" ");
        element.FormatSql(this);
        _text.Append(" ");
        return this;
    }

    internal SqlBuildingBuffer EncloseInSpaces(string value)
    {
        _text.Append(" ");
        _text.Append(value);
        _text.Append(" ");
        return this;
    }

    internal SqlBuildingBuffer OpenParenthesis(ISqlElement? element = null)
    {
        _text.Append("(");

        if (element != null)
        {
            element.FormatSql(this);
        }

        return this;
    }

    internal SqlBuildingBuffer PrependComma(ISqlElement element)
    {
        _text.Append(", ");
        element.FormatSql(this);
        return this;
    }

    internal SqlBuildingBuffer PrependComma(string value)
    {
        _text.Append(", ");
        _text.Append(value);
        return this;
    }

    internal SqlBuildingBuffer PrependCommaIf(bool condition, ISqlElement element)
    {
        if (condition)
        {
            _text.Append(", ");
            element.FormatSql(this);
        }

        return this;
    }

    internal SqlBuildingBuffer PrependCommaIf(bool condition, string value)
    {
        if (condition)
        {
            _text.Append(", ");
            _text.Append(value);
        }

        return this;
    }

    internal SqlBuildingBuffer PrependCommaIfNotNull(ISqlElement? element)
    {
        if (element is not null)
        {
            _text.Append(", ");
            element.FormatSql(this);
        }

        return this;
    }

    internal SqlBuildingBuffer PrependSpace(ISqlElement element)
    {
        _text.Append(" ");
        element.FormatSql(this);
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
        // Return a clone of _parameters to avoid keeping references
        new(_text.ToString(), [.. _parameters]);
}
