using System.Text;

namespace SqlArtisan.Tests;

// The shapes csharp-formatting.md names that the formatter cannot see (IDE2000 gates
// runs only): no blank line directly after `{` or before `}`, and no code line ending
// in `+`, `&&` or `||`.
public class FormattingSweepTests
{
    private enum Mode
    {
        Code,
        BlockComment,
        RegularString,
        VerbatimString,
        RawString,
    }

    [Fact]
    public void NoBlankLineBesideABrace()
    {
        string root = CommentCapRatchetTests.FindRepoRoot();
        List<string> offenders = [];

        foreach (string path in CommentCapRatchetTests.SourceFiles(root))
        {
            string[] lines = File.ReadAllLines(path);
            for (int i = 1; i < lines.Length - 1; i++)
            {
                if (lines[i].Trim().Length != 0)
                {
                    continue;
                }

                if (lines[i - 1].TrimEnd().EndsWith('{') || lines[i + 1].Trim() == "}")
                {
                    offenders.Add($"{Path.GetRelativePath(root, path).Replace('\\', '/')}:{i + 1}");
                }
            }
        }

        Assert.True(
            offenders.Count == 0,
            $"{offenders.Count} blank line(s) beside a brace:\n  " + string.Join(
                "\n  ",
                offenders));
    }

    [Fact]
    public void NoWrappedOperatorAtALineEnd()
    {
        string root = CommentCapRatchetTests.FindRepoRoot();
        List<string> offenders = [];

        foreach (string path in CommentCapRatchetTests.SourceFiles(root))
        {
            string[] code = CodeOnly(File.ReadAllLines(path));
            for (int i = 0; i < code.Length; i++)
            {
                string line = code[i].TrimEnd();
                if ((line.EndsWith('+') && !line.EndsWith("++", StringComparison.Ordinal))
                    || line.EndsWith("&&", StringComparison.Ordinal)
                    || line.EndsWith("||", StringComparison.Ordinal))
                {
                    offenders.Add($"{Path.GetRelativePath(root, path).Replace('\\', '/')}:{i + 1}");
                }
            }
        }

        Assert.True(
            offenders.Count == 0,
            $"{offenders.Count} line(s) ending in a wrapped operator; lead the next line "
                + "with it instead:\n  " + string.Join("\n  ", offenders));
    }

    [Theory]
    [InlineData("string q = @\"\"\"\";", "string q = @_;")]
    [InlineData("string q = @\"\"\"x\"\" y\" + z;", "string q = @_ + z;")]
    [InlineData("string q = $@\"a {b} \"\"c\"\"\" + z;", "string q = $@_b + z;")]
    [InlineData("string q = \"a \\\" b\" + z;", "string q = _ + z;")]
    [InlineData("char c = '\"'; int n = a +", "char c = _; int n = a +")]
    [InlineData("char c = '\\''; int n = a +", "char c = _; int n = a +")]
    [InlineData("int n = a + /* b && */ c; // d ||", "int n = a +  c; ")]
    [InlineData("f++;", "f++;")]
    [InlineData(
        "string m = $\"[{string.Join(\"\\\", \\\"\", xs)}]\" +",
        "string m = $_string.Join(_, xs) +")]
    [InlineData("string m = $\"got '{json.Trim('\"')}'\" +", "string m = $_json.Trim(_) +")]
    [InlineData(
        "string m = $\"{(a ? \"/*\" : \"x\")}\"; bool b = p &&",
        "string m = $_(a ? _ : _); bool b = p &&")]
    [InlineData("string m = $\"{a ?? \"//\"} {{x}}\" +", "string m = $_a ?? _ +")]
    [InlineData("string m = $$\"\"\"{a} {{b}}\"\"\" +", "string m = $$_b +")]
    public void CodeOnly_BlanksLiteralsAndDropsComments(string line, string expected) =>
        Assert.Equal(expected, CodeOnly([line])[0]);

    [Fact]
    public void CodeOnly_MultiLineRawString_BlanksItsBody()
    {
        string[] code = CodeOnly(["string q = \"\"\"", "    a &&", "    \"\"\" + z;"]);

        Assert.Equal(["string q = _", "", " + z;"], code);
    }

    [Fact]
    public void CodeOnly_MultiLineInterpolationHole_ReadsTheHoleAsCode()
    {
        string[] code = CodeOnly(
            ["string m = $@\"a {", "    x +", "    } b", "c\" +", "    y;", "int n = p &&"]);

        Assert.Equal(["string m = $@_", "    x +", "    ", " +", "    y;", "int n = p &&"], code);
    }

