using System.Globalization;
using Microsoft.CodeAnalysis.CSharp;
using SqlArtisan.TableClassGen;

namespace SqlArtisan.TableClassGen.Tests;

// A mixed-case catalog name (SQL Server, SQLite) keeps its casing past the first
// letter; only a single-case run is title-cased, so OrderID never becomes Orderid.
public class CaseConverterTests
{
    [Theory]
    [InlineData("order_id", "OrderId")]
    [InlineData("ORDER_ID", "OrderId")]
    [InlineData("OrderID", "OrderID")]
    [InlineData("customerName", "CustomerName")]
    [InlineData("IsActive", "IsActive")]
    [InlineData("URLPath", "URLPath")]
    [InlineData("web$api#v2", "WebApiV2")]
    [InlineData("2fa_code", "_2faCode")]
    [InlineData("__", "_")]
    [InlineData("", "")]
    public void SnakeToPascalCase_ConvertsEachRunByItsOwnCasing(string name, string expected) =>
        Assert.Equal(expected, CaseConverter.SnakeToPascalCase(name));

    [Theory]
    [InlineData("ลูกค้า", "ลูกค้า")]
    [InlineData("ชื่อ", "ชื่อ")]
    [InlineData("ग्राहक", "ग्राहक")]
    [InlineData("नाम", "नाम")]
    [InlineData("customer_ชื่อ", "Customerชื่อ")]
    [InlineData("école", "École")]
    [InlineData("a‿b", "A‿b")]
    [InlineData("́x", "_́x")]
    [InlineData("‿x", "_‿x")]
    [InlineData("ab‌cd", "Abcd")]
    [InlineData("‌", "_")]
    [InlineData("𠮷野家", "野家")]
    public void SnakeToPascalCase_NonAsciiName_KeepsWhatCSharpAdmits(
        string name,
        string expected) =>
        Assert.Equal(expected, CaseConverter.SnakeToPascalCase(name));

    // Roslyn's own admission test is the oracle: a character C# admits stays inside the
    // word, a formatting one vanishes without splitting it, and any other one splits it.
    [Fact]
    public void SnakeToPascalCase_EveryBmpCharacter_MatchesCSharpIdentifierRules()
    {
        for (int code = 0; code <= char.MaxValue; code++)
        {
            char c = (char)code;
            string result = CaseConverter.SnakeToPascalCase($"a{c}b");

            Assert.True(SyntaxFacts.IsValidIdentifier(result), $"U+{code:X4} gave {result}");
            if (char.GetUnicodeCategory(c) == UnicodeCategory.Format)
            {
                Assert.Equal("Ab", result);
            }
            else if (c != '_' && SyntaxFacts.IsIdentifierPartCharacter(c))
            {
                Assert.True(result.Length == 3 && result[2] == 'b', $"U+{code:X4} gave {result}");
            }
            else
            {
                Assert.Equal("AB", result);
            }
        }
    }
}
