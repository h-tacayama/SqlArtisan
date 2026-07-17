using SqlArtisan.TableClassGen;

namespace SqlArtisan.TableClassGen.Tests;

public class GeneratedIdentifierTests
{
    // PascalCase upper-cases each word's first letter, and every C# keyword is
    // lowercase, so a generated identifier can never collide with a keyword —
    // this pins that guarantee (see #322: @-escaping is unnecessary).
    [Fact]
    public void GeneratedCode_KeywordCatalogNames_Compiles()
    {
        string[] keywords =
        [
            "class", "int", "namespace", "public", "static", "void", "return",
            "null", "true", "false", "for", "if", "else", "new", "using", "this",
            "base", "var", "async", "await", "record", "yield", "nameof", "value",
        ];

        List<DbColumnInfo> columns =
            [.. keywords.Select(keyword => new DbColumnInfo(keyword, "text"))];
        DbTableInfo table = new("class", columns);

        GeneratedCodeCompiler.AssertCompiles([table.GenerateCode("Generated.Tables")]);
    }
}
