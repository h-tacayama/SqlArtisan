using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace SqlArtisan.TableClassGen;

internal enum TableStatus
{
    Unchanged = 0,
    Added = 1,
    Modified = 2,
    Removed = 3,
}

internal sealed class TableResult(
    string tableName,
    string path,
    TableStatus status,
    IReadOnlyList<string> changes)
{
    public string TableName => tableName;

    public string Path => path;

    public TableStatus Status => status;

    public IReadOnlyList<string> Changes => changes;

    // Rewriting a byte-identical file still bumps its mtime, which MSBuild and file
    // watchers read as a change — a regeneration would rebuild every table class.
    // The report reads this too, so a count cannot claim a write that never happened.
    public bool NeedsWrite => Status is TableStatus.Added or TableStatus.Modified;
}

// Comparison regenerates in memory and diffs against the files on disk: the
// generated classes are the only committed representation of the schema.
internal sealed class TableClassGenerator(ICatalogReader catalog, RunOptions options)
{
    // Matches the escaped literal TableClassEmitter.Quote produces, so a column
    // name carrying a quote or backslash still round-trips through the diff.
    private static readonly Regex ColumnPattern =
        new("""new DbColumn\(this, "(?<name>(?:[^"\\]|\\.)*)"\)""", RegexOptions.Compiled);

    private readonly CodeGenerationSettings _settings = options.Settings;

    private readonly TableClassEmitter _emitter = new(options.Settings);

    public IReadOnlyList<TableResult> Run()
    {
        List<TableResult> results = [];
        IReadOnlyList<CatalogTable> tables = [.. ResolveTables()];

        GuardClassNames(tables);

        // Emit everything before writing anything: Emit's per-table guard (a
        // property-name collision) must not leave earlier tables already written.
        List<(CatalogTable Table, string Code)> emitted =
            [.. tables.Select(t => (t, _emitter.Emit(t)))];

        foreach ((CatalogTable table, string code) in emitted)
        {
            string path = _settings.CreateOutputFilePath(table.ClassName);

            TableResult result = Compare(table, path, code);
            results.Add(result);

            if (ShouldWrite(result) && !options.DryRun)
            {
                WriteTableClass(path, code);
            }
        }

        results.AddRange(FindRemoved(results));

        GuardNothingToReport(results);

        return results;
    }

    // An emptied schema still reports its committed files as removed, so only a run
    // with nothing at all to say cannot tell a wrong schema name from a right one.
    private void GuardNothingToReport(IReadOnlyList<TableResult> results)
    {
        if (results.Count == 0)
        {
            throw new CommandLineException(options.Connection.EmptyCatalogMessage);
        }
    }

    // Left unguarded, the second table would overwrite the first's file and vanish
    // from the output, and --check would then report a drift no --fix could clear.
    private static void GuardClassNames(IReadOnlyList<CatalogTable> tables)
    {
        Dictionary<string, string> byClassName = new(StringComparer.Ordinal);

        foreach (CatalogTable table in tables)
        {
            if (byClassName.TryGetValue(table.ClassName, out string? first))
            {
                throw new CommandLineException(
                    $"Tables '{first}' and '{table.TableName}' both generate the class "
                        + $"{table.ClassName}; rename one of them or narrow the run with --tables.");
            }

            byClassName[table.ClassName] = table.TableName;
        }
    }

    private IEnumerable<CatalogTable> ResolveTables()
    {
        if (_settings.TableNames.Count == 0)
        {
            return catalog.GetAllTables();
        }

        List<CatalogTable> tables = [];

        // Deduplicated so `--tables orders,orders` reads twice as once instead of
        // reporting a bogus class-name collision between a table and itself.
        HashSet<string> seen = new(StringComparer.Ordinal);
        foreach (string name in _settings.TableNames)
        {
            if (!seen.Add(name))
            {
                continue;
            }

            if (!catalog.TryGetTable(name, out CatalogTable? table) || table is null)
            {
                throw new CommandLineException(
                    $"--tables names '{name}', which the schema does not contain");
            }

            tables.Add(table);
        }

        return tables;
    }

