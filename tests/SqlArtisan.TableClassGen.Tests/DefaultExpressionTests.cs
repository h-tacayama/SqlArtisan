using SqlArtisan.TableClassGen;

namespace SqlArtisan.TableClassGen.Tests;

public class DefaultExpressionTests
{
    // SQLite keeps the text as written; SQL Server stores the constraint
    // parenthesized, once or twice depending on how it was declared.
    [Theory]
    [InlineData("NULL")]
    [InlineData("null")]
    [InlineData("(NULL)")]
    [InlineData("((NULL))")]
    [InlineData(" NULL ")]
    public void IsNull_ExplicitNull_ReturnsTrue(string text) =>
        Assert.True(DefaultExpression.IsNull(text));

    [Theory]
    [InlineData("'NULL'")]
    [InlineData("('NULL')")]
    [InlineData("NULLIF(1, 1)")]
    [InlineData("0")]
    [InlineData("")]
    public void IsNull_AnyOtherDefault_ReturnsFalse(string text) =>
        Assert.False(DefaultExpression.IsNull(text));
}