    // Each line's code with literals blanked and comments dropped, so an operator
    // inside a string or a comment never reads as a trailing one. An interpolation
    // hole is code again, so a string or comment nested in one cannot desync the read.
    internal static string[] CodeOnly(string[] lines)
    {
        string[] code = new string[lines.Length];
        Stack<Frame> frames = new();
        frames.Push(new Frame(Mode.Code));

        for (int n = 0; n < lines.Length; n++)
        {
            string line = lines[n];
            StringBuilder kept = new();
            int i = 0;
            while (i < line.Length)
            {
                i = Step(line, i, frames, kept);
            }

            // Only an interpolation hole lets a regular string span lines; one left
            // open with no hole above it is unterminated, so the read restarts as code.
            if (frames.Peek().Mode == Mode.RegularString)
            {
                frames.Pop();
            }

            code[n] = kept.ToString();
        }

        return code;
    }

    private static int Step(string line, int i, Stack<Frame> frames, StringBuilder kept)
    {
        Frame top = frames.Peek();
        char c = line[i];
        switch (top.Mode)
        {
            case Mode.Code when c == '/' && At(line, i + 1) == '/':
                return line.Length;
            case Mode.Code when c == '/' && At(line, i + 1) == '*':
                frames.Push(new Frame(Mode.BlockComment));
                return i + 2;
            case Mode.Code when c == '"':
                return OpenString(line, i, frames, kept);
            case Mode.Code when c == '\'':
                int close = line.IndexOf('\'', i + (At(line, i + 1) == '\\' ? 3 : 2));
                kept.Append('_');
                return close < 0 ? line.Length : close + 1;
            case Mode.Code when top.IsHole && c == '{':
                top.Depth++;
                kept.Append(c);
                return i + 1;
            case Mode.Code when top.IsHole && c == '}':
                if (top.Depth == 0)
                {
                    frames.Pop();
                    return i + 1;
                }

                top.Depth--;
                kept.Append(c);
                return i + 1;
            case Mode.Code:
                kept.Append(c);
                return i + 1;
            case Mode.BlockComment when c == '*' && At(line, i + 1) == '/':
                frames.Pop();
                return i + 2;
            case Mode.RegularString when c == '\\':
                return i + 2;
            case Mode.VerbatimString when c == '"' && At(line, i + 1) == '"':
                return i + 2;
            case Mode.RegularString or Mode.VerbatimString when c == '"':
                frames.Pop();
                return i + 1;
            case Mode.RegularString or Mode.VerbatimString when top.Dollars > 0 && c == '{':
                if (At(line, i + 1) == '{')
                {
                    return i + 2;
                }

                frames.Push(Frame.Hole());
                return i + 1;
            case Mode.RawString when c == '"' && QuoteRun(line, i) >= top.Quotes:
                frames.Pop();
                return i + top.Quotes;
            case Mode.RawString when top.Dollars > 0 && c == '{':
                int run = BraceRun(line, i);
                if (run >= top.Dollars)
                {
                    frames.Push(Frame.Hole());
                }

                return i + run;
            default:
                return i + 1;
        }
    }

    // The prefix decides before the quote run: a raw literal takes no `@`, and
    // `@""""` is a verbatim one-quote string, not a raw opener.
    private static int OpenString(string line, int i, Stack<Frame> frames, StringBuilder kept)
    {
        int dollars = 0;
        bool verbatim = false;
        for (int back = i - 1; back >= 0 && line[back] is '$' or '@'; back--)
        {
            dollars += line[back] == '$' ? 1 : 0;
            verbatim |= line[back] == '@';
        }

        kept.Append('_');
        int quotes = QuoteRun(line, i);
        if (!verbatim && quotes >= 3)
        {
            frames.Push(new Frame(Mode.RawString) { Quotes = quotes, Dollars = dollars });
            return i + quotes;
        }

        frames.Push(new Frame(verbatim ? Mode.VerbatimString : Mode.RegularString)
        {
            Dollars = dollars,
        });
        return i + 1;
    }

    private static int BraceRun(string line, int start)
    {
        int end = start;
        while (end < line.Length && line[end] == '{')
        {
            end++;
        }

        return end - start;
    }

    private static char At(string line, int index) =>
        index >= 0 && index < line.Length ? line[index] : '\0';

    private static int QuoteRun(string line, int start)
    {
        int end = start;
        while (end < line.Length && line[end] == '"')
        {
            end++;
        }

        return end - start;
    }

    private sealed class Frame(Mode mode)
    {
        public Mode Mode { get; } = mode;

        public bool IsHole { get; private init; }

        public int Depth { get; set; }

        public int Quotes { get; init; }

        public int Dollars { get; init; }

        public static Frame Hole() => new(Mode.Code) { IsHole = true };
    }
}
