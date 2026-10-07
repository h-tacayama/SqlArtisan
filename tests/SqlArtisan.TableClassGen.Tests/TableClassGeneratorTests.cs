using SqlArtisan.TableClassGen;

namespace SqlArtisan.TableClassGen.Tests;

// The drift lane end to end: generate against a live (SQLite) schema, then let the
// schema and the committed files diverge and assert what --check reports.
public class TableClassGeneratorTests : IDisposable
{
    private const string Schema =
        """
        CREATE TABLE item (id INTEGER PRIMARY KEY, code TEXT NOT NULL);
        CREATE TABLE tag (id INTEGER PRIMARY KEY, label TEXT);
        """;

    private readonly string _outputDirectory = Path.Combine(
        Path.GetTempPath(),
        $"sqlartisan_tcg_out_{Guid.NewGuid():N}");

    public void Dispose()
    {
        if (Directory.Exists(_outputDirectory))
        {
            Directory.Delete(_outputDirectory, recursive: true);
        }
    }

    [Fact]
    public void Run_Generate_WritesEveryTableThenChecksClean()
    {
        using TempSqliteDatabase db = TempSqliteDatabase.Create(Schema);

        IReadOnlyList<TableResult> generated = Run(db, RunMode.Generate);

        Assert.Equal([TableStatus.Added, TableStatus.Added], generated.Select(r => r.Status));
        Assert.All(generated, r => Assert.True(File.Exists(r.Path)));

        Assert.All(Run(db, RunMode.Check), r => Assert.Equal(TableStatus.Unchanged, r.Status));
    }

    [Fact]
    public void Run_DryRun_WritesNothing()
    {
        using TempSqliteDatabase db = TempSqliteDatabase.Create(Schema);

        IReadOnlyList<TableResult> results = Run(db, RunMode.Generate, dryRun: true);

        Assert.All(results, r => Assert.False(File.Exists(r.Path)));
    }

    [Fact]
    public void Run_Check_AddedColumn_ReportsModifiedWithTheColumn()
    {
        using TempSqliteDatabase db = TempSqliteDatabase.Create(Schema);
        Run(db, RunMode.Generate);

        db.Execute("ALTER TABLE item ADD COLUMN note TEXT");

        TableResult item = Single(Run(db, RunMode.Check), "item");

        Assert.Equal(TableStatus.Modified, item.Status);
        Assert.Equal(["+ note"], item.Changes);
    }

    // The emitter escapes a quote in a column name to keep the C# literal valid
    // (TableClassEmitter.Quote); the diff regex has to match that escaped form, not
    // just a plain name, or the column silently disappears from the reported diff.
    [Fact]
    public void Run_Check_AddedColumnWithQuoteInName_ReportsModifiedWithTheColumn()
    {
        using TempSqliteDatabase db = TempSqliteDatabase.Create(Schema);
        Run(db, RunMode.Generate);

        db.Execute("""ALTER TABLE item ADD COLUMN "a""b" TEXT""");

        TableResult item = Single(Run(db, RunMode.Check), "item");

        Assert.Equal(TableStatus.Modified, item.Status);
        Assert.Equal(["+ a\"b"], item.Changes);
    }

    // Quote never emits a short or non-hex \u — only a hand-edited or corrupted
    // committed file can carry one. It must read back literally rather than abort
    // the whole run over one file.
    [Fact]
    public void Run_Check_CommittedFileHasMalformedUnicodeEscape_ReportsDriftInsteadOfThrowing()
    {
        using TempSqliteDatabase db = TempSqliteDatabase.Create(Schema);
        Run(db, RunMode.Generate);

        string itemPath = Path.Combine(_outputDirectory, "ItemTable.cs");
        File.WriteAllText(
            itemPath,
            File.ReadAllText(itemPath).Replace(
                "DbColumn(this, \"code\")", "DbColumn(this, \"co\\uAB\")"));

        TableResult item = Single(Run(db, RunMode.Check), "item");

        Assert.Equal(TableStatus.Modified, item.Status);
        Assert.Equal(["+ code", "- couAB"], item.Changes);
    }

