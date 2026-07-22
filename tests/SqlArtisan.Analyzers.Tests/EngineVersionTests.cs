namespace SqlArtisan.Analyzers.Tests;

public class EngineVersionTests
{
    [Theory]
    [InlineData("8.0.16")]
    [InlineData("23")]
    [InlineData("23ai")]
    [InlineData("3.44")]
    [InlineData("2022")]
    [InlineData("21.3")]
    public void TryParse_RecognizedFormat_Succeeds(string value)
    {
        Assert.True(EngineVersion.TryParse(value, out _));
    }

    [Theory]
    [InlineData("")]
    [InlineData("abc")]
    [InlineData("8..0")]
    [InlineData(".5")]
    [InlineData("v8")]
    [InlineData(null)]
    public void TryParse_UnrecognizedFormat_Fails(string? value)
    {
        Assert.False(EngineVersion.TryParse(value, out _));
    }

    [Fact]
    public void Parse_TrailingLetters_IgnoresThem()
    {
        EngineVersion twentyThreeAi = EngineVersion.Parse("23ai");
        EngineVersion twentyThree = EngineVersion.Parse("23");

        Assert.Equal(0, twentyThreeAi.CompareTo(twentyThree));
    }

    [Fact]
    public void CompareTo_HigherPatch_IsGreater()
    {
        Assert.True(EngineVersion.Parse("8.0.20") >= EngineVersion.Parse("8.0.16"));
        Assert.True(EngineVersion.Parse("8.0.16") < EngineVersion.Parse("8.0.20"));
    }

    [Fact]
    public void CompareTo_MissingSegment_ReadsAsZero()
    {
        Assert.Equal(0, EngineVersion.Parse("23").CompareTo(EngineVersion.Parse("23.0")));
        Assert.True(EngineVersion.Parse("8.0") < EngineVersion.Parse("8.0.1"));
    }

    [Fact]
    public void CompareTo_ComparesNumerically_NotLexicographically()
    {
        Assert.True(EngineVersion.Parse("3.44") > EngineVersion.Parse("3.9"));
    }

    [Fact]
    public void CompareTo_HigherYear_IsGreater()
    {
        Assert.True(EngineVersion.Parse("2022") >= EngineVersion.Parse("2019"));
    }

    [Fact]
    public void ToString_PreservesOriginalSpelling()
    {
        Assert.Equal("23ai", EngineVersion.Parse("23ai").ToString());
    }
}