    private static void WriteTableClass(string path, string code)
    {
        string? directory = Path.GetDirectoryName(path);

        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllText(path, code);
    }

    private static TableResult Compare(CatalogTable table, string path, string code)
    {
        if (!File.Exists(path))
        {
            return new TableResult(table.TableName, path, TableStatus.Added, []);
        }

        string committed = File.ReadAllText(path);

        // Compared with line endings normalized: a checkout under a different
        // autocrlf setting is not a schema change.
        if (string.Equals(
            committed.ReplaceLineEndings("\n"),
            code.ReplaceLineEndings("\n"),
            StringComparison.Ordinal))
        {
            return new TableResult(table.TableName, path, TableStatus.Unchanged, []);
        }

        return new TableResult(table.TableName, path, TableStatus.Modified, Diff(committed, code));
    }

    private static IReadOnlyList<string> Diff(string committed, string generated)
    {
        List<string> before = ColumnNames(committed);
        List<string> after = ColumnNames(generated);

        List<string> changes =
        [
            .. after.Where(c => !before.Contains(c)).Select(c => $"+ {c}"),
            .. before.Where(c => !after.Contains(c)).Select(c => $"- {c}"),
        ];

        // Same columns, different text: metadata, ordering, or an emitter option
        // moved. Naming the columns would be misleading, so say only what is known.
        return changes.Count > 0 ? changes : ["~ column metadata or layout changed"];
    }

    private static List<string> ColumnNames(string code) =>
        [.. ColumnPattern.Matches(code).Select(m => Unescape(m.Groups["name"].Value))];

    // Reverses TableClassEmitter.Quote's escaping, so a diff names the column as it
    // reads in the database rather than as it reads inside a C# string literal.
    private static string Unescape(string literal)
    {
        StringBuilder unescaped = new(literal.Length);

        for (int i = 0; i < literal.Length; i++)
        {
            if (literal[i] != '\\')
            {
                unescaped.Append(literal[i]);
                continue;
            }

            char next = literal[++i];

            // A malformed \u — hand-edited or corrupted; Quote never emits one —
            // reads back literally rather than aborting the whole --check/--fix run.
            if (next == 'u'
                && i + 4 < literal.Length
                && int.TryParse(
                    literal.AsSpan(i + 1, 4),
                    NumberStyles.AllowHexSpecifier,
                    CultureInfo.InvariantCulture,
                    out int code))
            {
                unescaped.Append((char)code);
                i += 4;
            }
            else
            {
                unescaped.Append(next);
            }
        }

        return unescaped.ToString();
    }

    private bool ShouldWrite(TableResult result) =>
        options.Mode is RunMode.Generate or RunMode.Fix && result.NeedsWrite;

    // Only meaningful over the whole schema: a run scoped by --tables never looked
    // at the other tables, so their absence from the results proves nothing.
    private IEnumerable<TableResult> FindRemoved(IReadOnlyList<TableResult> results)
    {
        if (_settings.TableNames.Count > 0 || !Directory.Exists(_settings.OutputDirectory))
        {
            return [];
        }

        HashSet<string> expected = new(
            results.Select(r => Path.GetFullPath(r.Path)),
            StringComparer.Ordinal);

        return Directory
            .EnumerateFiles(_settings.OutputDirectory, "*.cs", SearchOption.AllDirectories)
            .Where(p => !expected.Contains(Path.GetFullPath(p)) && IsGeneratedTableClass(p))
            // Named by file, not by table: the table is gone, so its catalog name is
            // no longer knowable.
            .Select(p => new TableResult(
                Path.GetFileName(p),
                p,
                TableStatus.Removed,
                []))
            .ToList();
    }

    // Recognizing our own header keeps hand-written files in the same directory out
    // of the report — and out of any future --prune.
    private static bool IsGeneratedTableClass(string path)
    {
        try
        {
            string text = File.ReadAllText(path);
            return text.StartsWith("// <auto-generated/>", StringComparison.Ordinal)
                && text.Contains(": DbTableBase", StringComparison.Ordinal);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return false;
        }
    }
}