    [Fact]
    public void Run_Check_MetadataOnlyChange_ReportsModifiedWithoutNamingColumns()
    {
        using TempSqliteDatabase db = TempSqliteDatabase.Create(
            "CREATE TABLE item (id INTEGER PRIMARY KEY, code TEXT);");
        Run(db, RunMode.Generate);

        // Same columns, different nullability: only the emitted metadata moves.
        db.Execute(
            """
            CREATE TABLE item_new (id INTEGER PRIMARY KEY, code TEXT NOT NULL);
            DROP TABLE item;
            ALTER TABLE item_new RENAME TO item;
            """);

        TableResult item = Single(Run(db, RunMode.Check), "item");

        Assert.Equal(TableStatus.Modified, item.Status);
        Assert.Equal(["~ column metadata or layout changed"], item.Changes);
    }

    [Fact]
    public void Run_Check_MissingFile_ReportsAdded()
    {
        using TempSqliteDatabase db = TempSqliteDatabase.Create(Schema);
        Run(db, RunMode.Generate);

        File.Delete(Path.Combine(_outputDirectory, "ItemTable.cs"));

        Assert.Equal(TableStatus.Added, Single(Run(db, RunMode.Check), "item").Status);
    }

    [Fact]
    public void Run_Check_DroppedTable_ReportsRemoved()
    {
        using TempSqliteDatabase db = TempSqliteDatabase.Create(Schema);
        Run(db, RunMode.Generate);

        db.Execute("DROP TABLE tag");

        TableResult removed = Assert.Single(
            Run(db, RunMode.Check).Where(r => r.Status == TableStatus.Removed));

        Assert.Equal("TagTable.cs", removed.TableName);
    }

    [Fact]
    public void Run_Check_HandWrittenFile_IsNotReportedAsRemoved()
    {
        using TempSqliteDatabase db = TempSqliteDatabase.Create(Schema);
        Run(db, RunMode.Generate);

        File.WriteAllText(
            Path.Combine(_outputDirectory, "Helpers.cs"),
            "namespace Generated.Tables;\n\ninternal static class Helpers { }\n");

        Assert.DoesNotContain(Run(db, RunMode.Check), r => r.Status == TableStatus.Removed);
    }

    [Fact]
    public void Run_Fix_RewritesTheDriftedFileOnly()
    {
        using TempSqliteDatabase db = TempSqliteDatabase.Create(Schema);
        Run(db, RunMode.Generate);

        string tagPath = Path.Combine(_outputDirectory, "TagTable.cs");
        DateTime tagWrittenAt = File.GetLastWriteTimeUtc(tagPath);
        db.Execute("ALTER TABLE item ADD COLUMN note TEXT");

        Run(db, RunMode.Fix);

        Assert.All(Run(db, RunMode.Check), r => Assert.Equal(TableStatus.Unchanged, r.Status));
        Assert.Equal(tagWrittenAt, File.GetLastWriteTimeUtc(tagPath));
    }

    // A plain regeneration used to rewrite every file, byte-identical ones included,
    // so the whole output directory looked new to MSBuild and to file watchers.
    [Fact]
    public void Run_Generate_UnchangedTable_LeavesItsFileUntouched()
    {
        using TempSqliteDatabase db = TempSqliteDatabase.Create(Schema);
        Run(db, RunMode.Generate);

        string tagPath = Path.Combine(_outputDirectory, "TagTable.cs");
        DateTime tagWrittenAt = File.GetLastWriteTimeUtc(tagPath);
        db.Execute("ALTER TABLE item ADD COLUMN note TEXT");

        Run(db, RunMode.Generate);

        Assert.Equal(tagWrittenAt, File.GetLastWriteTimeUtc(tagPath));
        // The legal twin: skipping the unchanged file must not skip the changed one.
        Assert.Contains(
            "note",
            File.ReadAllText(Path.Combine(_outputDirectory, "ItemTable.cs")),
            StringComparison.Ordinal);
    }

