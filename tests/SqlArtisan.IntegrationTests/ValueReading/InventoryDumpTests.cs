namespace SqlArtisan.IntegrationTests.ValueReading;

// Trial-only: writes the position inventory where the report reads it.
public sealed class InventoryDumpTests
{
    [Fact]
    public void Inventory_Dump_WritesCsv()
    {
        string? path = Environment.GetEnvironmentVariable("VR_INVENTORY_CSV");
        if (path is null)
        {
            return;
        }

        IEnumerable<string> lines = PositionInventory.Slots.Select(s =>
            $"{s.DeclaringType}\t{s.Signature}\t{s.Parameter}\t{s.ParameterType}\t{s.Kind}");
        File.WriteAllLines(path, lines);
        File.AppendAllText(
            path,
            $"# members={PositionInventory.MemberCount} "
                + $"structural={PositionInventory.StructuralParameterCount}\n");
    }
}
