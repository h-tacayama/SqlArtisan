namespace SqlArtisan.Analyzers.Tests;

public class ConstructKeyNamingTests
{
    [Theory]
    [InlineData("MergeInto", "merge_into")]
    [InlineData("DateTrunc", "date_trunc")]
    [InlineData("WithRollup", "with_rollup")]
    [InlineData("DistinctOn", "distinct_on")]
    [InlineData("AddMonths", "add_months")]
    [InlineData("Dateadd", "dateadd")]
    [InlineData("Rollup", "rollup")]
    public void ToSnakeCase_PascalCaseName_MatchesUnderscoreSegments(string pascalCase, string expectedSnakeCase)
    {
        Assert.Equal(expectedSnakeCase, ConstructKeyNaming.ToSnakeCase(pascalCase));
    }

    [Fact]
    public void MemberKey_Round_HasSqlartisanConstructPrefix()
    {
        Assert.Equal("sqlartisan_construct_round", ConstructKeyNaming.MemberKey("Round"));
    }

    [Fact]
    public void ArityKey_RoundArity2_AppendsArityNotBareDigit()
    {
        Assert.Equal("sqlartisan_construct_round_arity2", ConstructKeyNaming.ArityKey("Round", 2));
    }

    [Fact]
    public void ArityKey_MemberEndingInDigit_DoesNotCollideWithArityKey()
    {
        // "Atan2" (single word, SQL token ATAN2 has no underscore) snake-cases to "atan2";
        // an "Atan" member's 2-argument-form arity key is "atan_arity2" — the "_arity"
        // separator is exactly what keeps these from colliding.
        Assert.Equal("sqlartisan_construct_atan2", ConstructKeyNaming.MemberKey("Atan2"));
        Assert.Equal("sqlartisan_construct_atan_arity2", ConstructKeyNaming.ArityKey("Atan", 2));
    }
}
