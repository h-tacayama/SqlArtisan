using System.Text;

namespace InlineSqlSharp.TableClassGen;

internal sealed class DbTableInfo(
    string name,
    IReadOnlyList<DbColumnInfo> columns)
{
    public string Name => name;

    public string PascalCaseName => CaseConverter.SnakeToPascalCase(name);

    public IReadOnlyList<DbColumnInfo> Columns => columns;

    public string GenerateCode(string @namespace)
    {
        StringBuilder code = new();

        code.AppendLine("using InlineSqlSharp;");
        code.AppendLine();
        code.AppendLine($"namespace {@namespace};");
        code.AppendLine();
        code.AppendLine($"internal sealed class {PascalCaseName} : AbstractTable");
        code.AppendLine("{");

        code.AppendLine($"\tpublic {PascalCaseName}(string tableAlias) : base(\"{Name}\", tableAlias)");
        code.AppendLine("\t{");

        foreach (DbColumnInfo column in Columns)
        {
            code.AppendLine($"\t\t{column.PascalCaseName} = new Column(tableAlias, \"{column.Name}\");");
        }

        code.AppendLine("\t}");

        foreach (DbColumnInfo column in Columns)
        {
            code.AppendLine();
            code.AppendLine($"\tpublic Column {column.PascalCaseName} {{ get; }}");
        }

        code.AppendLine("}");

        return code.ToString();
    }
}