    [Fact]
    public void Run_Tables_LimitsTheRunToTheNamedTables()
    {
        using TempSqliteDatabase db = TempSqliteDatabase.Create(Schema);

        IReadOnlyList<TableResult> results = Run(db, RunMode.Generate, tableNames: ["item"]);

        TableResult only = Assert.Single(results);
        Assert.Equal("item", only.TableName);
        Assert.False(File.Exists(Path.Combine(_outputDirectory, "TagTable.cs")));
    }

    // A scoped run never looked at the other tables, so it must not conclude their
    // files are orphans.
    [Fact]
    public void Run_Tables_DoesNotReportRemovedFiles()
    {
        using TempSqliteDatabase db = TempSqliteDatabase.Create(Schema);
        Run(db, RunMode.Generate);
        db.Execute("DROP TABLE tag");

        Assert.DoesNotContain(
            Run(db, RunMode.Check, tableNames: ["item"]),
            r => r.Status == TableStatus.Removed);
    }

    [Fact]
    public void Run_UnknownTable_ThrowsCommandLineException()
    {
        using TempSqliteDatabase db = TempSqliteDatabase.Create(Schema);

        CommandLineException ex = Assert.Throws<CommandLineException>(
            () => Run(db, RunMode.Generate, tableNames: ["nope"]));

        Assert.Equal("--tables names 'nope', which is not a table in the schema", ex.Message);
    }

    // pragma_table_info answers for these too, but the full run never lists them,
    // so a class generated from one read as removed on every later full --check.
    [Theory]
    [InlineData("item_view")]
    [InlineData("sqlite_sequence")]
    public void Run_TablesNamesAnObjectTheFullRunExcludes_ThrowsCommandLineException(
        string name)
    {
        using TempSqliteDatabase db = TempSqliteDatabase.Create(
            Schema
                + "CREATE TABLE counter (id INTEGER PRIMARY KEY AUTOINCREMENT);"
                + "CREATE VIEW item_view AS SELECT id FROM item;");

        CommandLineException ex = Assert.Throws<CommandLineException>(
            () => Run(db, RunMode.Generate, tableNames: [name]));

        Assert.Equal($"--tables names '{name}', which is not a table in the schema", ex.Message);
        Assert.False(Directory.Exists(_outputDirectory));
    }

    // SQLite folds identifier case, so the user's spelling used to reach the
    // emitted literal: `--tables ITEM` wrote a file the full run reported modified.
    [Fact]
    public void Run_TablesInAnotherCase_WritesWhatTheFullRunWrites()
    {
        using TempSqliteDatabase db = TempSqliteDatabase.Create(Schema);
        Run(db, RunMode.Generate);

        IReadOnlyList<TableResult> results = Run(db, RunMode.Check, tableNames: ["ITEM"]);

        TableResult item = Assert.Single(results);
        Assert.Equal("item", item.TableName);
        Assert.Equal(TableStatus.Unchanged, item.Status);
    }

    // Deduplicating by the lowercased name merged two distinct tables, so the run
    // generated one of the two it was asked for and exited 0.
    [Fact]
    public void Run_LowercaseTablesNamingTwoTablesWithOneLoweredName_ThrowsCommandLineException()
    {
        using TempSqliteDatabase db = TempSqliteDatabase.Create(
            "CREATE TABLE \"Ä\" (id INTEGER, a TEXT); CREATE TABLE \"ä\" (id INTEGER, b TEXT);");

        Assert.Throws<CommandLineException>(
            () => Run(db, RunMode.Generate, tableNames: ["Ä", "ä"], lowercaseNames: true));
        Assert.False(Directory.Exists(_outputDirectory));
    }

