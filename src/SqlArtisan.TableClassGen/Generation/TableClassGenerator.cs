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
    private readonly CodeGenerationSettings _settings = options.Settings;

    private readonly TableClassEmitter _emitter = new(options.Settings);

    public IReadOnlyList<TableResult> Run()
    {
        GuardOutputDirectory();

        IReadOnlyList<CatalogTable> tables = [.. ResolveTables()];

        GuardClassNames(tables);

        // Emit everything before writing anything: Emit's per-table guard (a
        // property-name collision) must not leave earlier tables already written.
        List<(CatalogTable Table, string Code)> emitted =
            [.. tables.Select(t => (t, _emitter.Emit(t)))];

        GuardTableNames(emitted);

        List<(TableResult Result, string Code)> compared =
        [
            .. emitted.Select(e => (
                Compare(e.Table, _settings.CreateOutputFilePath(e.Table.ClassName), e.Code),
                e.Code)),
        ];

        // Scanned before any write, for the reason Emit runs first: a scan that
        // fails must not follow a --fix that already rewrote every file.
        List<TableResult> results = [.. compared.Select(c => c.Result)];
        results.AddRange(FindRemoved(results));

        GuardNothingToReport(results);

        if (!options.DryRun)
        {
            WriteDrifted(compared);
        }

        return results;
    }

    // Read as a directory, a file there makes every table read as added and
    // the first write fail naming the file rather than the flag.
    private void GuardOutputDirectory()
    {
        if (File.Exists(_settings.OutputDirectory))
        {
            throw new CommandLineException(
                $"--output names a file, not a directory: {_settings.OutputDirectory}");
        }
    }

    // A write can still fail midway (a full disk, a file locked by an editor);
    // the files already rewritten are named, since nothing else reports them.
    private void WriteDrifted(IReadOnlyList<(TableResult Result, string Code)> compared)
    {
        List<string> written = [];

        foreach ((TableResult result, string code) in compared.Where(c => ShouldWrite(c.Result)))
        {
            try
            {
                WriteTableClass(result.Path, code);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                string after = written.Count == 0
                    ? string.Empty
                    : $" after writing {string.Join(", ", written)}";

                throw new CommandLineException(
                    $"Cannot write {result.Path} under --output{after} ({ex.Message})");
            }

            written.Add(result.Path);
        }
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
        // Case-insensitive: the file name is what collides, and the common
        // filesystems fold case.
        Dictionary<string, CatalogTable> byClassName = new(StringComparer.OrdinalIgnoreCase);

        foreach (CatalogTable table in tables)
        {
            if (byClassName.TryGetValue(table.ClassName, out CatalogTable? first))
            {
                throw new CommandLineException(CollisionMessage(first, table));
            }

            byClassName[table.ClassName] = table;
        }
    }

    // A case-only pair generates two distinct classes, so saying they "both
    // generate" one of them would send the reader after a name that is not the
    // constraint: the file names are what collide on a case-folding file system.
    private static string CollisionMessage(CatalogTable first, CatalogTable second)
    {
        string remedy = "rename one of them or narrow the run with --tables";

        return string.Equals(first.ClassName, second.ClassName, StringComparison.Ordinal)
            ? $"Tables '{first.TableName}' and '{second.TableName}' both generate the class "
                + $"{second.ClassName}; {remedy}"
            : $"Tables '{first.TableName}' and '{second.TableName}' generate the classes "
                + $"{first.ClassName} and {second.ClassName}, whose file names differ only by "
                + $"case and collide on a case-folding file system; {remedy}";
    }

    private IEnumerable<CatalogTable> ResolveTables()
    {
        if (_settings.TableNames.Count == 0)
        {
            return catalog.GetAllTables();
        }

        List<CatalogTable> tables = [];

        // By the name given, so `orders,orders` is no collision with itself; a
        // --lowercase TableName would instead merge two distinct tables silently.
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
                    $"--tables names '{name}', which is not a table in the schema");
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

        string committed = ReadCommitted(path);

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

    // Before any write, for the reason Emit runs first: a multi-table run must
    // not rewrite earlier files and then abort on a later file's collision.
    private void GuardTableNames(IReadOnlyList<(CatalogTable Table, string Code)> emitted)
    {
        foreach ((CatalogTable table, string code) in emitted)
        {
            string path = _settings.CreateOutputFilePath(table.ClassName);

            if (File.Exists(path))
            {
                GuardTableName(path, ReadCommitted(path), code);
            }
        }
    }

    // Two tables can share a class name across --tables runs, where GuardClassNames
    // sees only one of them; the file on disk then names the other. A file whose
    // table it cannot read is refused: passing it let a run overwrite another's.
    private static void GuardTableName(string path, string committed, string generated)
    {
        string current = CommittedFile.TableName(generated)!;

        if (CommittedFile.TableName(committed) is not { } existing)
        {
            throw new CommandLineException(
                $"{Path.GetFileName(path)} is where this run writes '{current}', but no table "
                    + "the tool can read is named in it; move the file, or generate into "
                    + "another --output");
        }

        if (!NameOneTable(existing, current))
        {
            throw new CommandLineException(
                $"{Path.GetFileName(path)} already describes table '{existing}', and this run "
                    + $"generates '{current}' into it; generate the two into separate --output "
                    + "directories, or rename one of the tables");
        }
    }

    // The committed file carries the emitted literal, which --lowercase and
    // --qualify-schema each vary, so compare the identity it spells rather than
    // its text: a schema counts only where both sides carry one.
    private static bool NameOneTable(string existing, string current)
    {
        (string existingSchema, string existingName) = SplitQualified(existing);
        (string currentSchema, string currentName) = SplitQualified(current);

        if (!string.Equals(existingName, currentName, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return existingSchema.Length == 0
            || currentSchema.Length == 0
            || string.Equals(existingSchema, currentSchema, StringComparison.OrdinalIgnoreCase);
    }

    private static (string Schema, string Name) SplitQualified(string tableName)
    {
        int separator = tableName.LastIndexOf('.');

        return separator < 0
            ? (string.Empty, tableName)
            : (tableName[..separator], tableName[(separator + 1)..]);
    }

    private static IReadOnlyList<string> Diff(string committed, string generated)
    {
        List<string> before = CommittedFile.ColumnNames(committed);
        List<string> after = CommittedFile.ColumnNames(generated);

        List<string> changes =
        [
            .. after.Where(c => !before.Contains(c)).Select(c => $"+ {c}"),
            .. before.Where(c => !after.Contains(c)).Select(c => $"- {c}"),
        ];

        // Same columns, different text: metadata, ordering, or an emitter option
        // moved. Naming the columns would be misleading, so say only what is known.
        return changes.Count > 0 ? changes : ["~ column metadata or layout changed"];
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
        string[] files = ListSourceFiles();
        HashSet<string> listed = new(files.Select(Path.GetFullPath), StringComparer.Ordinal);

        return files
            .Where(p => !expected.Contains(Path.GetFullPath(p))
                && !IsListedUnderAnotherCase(Path.GetFullPath(p), expected, listed, File.Exists)
                && IsOwnGeneratedFile(p))
            .Order(StringComparer.Ordinal)
            // Named by file, not by table: the table is gone, so its catalog name is
            // no longer knowable.
            .Select(p => new TableResult(Path.GetFileName(p), p, TableStatus.Removed, []))
            .ToList();
    }

    private string[] ListSourceFiles()
    {
        try
        {
            return Directory.GetFiles(
                _settings.OutputDirectory,
                "*.cs",
                SearchOption.AllDirectories);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // A subdirectory the scan cannot enter may hold an orphan, so skipping it
            // would report in sync over a file the run never looked at.
            throw new CommandLineException(
                $"Cannot scan --output {_settings.OutputDirectory} for orphan files "
                    + $"({ex.Message})");
        }
    }

    // A case-folding file system keeps an overwritten entry's old spelling, so an
    // expected path that exists yet does not list is the listed file under another
    // case; on a case-sensitive file system an existing path always lists.
    internal static bool IsListedUnderAnotherCase(
        string listedPath,
        IReadOnlySet<string> expected,
        IReadOnlySet<string> listed,
        Func<string, bool> exists) =>
        expected.Any(e => string.Equals(e, listedPath, StringComparison.OrdinalIgnoreCase)
            && !listed.Contains(e)
            && exists(e));

    // The generated header keeps hand-written files out of the report, and the
    // namespace keeps out another generation's files in a nested --output.
    private bool IsOwnGeneratedFile(string path)
    {
        string text = ReadCommitted(path);

        return CommittedFile.IsGenerated(text)
            && CommittedFile.IsInNamespace(text, _settings.OutputNamespace);
    }

    // A file the run cannot read may be one of its own, so it fails the run rather
    // than reading as absent, which reported an unreadable orphan as in sync.
    private static string ReadCommitted(string path)
    {
        try
        {
            return File.ReadAllText(path);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            throw new CommandLineException($"Cannot read {path} under --output ({ex.Message})");
        }
    }
}
