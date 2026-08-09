using static SqlArtisan.Sql;

namespace SqlArtisan.Tests;

public partial class FunctionTests
{
    [Fact]
    public void Year_PrecisionAboveMaximum_ThrowsArgumentException()
    {
        ArgumentException ex = Assert.Throws<ArgumentException>(() => Year(10));

        Assert.Equal("YEAR precision must be between 0 and 9.", ex.Message);
    }

    [Fact]
    public void Year_PrecisionNegative_ThrowsArgumentException()
    {
        ArgumentException ex = Assert.Throws<ArgumentException>(() => Year(-1));

        Assert.Equal("YEAR precision must be between 0 and 9.", ex.Message);
    }
}