    // Read as a directory, a file made every table read as added and the first
    // write fail naming the file rather than the flag.
    [Fact]
    public void Run_OutputNamesAFile_ThrowsCommandLineException()
    {
        using TempSqliteDatabase db = TempSqliteDatabase.Create(Schema);
        using TempFile file = TempFile.Create("{}");

        CommandLineException ex = Assert.Throws<CommandLineException>(
            () => RunInto(db, RunMode.Check, file.Path));

        Assert.Equal($"--output names a file, not a directory: {file.Path}", ex.Message);
    }

    // A directory squatting on the second table's file name fails its write after
    // the first file is rewritten, which the message must name.
    [Fact]
    public void Run_WriteFailsAfterAnEarlierWrite_NamesTheFilesAlreadyWritten()
    {
        using TempSqliteDatabase db = TempSqliteDatabase.Create(Schema);
        string blocked = Path.Combine(_outputDirectory, "TagTable.cs");
        Directory.CreateDirectory(blocked);

        CommandLineException ex = Assert.Throws<CommandLineException>(
            () => Run(db, RunMode.Generate));

        string written = Path.Combine(_outputDirectory, "ItemTable.cs");
        Assert.StartsWith(
            $"Cannot write {blocked} under --output after writing {written} (",
            ex.Message,
            StringComparison.Ordinal);
        Assert.True(File.Exists(written));
    }

    // The scan used to run after the writes, so a subdirectory it could not enter
    // aborted a --fix that had already rewritten every file, with nothing reported.
    [Fact]
    public void Run_FixWithUnreadableSubdirectory_FailsBeforeAnyWrite()
    {
        // File modes do not bind root, and Windows has none.
        if (OperatingSystem.IsWindows() || Environment.UserName == "root")
        {
            return;
        }

        using TempSqliteDatabase db = TempSqliteDatabase.Create(Schema);
        Run(db, RunMode.Generate);
        db.Execute("ALTER TABLE item ADD COLUMN note TEXT");

        string locked = Path.Combine(_outputDirectory, "Locked");
        Directory.CreateDirectory(locked);
        File.SetUnixFileMode(locked, UnixFileMode.None);
        string item = Path.Combine(_outputDirectory, "ItemTable.cs");
        string before = File.ReadAllText(item);

        try
        {
            CommandLineException ex = Assert.Throws<CommandLineException>(
                () => Run(db, RunMode.Fix));

            Assert.StartsWith(
                $"Cannot scan --output {_outputDirectory} for orphan files (",
                ex.Message,
                StringComparison.Ordinal);
            Assert.Equal(before, File.ReadAllText(item));
        }
        finally
        {
            File.SetUnixFileMode(locked, UnixFileMode.UserRead | UnixFileMode.UserWrite
                | UnixFileMode.UserExecute);
        }
    }

    // Unguarded, the second table overwrote the first's file and disappeared,
    // leaving a drift that --fix could not clear.
    [Fact]
    public void Run_TwoTablesWithOneClassName_ThrowsCommandLineException()
    {
        using TempSqliteDatabase db = TempSqliteDatabase.Create(
            """
            CREATE TABLE dupe_class (id INTEGER);
            CREATE TABLE dupe__class (id INTEGER);
            """);

        CommandLineException ex = Assert.Throws<CommandLineException>(
            () => Run(db, RunMode.Generate));

        Assert.Equal(
            "Tables 'dupe__class' and 'dupe_class' both generate the class DupeClassTable; "
                + "rename one of them or narrow the run with --tables.",
            ex.Message);
    }

