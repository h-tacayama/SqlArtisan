using Npgsql;
using SqlArtisan.Dapper;

namespace SqlArtisan.Tests;

public class ArrayQueryParameterTests
{
    [Fact]
    public void AddParameter_PreservesArrayAsSingleParameter()
    {
        int[] values = [1, 2, 3];
        using NpgsqlCommand command = new();

        new ArrayQueryParameter(values).AddParameter(command, ":0");

        Assert.Single(command.Parameters);
        Assert.Equal(":0", command.Parameters[0].ParameterName);
        Assert.Same(values, command.Parameters[0].Value);
    }
}
