using System.Text;

namespace SqlArtisan.Tests;

// The shapes csharp-formatting.md names that the formatter cannot see (IDE2000 gates
// runs only): no blank line directly after `{` or before `}`, and no wrapped operator
// left at a line's end.
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

    // Each line's code with literals blanked and comments dropped, so an operator
    // inside a string or a comment never reads as a trailing one.
    private static string[] CodeOnly(string[] lines)
    {
        string[] code = new string[lines.Length];
        Mode mode = Mode.Code;
        int rawQuotes = 0;

        for (int n = 0; n < lines.Length; n++)
        {
            string line = lines[n];
            StringBuilder kept = new();
            int i = 0;
            while (i < line.Length)
            {
                char c = line[i];
                switch (mode)
                {
                    case Mode.Code when c == '/' && At(line, i + 1) == '/':
                        i = line.Length;
                        break;
                    case Mode.Code when c == '/' && At(line, i + 1) == '*':
                        mode = Mode.BlockComment;
                        i += 2;
                        break;
                    case Mode.Code when c == '"':
                        int quotes = QuoteRun(line, i);
                        if (quotes >= 3)
                        {
                            mode = Mode.RawString;
                            rawQuotes = quotes;
                            i += quotes;
                        }
                        else
                        {
                            mode = At(line, i - 1) == '@'
                                || (At(line, i - 1) == '$' && At(line, i - 2) == '@')
                                    ? Mode.VerbatimString
                                    : Mode.RegularString;
                            i++;
                        }

                        kept.Append('_');
                        break;
                    case Mode.Code when c == '\'':
                        int close = line.IndexOf('\'', i + (At(line, i + 1) == '\\' ? 3 : 2));
                        i = close < 0 ? line.Length : close + 1;
                        kept.Append('_');
                        break;
                    case Mode.Code:
                        kept.Append(c);
                        i++;
                        break;
                    case Mode.BlockComment:
                        if (c == '*' && At(line, i + 1) == '/')
                        {
                            mode = Mode.Code;
                            i += 2;
                        }
                        else
                        {
                            i++;
                        }

                        break;
                    case Mode.RegularString:
                        if (c == '"')
                        {
                            mode = Mode.Code;
                        }

                        i += c == '\\' ? 2 : 1;
                        break;
                    case Mode.VerbatimString when c == '"' && At(line, i + 1) == '"':
                        i += 2;
                        break;
                    case Mode.VerbatimString:
                        if (c == '"')
                        {
                            mode = Mode.Code;
                        }

                        i++;
                        break;
                    case Mode.RawString when c == '"' && QuoteRun(line, i) >= rawQuotes:
                        mode = Mode.Code;
                        i += QuoteRun(line, i);
                        break;
                    default:
                        i++;
                        break;
                }
            }

            // A regular string cannot span lines; an unclosed one is a malformed read.
            if (mode == Mode.RegularString)
            {
                mode = Mode.Code;
            }

            code[n] = kept.ToString();
        }

        return code;
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
}