    // Unreadable, a file may be one of the tool's own orphans, so it fails the run
    // naming the file rather than reading as absent: skipped, an orphan the run
    // could not read reported in sync (#645, decided over the earlier skip).
    [Fact]
    public void Run_Check_UnreadableFileInOutputDirectory_FailsNamingTheFile()
    {
        // File modes do not bind root, and Windows has none.
        if (OperatingSystem.IsWindows() || Environment.UserName == "root")
        {
            return;
        }

        using TempSqliteDatabase db = TempSqliteDatabase.Create(Schema);
        Run(db, RunMode.Generate);

        string locked = Path.Combine(_outputDirectory, "Locked.cs");
        File.WriteAllText(locked, "not a generated file");
        File.SetUnixFileMode(locked, UnixFileMode.None);

        try
        {
            CommandLineException ex = Assert.Throws<CommandLineException>(
                () => Run(db, RunMode.Check));

            Assert.StartsWith(
                $"Cannot read {locked} under --output (",
                ex.Message,
                StringComparison.Ordinal);
        }
        finally
        {
            File.SetUnixFileMode(locked, UnixFileMode.UserRead | UnixFileMode.UserWrite);
        }
    }

    // A literal the reader cannot match (wrapped by a formatter, say) used to pass
    // the guard, so a --tables run overwrote another table's file and exited 0.
    [Theory]
    [InlineData("base(\n        \"order_item\", tableAlias)")]
    [InlineData(null)]
    public void Run_CommittedFileWithNoReadableTable_IsRefused(string? baseCall)
    {
        using TempSqliteDatabase db = TempSqliteDatabase.Create(
            """
            CREATE TABLE order_item (id INTEGER PRIMARY KEY);
            CREATE TABLE orderItem (code TEXT PRIMARY KEY);
            """);
        Run(db, RunMode.Generate, tableNames: ["order_item"]);
        string path = Path.Combine(_outputDirectory, "OrderItemTable.cs");
        string committed = baseCall is null
            ? "// hand-written\npublic sealed class OrderItemTable { }\n"
            : File.ReadAllText(path).Replace(
                "base(\"order_item\", tableAlias)", baseCall, StringComparison.Ordinal);
        File.WriteAllText(path, committed);

        CommandLineException ex = Assert.Throws<CommandLineException>(
            () => Run(db, RunMode.Generate, tableNames: ["orderItem"]));

        Assert.Equal(
            "OrderItemTable.cs is where this run writes 'orderItem', but no table the tool "
                + "can read is named in it; move the file, or generate into another --output",
            ex.Message);
        Assert.Equal(committed, File.ReadAllText(path));
    }

    // 0.7 and earlier wrote no header, so the guard reads the table literal alone:
    // requiring the header would refuse every pre-0.8 file a regeneration replaces.
    [Fact]
    public void Run_OverAPre0_8FileOfTheSameTable_RegeneratesIt()
    {
        using TempSqliteDatabase db = TempSqliteDatabase.Create(Schema);
        Directory.CreateDirectory(_outputDirectory);
        string path = Path.Combine(_outputDirectory, "ItemTable.cs");
        File.WriteAllText(
            path,
            """
            using SqlArtisan;

            namespace Generated.Tables;

            internal sealed class ItemTable : DbTableBase
            {
            	public ItemTable(string tableAlias = "") : base("item", tableAlias)
            	{
            		Id = new DbColumn(tableAlias, "id");
            	}

            	public DbColumn Id { get; }
            }
            """);

        TableResult item = Single(Run(db, RunMode.Generate), "item");

        Assert.Equal(TableStatus.Modified, item.Status);
        Assert.Contains("global::SqlArtisan.DbTableBase", File.ReadAllText(path));
    }

    // A license header a tool prepends used to hide the file from the orphan scan,
    // which read the generated header only at offset zero.
    [Theory]
    [InlineData("// Copyright (c) Example\n\n")]
    [InlineData("/*\n * Copyright (c) Example\n */\n\n")]
    public void Run_Check_OrphanWithAPrependedComment_IsReportedRemoved(string license)
    {
        using TempSqliteDatabase db = TempSqliteDatabase.Create(Schema);
        Run(db, RunMode.Generate);
        string tag = Path.Combine(_outputDirectory, "TagTable.cs");
        File.WriteAllText(tag, license + File.ReadAllText(tag));
        db.Execute("DROP TABLE tag");

        TableResult removed = Assert.Single(
            Run(db, RunMode.Check), r => r.Status == TableStatus.Removed);

        Assert.Equal("TagTable.cs", removed.TableName);
    }

