using System.Text;
using SqlArtisan.TableClassGen;

namespace SqlArtisan.TableClassGen.Tests;

// Files committed by earlier runs are read back by every later version: Shape1_0 is
// what 1.0 emits and Shape0_8 what 0.8 through 0.12 emitted, and both stay frozen,
// so a reader change that stops recognizing either fails here, not in a user's CI.
public class FixtureCorpusTests : IDisposable
{
    private const string Namespace = "Fixture.Tables";

    // The schema both fixture sets were generated from (Shape0_8 by the 0.12 tool).
    private const string Schema =
        """
        CREATE TABLE customer (id INTEGER PRIMARY KEY, name TEXT NOT NULL, created_at TEXT);
        CREATE TABLE order_item (order_id INTEGER NOT NULL, quantity INTEGER DEFAULT 1);
        CREATE INDEX ix_order_item_quantity ON order_item (quantity);
        """;

    private readonly string _output = Path.Combine(
        Path.GetTempPath(),
        $"sqlartisan_tcg_fixture_{Guid.NewGuid():N}");

    public void Dispose()
    {
        if (Directory.Exists(_output))
        {
            Directory.Delete(_output, recursive: true);
        }
    }

    // The one test that pins the emitter to a fixture: when a minor release changes
    // the emitted text, it moves to a new fixture set, and Shape1_0 stays for the
    // readers below.
    [Theory]
    [InlineData(false, "\n", false)]
    [InlineData(false, "\r\n", false)]
    [InlineData(false, "\n", true)]
    [InlineData(true, "\r\n", true)]
    public void Check_Shape1_0Files_AreInSync(bool subfolders, string newline, bool bom)
    {
        using TempSqliteDatabase db = TempSqliteDatabase.Create(Schema);
        Commit("Shape1_0", subfolders, newline, bom);

        IReadOnlyList<TableResult> results = Run(db, RunMode.Check, subfolders);

        Assert.Equal(2, results.Count);
        Assert.All(results, r => Assert.Equal(TableStatus.Unchanged, r.Status));
    }

    [Theory]
    [InlineData("Shape1_0", false)]
    [InlineData("Shape1_0", true)]
    [InlineData("Shape0_8", false)]
    [InlineData("Shape0_8", true)]
    public void Check_TableDropped_ReportsItsFileRemoved(string shape, bool subfolders)
    {
        using TempSqliteDatabase db = TempSqliteDatabase.Create(Schema);
        Commit(shape, subfolders, "\r\n", bom: true);
        db.Execute("DROP TABLE order_item");

        IReadOnlyList<TableResult> results = Run(db, RunMode.Check, subfolders);

        TableResult removed = Assert.Single(results, r => r.Status == TableStatus.Removed);
        Assert.Equal("OrderItemTable.cs", removed.TableName);
    }

    // 0.8 through 0.12 wrote the core names unqualified: the columns must still
    // read back, so the diff says only the layout moved rather than every column.
    [Fact]
    public void Check_Shape0_8Files_ReportLayoutChangeNotColumnChange()
    {
        using TempSqliteDatabase db = TempSqliteDatabase.Create(Schema);
        Commit("Shape0_8", subfolders: false, "\n", bom: false);

        IReadOnlyList<TableResult> results = Run(db, RunMode.Check, subfolders: false);

        Assert.All(results, r => Assert.Equal(TableStatus.Modified, r.Status));
        Assert.All(
            results,
            r => Assert.Equal(["~ column metadata or layout changed"], r.Changes));
    }

    // The table literal is what tells two tables sharing a class name apart; a
    // later version that cannot read it in a committed file must refuse the file.
    [Theory]
    [InlineData("Shape1_0")]
    [InlineData("Shape0_8")]
    public void Generate_AnotherTableIntoACommittedFile_IsRefused(string shape)
    {
        using TempSqliteDatabase db = TempSqliteDatabase.Create(
            Schema + "CREATE TABLE orderItem (code TEXT PRIMARY KEY);");
        Commit(shape, subfolders: false, "\r\n", bom: true);
        string committed = File.ReadAllText(Path.Combine(_output, "OrderItemTable.cs"));

        CommandLineException ex = Assert.Throws<CommandLineException>(
            () => Run(db, RunMode.Generate, subfolders: false, tableNames: ["orderItem"]));

        Assert.Equal(
            "OrderItemTable.cs already describes table 'order_item', and this run generates "
                + "'orderItem' into it; generate the two into separate --output directories, "
                + "or rename one of the tables",
            ex.Message);
        Assert.Equal(committed, File.ReadAllText(Path.Combine(_output, "OrderItemTable.cs")));
    }

    private void Commit(string shape, bool subfolders, string newline, bool bom)
    {
        string source = Path.Combine(AppContext.BaseDirectory, "Fixtures", shape);

        foreach (string fixture in Directory.GetFiles(source, "*.cs.txt"))
        {
            string name = Path.GetFileNameWithoutExtension(fixture);
            string directory = subfolders ? Path.Combine(_output, name[..1]) : _output;
            Directory.CreateDirectory(directory);

            File.WriteAllText(
                Path.Combine(directory, name),
                File.ReadAllText(fixture).ReplaceLineEndings(newline),
                new UTF8Encoding(encoderShouldEmitUTF8Identifier: bom));
        }
    }

    private IReadOnlyList<TableResult> Run(
        TempSqliteDatabase db,
        RunMode mode,
        bool subfolders,
        IReadOnlyList<string>? tableNames = null)
    {
        CodeGenerationSettings settings = new(
            Namespace,
            lowercaseNames: false,
            _output,
            subfolders,
            tableNames);

        return new TableClassGenerator(
            CatalogReaderFactory.Create(db.ConnectionInfo, lowercaseNames: false),
            new RunOptions(mode, db.ConnectionInfo, settings)).Run();
    }
}
