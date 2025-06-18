using System.Text;

namespace SqlArtisan.TableClassGen;

internal sealed class DbTableInfo(
    string tableName,
    IReadOnlyList<DbColumnInfo> columns)
{
    public string TableName => tableName;

    public string ClassName => $"{CaseConverter.SnakeToPascalCase(tableName)}Table";

    public IReadOnlyList<DbColumnInfo> Columns => columns;

    public string GenerateCode(string @namespace)
    {
        StringBuilder code = new();

        code.AppendLine("using SqlArtisan;");
        code.AppendLine();
        code.AppendLine($"namespace {@namespace};");
        code.AppendLine();
        code.AppendLine($"internal sealed class {ClassName} : DbTableBase");
        code.AppendLine("{");

        code.AppendLine($"\tpublic {ClassName}(string tableAlias = \"\") : base(\"{TableName}\", tableAlias)");
        code.AppendLine("\t{");

        foreach (DbColumnInfo column in Columns)
        {
            code.AppendLine($"\t\t{column.PascalCaseName} = new DbColumn(tableAlias, \"{column.Name}\");");
        }

        code.AppendLine("\t}");

        foreach (DbColumnInfo column in Columns)
        {
            code.AppendLine();
            code.AppendLine($"\tpublic DbColumn {column.PascalCaseName} {{ get; }}");
        }

        code.AppendLine("}");

        return code.ToString();
    }
}