    // The emitter copies --namespace as given, so the scan compared `App.Tables `
    // with the file's `App.Tables` and reported a dropped table's file in sync.
    [Fact]
    public void Run_Check_NamespaceWithStrayWhitespace_StillFindsTheOrphan()
    {
        using TempSqliteDatabase db = TempSqliteDatabase.Create(Schema);
        RunOptions options = new(
            RunMode.Generate,
            db.ConnectionInfo,
            TestSettings.Create(outputNamespace: "App.Tables ", outputDirectory: _outputDirectory));
        ICatalogReader catalog = CatalogReaderFactory.Create(db.ConnectionInfo, false);
        new TableClassGenerator(catalog, options).Run();
        db.Execute("DROP TABLE tag");

        IReadOnlyList<TableResult> results = new TableClassGenerator(
            catalog,
            new RunOptions(RunMode.Check, db.ConnectionInfo, options.Settings)).Run();

        Assert.Single(results, r => r.Status == TableStatus.Removed);
    }

    // The tool's own remedy for a class-name clash, separate --output directories,
    // nests easily; the scan then claimed the inner generation's files for good.
    [Fact]
    public void Run_Check_AnotherNamespacesFilesInANestedDirectory_AreNotOrphans()
    {
        using TempSqliteDatabase other = TempSqliteDatabase.Create(
            "CREATE TABLE audit (id INTEGER PRIMARY KEY);");
        new TableClassGenerator(
            CatalogReaderFactory.Create(other.ConnectionInfo, lowercaseNames: false),
            new RunOptions(
                RunMode.Generate,
                other.ConnectionInfo,
                TestSettings.Create(
                    outputNamespace: "Generated.Tables.Audit",
                    outputDirectory: Path.Combine(_outputDirectory, "Audit")))).Run();
        using TempSqliteDatabase db = TempSqliteDatabase.Create(Schema);
        Run(db, RunMode.Generate);

        Assert.All(Run(db, RunMode.Check), r => Assert.Equal(TableStatus.Unchanged, r.Status));
    }

    // No case-folding file system in CI, so the decision is pinned on its inputs:
    // the regenerated file lists under its old spelling while the computed path
    // exists without listing; on a case-sensitive one both spellings list.
    [Theory]
    [InlineData(new[] { "/o/APIKeysTable.cs" }, true, true)]
    [InlineData(new[] { "/o/APIKeysTable.cs", "/o/ApikeysTable.cs" }, true, false)]
    [InlineData(new[] { "/o/APIKeysTable.cs" }, false, false)]
    public void IsListedUnderAnotherCase_DecidesByListingAndExistence(
        string[] listed,
        bool computedExists,
        bool expected)
    {
        HashSet<string> computed = ["/o/ApikeysTable.cs"];

        bool sameFile = TableClassGenerator.IsListedUnderAnotherCase(
            "/o/APIKeysTable.cs",
            computed,
            new HashSet<string>(listed),
            path => computedExists && computed.Contains(path) || listed.Contains(path));

        Assert.Equal(expected, sameFile);
    }

    // The property-name guard used to run per table inside the write loop, so a
    // collision in a later table left the earlier tables' files already written.
    [Fact]
    public void Run_LaterTableWithPropertyCollision_WritesNothing()
    {
        using TempSqliteDatabase db = TempSqliteDatabase.Create(
            """
            CREATE TABLE aaa_first (id INTEGER);
            CREATE TABLE bbb_second (x_y INTEGER, x__y INTEGER);
            """);

        Assert.Throws<CommandLineException>(() => Run(db, RunMode.Generate));

        Assert.False(File.Exists(Path.Combine(_outputDirectory, "AaaFirstTable.cs")));
    }

