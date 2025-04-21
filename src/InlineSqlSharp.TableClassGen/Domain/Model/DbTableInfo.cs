using System.Text;

namespace InlineSqlSharp.TableClassGen;

internal sealed class DbTableInfo(
    string name,
    IReadOnlyList<DbColumnInfo> columns)
{
    public string Name => name;

    public IReadOnlyList<DbColumnInfo> Columns => columns;

    public string GenerateCode(string @namespace)
    {
        StringBuilder code = new();

        code.AppendLine($"namespace {@namespace};");
        code.AppendLine();
        code.AppendLine($"internal sealed class {Name} : Table");
        code.AppendLine("{");

        code.AppendLine($"\tpublic {Name}(string alias) : base(alias)");
        code.AppendLine("\t{");

        foreach (DbColumnInfo column in Columns)
        {
            code.AppendLine($"\t\t{column.Name} = new Column(alias, nameof({column.Name}));");
        }

        code.AppendLine("\t}");

        foreach (DbColumnInfo column in Columns)
        {
            code.AppendLine();
            code.AppendLine($"\tpublic Column {column.Name} {{ get; }}");
        }

        code.AppendLine("}");

        return code.ToString();
    }
}
