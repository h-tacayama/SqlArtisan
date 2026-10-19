using static SqlArtisan.Sql;

namespace SqlArtisan.Tests;

// A function-argument enum binds only through Bind, new BindValue(...) or BindArray(...),
// which state the intent (#667); every other public enum binds wherever a value does.
public class FunctionArgumentEnumTests
{
    private static readonly Type[] s_functionArguments =
        [typeof(DateTimePart), typeof(RegexpOptions), typeof(SearchModifier)];

    private static readonly Type[] s_values = [typeof(Dbms), typeof(DbTypeCategory)];

    private readonly TestTable _t = new("t");

    public static TheoryData<string, object> FunctionArgumentInValueSlot()
    {
        TheoryData<string, object> data = [];
        string[] slots =
            ["Expression", "Comparison", "SelectItem", "InsertValue", "ValuesItem", "Regexp",
                "Against"];

        foreach (object value in new object[]
            { DateTimePart.Year, RegexpOptions.CaseInsensitive, SearchModifier.InBooleanMode })
        {
            foreach (string slot in slots)
            {
                data.Add(slot, value);
            }
        }

        return data;
    }

    [Theory]
    [MemberData(nameof(FunctionArgumentInValueSlot))]
    public void ImplicitValue_FunctionArgumentEnum_ThrowsArgumentException(
        string slot,
        object value)
    {
        Action build = slot switch
        {
            "Expression" => () => Sum(value),
            "Comparison" => () => _ = _t.Code == value,
            "SelectItem" => () => Select(value),
            "InsertValue" => () => InsertInto(_t, _t.Code).Values(value),
            "ValuesItem" => () => Values("v", ["x"], [[value]]),
            "Regexp" => () => RegexpCount(_t.Name, "a", value),
            _ => () => Match(_t.Name).Against(value),
        };

        ArgumentException ex = Assert.Throws<ArgumentException>(build);

        string name = value.GetType().Name;
        Assert.Equal(
            $"{name} is a function argument, not a value; pass it to a {name} parameter, "
                + "or, to store or compare the enum itself, bind it with Sql.Bind(...).",
            ex.Message);
    }

    [Fact]
    public void Bind_FunctionArgumentEnum_CorrectSql()
    {
        SqlStatement sql =
            Select(_t.Code)
            .From(_t)
            .Where(_t.Code == Bind(DateTimePart.Year))
            .Build();

        Assert.Equal("SELECT \"t\".code FROM test_table \"t\" WHERE \"t\".code = :0", sql.Text);
        Assert.Equal(1, sql.Parameters.Count);
        Assert.Equal(DateTimePart.Year, sql.Parameters.Get<DateTimePart>(":0"));
    }

    [Fact]
    public void BindValue_FunctionArgumentEnum_CorrectSql()
    {
        SqlStatement sql =
            InsertInto(_t, _t.Code)
            .Values(new BindValue(SearchModifier.InBooleanMode))
            .Build();

        Assert.Equal("INSERT INTO test_table AS \"t\" (code) VALUES (:0)", sql.Text);
        Assert.Equal(1, sql.Parameters.Count);
        Assert.Equal(
            SearchModifier.InBooleanMode, sql.Parameters.Get<SearchModifier>(":0"));
    }

    // No factory takes Dbms or DbTypeCategory as an argument, so one in a value slot
    // is meant as the value it binds.
    [Fact]
    public void ImplicitValue_OtherSqlArtisanEnum_CorrectSql()
    {
        SqlStatement sql =
            Select(_t.Code)
            .From(_t)
            .Where((_t.Code == Dbms.Oracle) & (_t.Name == DbTypeCategory.Text))
            .Build();

        Assert.Equal(
            "SELECT \"t\".code FROM test_table \"t\" "
                + "WHERE (\"t\".code = :0) AND (\"t\".name = :1)",
            sql.Text);
        Assert.Equal(2, sql.Parameters.Count);
        Assert.Equal(Dbms.Oracle, sql.Parameters.Get<Dbms>(":0"));
        Assert.Equal(DbTypeCategory.Text, sql.Parameters.Get<DbTypeCategory>(":1"));
    }

    // A new public enum must be sorted into one list or the other: a function argument
    // left out would bind silently at every arity that lacks its slot.
    [Fact]
    public void PublicEnums_AreEachClassified()
    {
        List<string> shipped = [.. typeof(Sql).Assembly.GetExportedTypes()
            .Where(type => type.IsEnum)
            .Select(type => type.Name)
            .OrderBy(name => name, StringComparer.Ordinal)];
        List<string> classified = [.. s_functionArguments.Concat(s_values)
            .Select(type => type.Name)
            .OrderBy(name => name, StringComparer.Ordinal)];

        Assert.Equal(shipped, classified);
    }

    [Fact]
    public void ImplicitValue_EveryPublicEnum_BindsOrThrowsAsClassified()
    {
        foreach (Type type in s_functionArguments.Concat(s_values))
        {
            object value = Enum.GetValues(type).GetValue(0)!;

            bool rejected = Record.Exception(() => Select(value)) is ArgumentException;

            Assert.True(
                rejected == s_functionArguments.Contains(type),
                $"{type.Name} {(rejected ? "is rejected" : "binds")} in a value slot.");
        }
    }
}
