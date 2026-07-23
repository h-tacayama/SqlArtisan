using System.Reflection;

namespace SqlArtisan.Tests;

public class DbColumnTests
{
    [Fact]
    public void DbColumn_Name_ReturnsRawColumnName()
    {
        TestTable t = new("t");

        Assert.Equal("code", t.Code.Name);
        Assert.Equal("name", t.Name.Name);
        Assert.Equal("created_at", t.CreatedAt.Name);
    }

    [Fact]
    public void DbColumn_ReflectedColumnProperties_ExposeNames()
    {
        TestTable t = new("t");

        PropertyInfo[] columnProperties = [.. typeof(TestTable)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.PropertyType == typeof(DbColumn))];

        IEnumerable<string> names = columnProperties.Select(p => ((DbColumn)p.GetValue(t)!).Name);

        Assert.Equal(["code", "name", "created_at"], names);
    }
}
