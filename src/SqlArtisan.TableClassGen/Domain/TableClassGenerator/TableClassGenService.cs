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
}

/// <summary>
/// Generates, or compares against, the committed table classes. Comparison
/// regenerates in memory and diffs against the files on disk — the classes are the
/// only committed representation of the schema.
/// </summary>
internal sealed class TableClassGenService(ITableInfoRepository repository, RunOptions options)
{
    private static readonly Regex ColumnPattern =
        new("""new DbColumn\(this, "(?<name>[^"]*)"\)""", RegexOptions.Compiled);

    private readonly CodeGenerationSettings _settings = options.Settings;

    public IReadOnlyList<TableResult> Run()
    {
        List<TableResult> results = [];

        foreach (DbTableInfo table in ResolveTables())
        {
            string code = table.GenerateCode(_settings);
            string path = _settings.CreateOutputFilePath(table.ClassName);

            TableResult result = Compare(table, path, code);
            results.Add(result);

            if (ShouldWrite(result.Status) && !options.DryRun)
            {
                WriteTableClass(path, code);
            }
        }

        results.AddRange(FindRemoved(results));

        return results;
    }

    private IEnumerable<DbTableInfo> ResolveTables()
    {
        if (_settings.TableNames.Count == 0)
        {
            return repository.GetAllTables();
        }

        List<DbTableInfo> tables = [];

        foreach (string name in _settings.TableNames)
        {
            if (!repository.TryGetTableInfo(name, out DbTableInfo? table) || table is null)
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

    private static TableResult Compare(DbTableInfo table, string path, string code)
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
        [.. ColumnPattern.Matches(code).Select(m => m.Groups["name"].Value)];

    private bool ShouldWrite(TableStatus status) =>
        options.Mode switch
        {
            RunMode.Generate => true,
            RunMode.Fix => status is TableStatus.Added or TableStatus.Modified,
            _ => false,
        };

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
        catch (IOException)
        {
            return false;
        }
    }
}
