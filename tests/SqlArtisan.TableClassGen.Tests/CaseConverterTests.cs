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
}