    // `--tables item,item` used to reach the class-name guard as two tables and be
    // misdiagnosed as a collision of a table with itself.
    [Fact]
    public void Run_Tables_DuplicateName_ReadsOnce()
    {
        using TempSqliteDatabase db = TempSqliteDatabase.Create(Schema);

        TableResult only = Assert.Single(
            Run(db, RunMode.Generate, tableNames: ["item", "item"]));

        Assert.Equal("item", only.TableName);
    }

    // The escape hatch the message names has to work.
    [Fact]
    public void Run_TwoTablesWithOneClassName_NarrowedByTables_Generates()
    {
        using TempSqliteDatabase db = TempSqliteDatabase.Create(
            """
            CREATE TABLE dupe_class (id INTEGER);
            CREATE TABLE dupe__class (id INTEGER);
            """);

        TableResult only = Assert.Single(
            Run(db, RunMode.Generate, tableNames: ["dupe_class"]));

        Assert.Equal("dupe_class", only.TableName);
    }

    // Nothing read and nothing committed used to be a successful run reporting
    // nothing, so a misspelled --schema was indistinguishable from a clean one.
    [Fact]
    public void Run_NothingReadAndNothingCommitted_FailsNamingTheOptionThatSelectsTables()
    {
        using TempSqliteDatabase db = TempSqliteDatabase.Create(
            "CREATE TABLE gone (id INTEGER); DROP TABLE gone;");

        CommandLineException error = Assert.Throws<CommandLineException>(
            () => Run(db, RunMode.Generate));

        Assert.Equal(
            $"No tables found in the SQLite database file '{db.ConnectionInfo.ServiceName}'; "
                + "check --file",
            error.Message);
    }

    // The guard must not swallow this: an emptied schema whose classes are still
    // committed has a real answer — every one of them is removed.
    [Fact]
    public void Run_CheckAfterEverySourceTableDropped_ReportsThemRemoved()
    {
        using TempSqliteDatabase db = TempSqliteDatabase.Create(Schema);
        Run(db, RunMode.Generate);

        db.Execute("DROP TABLE item; DROP TABLE tag;");

        IReadOnlyList<TableResult> results = Run(db, RunMode.Check);

        Assert.Equal(2, results.Count);
        Assert.All(results, r => Assert.Equal(TableStatus.Removed, r.Status));
    }

    private IReadOnlyList<TableResult> Run(
        TempSqliteDatabase db,
        RunMode mode,
        bool dryRun = false,
        IReadOnlyList<string>? tableNames = null,
        bool lowercaseNames = false) =>
        RunInto(db, mode, _outputDirectory, dryRun, tableNames, lowercaseNames);

    private static IReadOnlyList<TableResult> RunInto(
        TempSqliteDatabase db,
        RunMode mode,
        string outputDirectory,
        bool dryRun = false,
        IReadOnlyList<string>? tableNames = null,
        bool lowercaseNames = false)
    {
        RunOptions options = new(
            mode,
            db.ConnectionInfo,
            TestSettings.Create(outputDirectory: outputDirectory, tableNames: tableNames),
            dryRun);

        return new TableClassGenerator(
            CatalogReaderFactory.Create(db.ConnectionInfo, lowercaseNames),
            options).Run();
    }

    private static TableResult Single(IReadOnlyList<TableResult> results, string tableName) =>
        Assert.Single(results.Where(r => r.TableName == tableName));

    // The file name is what collides, and the common filesystems fold case.
    [Fact]
    public void Run_TwoTablesWithCaseOnlyClassNames_ThrowsCommandLineException()
    {
        using TempSqliteDatabase db = TempSqliteDatabase.Create(
            """
            CREATE TABLE web_api (id INTEGER);
            CREATE TABLE webapi (id INTEGER);
            """);

        CommandLineException ex = Assert.Throws<CommandLineException>(
            () => Run(db, RunMode.Generate));

        Assert.Equal(
            "Tables 'web_api' and 'webapi' generate the classes WebApiTable and WebapiTable, "
                + "whose file names differ only by case and collide on a case-folding file "
                + "system; rename one of them or narrow the run with --tables.",
            ex.Message);
    }

