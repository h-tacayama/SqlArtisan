using System.Reflection;

namespace SqlArtisan.Tests;

// Keywords.cs holds exactly one SQL lexical token per constant; a multi-word
// phrase is composed at its use site by const interpolation of atoms
// (.claude/rules/sql-building-style.md, #208). This gate keeps the rule
// mechanical instead of remembered, like the BOM gate (#134).
public class KeywordsTests
{
    [Fact]
    public void Keywords_EveryConstant_IsSingleToken()
    {
        Type keywords = typeof(Dbms).Assembly.GetType("SqlArtisan.Internal.Keywords")!;
        FieldInfo[] constants = keywords.GetFields(BindingFlags.NonPublic | BindingFlags.Static);

        Assert.NotEmpty(constants);

        foreach (FieldInfo constant in constants)
        {
            string value = (string)constant.GetRawConstantValue()!;

            Assert.False(
                value.Contains(' '),
                $"Keywords.{constant.Name} = \"{value}\" contains a space. One SQL token per constant — compose the phrase at the use site with const interpolation.");
        }
    }
}
