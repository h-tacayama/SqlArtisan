using System.Text.RegularExpressions;

namespace SqlArtisan.IntegrationTests.Randomized;

/// <summary>Reads the subset of PICT's model language the <c>Models/</c> files use, so the DB-less
/// gate can check the CSV without PICT. A construct outside the subset throws rather than
/// reading as true.</summary>
internal sealed partial class PictModel
{
    private PictModel(
        List<(string Name, string[] Levels)> factors,
        List<(string[] Factors, int Order)> subModels,
        List<Constraint> constraints)
    {
        Factors = factors;
        SubModels = subModels;
        Constraints = constraints;
    }

    public IReadOnlyList<(string Name, string[] Levels)> Factors { get; }

    public IReadOnlyList<(string[] Factors, int Order)> SubModels { get; }

    public IReadOnlyList<Constraint> Constraints { get; }

    public static PictModel Parse(string text)
    {
        List<(string, string[])> factors = [];
        List<(string[], int)> subModels = [];
        List<string> constraintLines = [];
        foreach (string raw in text.Split('\n'))
        {
            string line = raw.Trim();
            if (line.Length == 0 || line.StartsWith('#'))
            {
                continue;
            }

            if (constraintLines.Count > 0 || line.StartsWith("IF ", StringComparison.Ordinal))
            {
                constraintLines.Add(line);
            }
            else if (SubModelPattern().Match(line) is { Success: true } sub)
            {
                subModels.Add((
                    [.. sub.Groups[1].Value.Split(',').Select(f => f.Trim())],
                    int.Parse(sub.Groups[2].Value)));
            }
            else
            {
                int colon = line.IndexOf(':');
                factors.Add((
                    line[..colon].Trim(),
                    [.. line[(colon + 1)..].Split(',').Select(v => v.Trim())]));
            }
        }

        List<Constraint> constraints = [.. string.Join(" ", constraintLines)
            .Split(';')
            .Select(s => s.Trim())
            .Where(s => s.Length > 0)
            .Select(s => new Constraint(s, new Parser(Tokenize(s)).Statement()))];
        return new PictModel(factors, subModels, constraints);
    }

    /// <summary>Whether some full row that every constraint allows holds
    /// <paramref name="fixedLevels"/>.</summary>
    public bool IsSatisfiable(IReadOnlyDictionary<string, string> fixedLevels)
    {
        Dictionary<string, string> assignment = new(fixedLevels);
        return Extend(assignment, 0);
    }

    private bool Extend(Dictionary<string, string> assignment, int factor)
    {
        if (Constraints.Any(c => c.Holds(assignment) == false))
        {
            return false;
        }

        if (factor == Factors.Count)
        {
            return true;
        }

        (string name, string[] levels) = Factors[factor];
        if (assignment.ContainsKey(name))
        {
            return Extend(assignment, factor + 1);
        }

        foreach (string level in levels)
        {
            assignment[name] = level;
            if (Extend(assignment, factor + 1))
            {
                assignment.Remove(name);
                return true;
            }
        }

        assignment.Remove(name);
        return false;
    }

    [GeneratedRegex(@"^\{(.*)\}\s*@\s*(\d+)$")]
    private static partial Regex SubModelPattern();

    [GeneratedRegex("""\[[^\]]+\]|"[^"]*"|<>|[=(){},]|[A-Za-z]+""")]
    private static partial Regex TokenPattern();

    private static List<string> Tokenize(string text) =>
        [.. TokenPattern().Matches(text).Select(m => m.Value)];

    /// <summary>One model constraint, three-valued over a partial row: <see langword="null"/>
    /// while a factor it reads is unassigned.</summary>
    internal sealed record Constraint(
        string Text,
        Func<IReadOnlyDictionary<string, string>, bool?> Holds);

    private sealed class Parser(List<string> tokens)
    {
        private int _next;

        public Func<IReadOnlyDictionary<string, string>, bool?> Statement()
        {
            if (Peek() != "IF")
            {
                Func<IReadOnlyDictionary<string, string>, bool?> rule = Or();
                End();
                return rule;
            }

            _next++;
            Func<IReadOnlyDictionary<string, string>, bool?> condition = Or();
            Expect("THEN");
            Func<IReadOnlyDictionary<string, string>, bool?> consequence = Or();
            End();
            return row => Implies(condition(row), consequence(row));
        }

        private Func<IReadOnlyDictionary<string, string>, bool?> Or()
        {
            Func<IReadOnlyDictionary<string, string>, bool?> left = And();
            while (Peek() == "OR")
            {
                _next++;
                Func<IReadOnlyDictionary<string, string>, bool?> a = left;
                Func<IReadOnlyDictionary<string, string>, bool?> b = And();
                left = row => a(row) == true || b(row) == true
                    ? true
                    : a(row) == false && b(row) == false ? false : null;
            }

            return left;
        }

        private Func<IReadOnlyDictionary<string, string>, bool?> And()
        {
            Func<IReadOnlyDictionary<string, string>, bool?> left = Unary();
            while (Peek() == "AND")
            {
                _next++;
                Func<IReadOnlyDictionary<string, string>, bool?> a = left;
                Func<IReadOnlyDictionary<string, string>, bool?> b = Unary();
                left = row => a(row) == false || b(row) == false
                    ? false
                    : a(row) == true && b(row) == true ? true : null;
            }

            return left;
        }

        private Func<IReadOnlyDictionary<string, string>, bool?> Unary()
        {
            if (Peek() == "NOT")
            {
                _next++;
                Func<IReadOnlyDictionary<string, string>, bool?> operand = Unary();
                return row => !operand(row);
            }

            if (Peek() == "(")
            {
                _next++;
                Func<IReadOnlyDictionary<string, string>, bool?> inner = Or();
                Expect(")");
                return inner;
            }

            string factor = Take();
            if (!factor.StartsWith('['))
            {
                throw new FormatException($"Expected a [factor], found '{factor}'.");
            }

            factor = factor[1..^1];
            string op = Take();
            HashSet<string> values = [];
            switch (op)
            {
                case "=" or "<>":
                    values.Add(Literal(Take()));
                    break;
                case "IN":
                    Expect("{");
                    do
                    {
                        values.Add(Literal(Take()));
                    }
                    while (Take() == ",");

                    _next--;
                    Expect("}");
                    break;
                default:
                    throw new FormatException($"Unsupported PICT operator '{op}'.");
            }

            bool negated = op == "<>";
            return row => row.TryGetValue(factor, out string? level)
                ? values.Contains(level) != negated
                : null;
        }

        private static bool? Implies(bool? condition, bool? consequence) =>
            condition == false || consequence == true
                ? true
                : condition == true && consequence == false ? false : null;

        private static string Literal(string token) => token.StartsWith('"')
            ? token[1..^1]
            : throw new FormatException($"Expected a quoted value, found '{token}'.");

        private string? Peek() => _next < tokens.Count ? tokens[_next] : null;

        private string Take() => _next < tokens.Count
            ? tokens[_next++]
            : throw new FormatException("The constraint ends early.");

        private void Expect(string token)
        {
            if (Take() != token)
            {
                throw new FormatException($"Expected '{token}' in the constraint.");
            }
        }

        private void End()
        {
            if (Peek() is { } extra)
            {
                throw new FormatException($"Unexpected '{extra}' after the constraint.");
            }
        }
    }
}