    // Two --tables runs can each pass GuardClassNames and still target one file; the
    // file on disk names the other table (release audit pass 8).
    [Fact]
    public void Run_TablesScopedRunOverAnotherTablesFile_ThrowsCommandLineException()
    {
        using TempSqliteDatabase db = TempSqliteDatabase.Create(
            """
            CREATE TABLE order_item (id INTEGER PRIMARY KEY);
            CREATE TABLE orderItem (code TEXT PRIMARY KEY);
            """);
        Run(db, RunMode.Generate, tableNames: ["order_item"]);

        CommandLineException ex = Assert.Throws<CommandLineException>(
            () => Run(db, RunMode.Generate, tableNames: ["orderItem"]));

        Assert.Equal(
            "OrderItemTable.cs already describes table 'order_item', and this run generates "
                + "'orderItem' into it; generate the two into separate --output directories, "
                + "or rename one of the tables",
            ex.Message);
        Assert.Contains(
            "base(\"order_item\"",
            File.ReadAllText(Path.Combine(_outputDirectory, "OrderItemTable.cs")));
    }

    // The committed file carries the emitted literal, which --lowercase varies,
    // so the same table must not read as a different one across the toggle.
    [Fact]
    public void Run_LowercaseToggledBetweenRuns_RegeneratesTheSameTable()
    {
        using TempSqliteDatabase db = TempSqliteDatabase.Create(
            "CREATE TABLE ITEM (id INTEGER PRIMARY KEY);");
        Run(db, RunMode.Generate, lowercaseNames: true);

        Run(db, RunMode.Generate, lowercaseNames: false);

        Assert.Contains(
            "base(\"ITEM\"",
            File.ReadAllText(Path.Combine(_outputDirectory, "ItemTable.cs")));
    }

    // The same for --qualify-schema, whose literal gains a `{schema}.` prefix.
    [Fact]
    public void Run_OverASchemaQualifiedCommittedFile_RegeneratesTheSameTable()
    {
        using TempSqliteDatabase db = TempSqliteDatabase.Create(
            "CREATE TABLE item (id INTEGER PRIMARY KEY);");
        Run(db, RunMode.Generate);
        string path = Path.Combine(_outputDirectory, "ItemTable.cs");
        File.WriteAllText(
            path,
            File.ReadAllText(path)
                .Replace("base(\"item\"", "base(\"main.item\"", StringComparison.Ordinal));

        Run(db, RunMode.Generate);

        Assert.Contains("base(\"item\"", File.ReadAllText(path));
    }

    // The guard runs before any write, like the emitter's: a later table's
    // collision must not leave earlier tables already rewritten.
    [Fact]
    public void Run_LaterTableCollidesWithItsCommittedFile_WritesNothing()
    {
        using TempSqliteDatabase db = TempSqliteDatabase.Create(
            """
            CREATE TABLE item (id INTEGER PRIMARY KEY);
            CREATE TABLE order_item (id INTEGER PRIMARY KEY);
            CREATE TABLE orderItem (code TEXT PRIMARY KEY);
            """);
        Run(db, RunMode.Generate, tableNames: ["order_item"]);

        Assert.Throws<CommandLineException>(
            () => Run(db, RunMode.Generate, tableNames: ["item", "orderItem"]));

        Assert.False(File.Exists(Path.Combine(_outputDirectory, "ItemTable.cs")));
        Assert.Contains(
            "base(\"order_item\"",
            File.ReadAllText(Path.Combine(_outputDirectory, "OrderItemTable.cs")));
    }
}
