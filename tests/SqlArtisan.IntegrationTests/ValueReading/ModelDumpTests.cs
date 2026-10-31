using System.Text;

namespace SqlArtisan.IntegrationTests.ValueReading;

// Trial-only: writes the model's cell table and its counts where the report reads them.
public sealed class ModelDumpTests
{
    // Context refines only these classes; every other class reads alike in each context.
    private static readonly (PositionClass Class, Context[] Contexts)[] ContextCells =
    [
        (PositionClass.StatementOrderByKey,
            [Context.TopLevel, Context.Subquery, Context.CorrelatedSubquery, Context.Cte,
                Context.SetOperationBranch]),
        (PositionClass.ExpressionOrderByKey, [Context.InsideWindow, Context.AggregateOrderBy]),
        (PositionClass.QuantifiableOperand, [Context.TopLevel, Context.SetClause]),
        (PositionClass.SimpleCaseOperand, [Context.TopLevel, Context.SetClause]),
    ];

    [Fact]
    public void Model_Dump_WritesCells()
    {
        string? path = Environment.GetEnvironmentVariable("VR_MODEL_TSV");
        if (path is null)
        {
            return;
        }

        StringBuilder sb = new();
        sb.Append("class\tslots\tkind\tlane\tcontext\toutcome\tshape\tdiagnostic\tquestion\tbasis\n");
        Dictionary<PositionClass, int> slots = PositionInventory.Slots
            .GroupBy(s => ValueReadingModel.ClassOf(s)!.Value)
            .ToDictionary(g => g.Key, g => g.Count());

        foreach (PositionClass c in ValueReadingModel.Classes)
        {
            Context[] contexts = ContextCells.FirstOrDefault(x => x.Class == c).Contexts
                ?? [Context.TopLevel];
            foreach (ValueKind k in ValueKinds.All)
            {
                foreach (Lane lane in ValueReadingModel.Lanes)
                {
                    foreach (Context ctx in contexts)
                    {
                        Cell cell = ValueReadingModel.Expect(c, k, lane, ctx);
                        sb.Append(c).Append('\t').Append(slots.GetValueOrDefault(c)).Append('\t')
                            .Append(k).Append('\t').Append(lane).Append('\t').Append(ctx)
                            .Append('\t').Append(cell.Outcome).Append('\t').Append(cell.Shape)
                            .Append('\t').Append(cell.DiagnosticId).Append('\t')
                            .Append(cell.QuestionId).Append('\t').Append(cell.Basis).Append('\n');
                    }
                }
            }
        }

        File.WriteAllText(path, sb.ToString());
    }
}
