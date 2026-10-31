using System.Data;
using System.Reflection;
using static SqlArtisan.Sql;

namespace SqlArtisan.IntegrationTests.ValueReading;

/// <summary>What a slot does with a value at build time, observed without a database.</summary>
public enum Observed
{
    CompileStop,
    EagerThrow,
    BuildThrow,
    BuiltMarker,
    BuiltLiteral,
    BuiltOther,
    Unreachable,
}

/// <summary>
/// Injects one value kind into one position slot and builds the result for one dialect.
/// The filler logic follows FactoryGuardSweepTests (a valid value for every other
/// parameter, so the injected value is the only variable); builder-stage slots take a
/// fresh chain from <see cref="StagePool"/>, since one builder is one chain (#676).
/// </summary>
public static class PositionProbe
{
    public sealed record Result(Observed Observed, string Detail);

    private static DbTable T => new("vr_t", "t");

    public static Result Run(PositionInventory.Slot slot, ValueKind kind, Dbms dbms)
    {
        MethodBase? member = Resolve(slot);
        if (member is null)
        {
            return new(Observed.Unreachable, "member not found");
        }

        ParameterInfo parameter = member.GetParameters().First(p => p.Name == slot.Parameter);
        if (!ValueKinds.Compiles(kind, parameter.ParameterType))
        {
            return new(Observed.CompileStop, "");
        }

        object? sample = ValueKinds.Sample(kind, T);
        object? node;
        try
        {
            node = Invoke(member, parameter, sample);
        }
        catch (TargetInvocationException ex)
        {
            return new(Observed.EagerThrow, ex.InnerException?.Message ?? ex.Message);
        }
        catch (Exception ex)
        {
            // Not the library's throw (that arrives wrapped): reflection or the harness.
            return new(Observed.Unreachable, $"{ex.GetType().Name}: {ex.Message}");
        }

        SqlStatement? sql;
        try
        {
            sql = Embed(node, dbms);
        }
        catch (Exception ex)
        {
            return new(Observed.BuildThrow, ex.Message);
        }

        if (sql is null)
        {
            return new(Observed.Unreachable, $"cannot embed {node?.GetType().Name}");
        }

        return new(ShapeOf(kind, sql), sql.Text);
    }

    private static Observed ShapeOf(ValueKind kind, SqlStatement sql)
    {
        object? probe = kind switch
        {
            ValueKind.BindInt => ValueKinds.Ordinal,
            ValueKind.BindText => "b",
            ValueKind.BindNull => DBNull.Value,
            ValueKind.CSharpNull or ValueKind.SqlNull or ValueKind.Column or ValueKind.Expression
                or ValueKind.Subquery or ValueKind.Quantified or ValueKind.Interval => null,
            _ => ValueKinds.Sample(kind, T),
        };

        if (probe is null)
        {
            return Observed.BuiltOther;
        }

        bool bound = false;
        sql.Parameters.ForEach((_, b) =>
        {
            if (Equals(b.Value, probe)
                || (b.Value is short or byte or int && probe is short or byte or int
                    && Convert.ToInt32(b.Value) == Convert.ToInt32(probe)))
            {
                bound = true;
            }
        });

        return bound ? Observed.BuiltMarker : Observed.BuiltLiteral;
    }

    private static MethodBase? Resolve(PositionInventory.Slot slot)
    {
        Type? type = typeof(Sql).Assembly.GetExportedTypes().FirstOrDefault(
            t => t.Name == slot.DeclaringType);
        if (type is null)
        {
            return null;
        }

        IEnumerable<MethodBase> candidates = slot.Member == ".ctor"
            ? type.GetConstructors()
            : type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static
                | BindingFlags.DeclaredOnly).Where(m => m.Name == slot.Member);

        MethodBase? found = candidates.FirstOrDefault(
            m => PositionInventory.Signature(m) == slot.Signature);
        return found is MethodInfo { IsGenericMethodDefinition: true } generic
            ? generic.MakeGenericMethod(typeof(object))
            : found;
    }

    private static object? Invoke(MethodBase member, ParameterInfo injected, object? sample)
    {
        ParameterInfo[] parameters = member.GetParameters();
        object?[] args = new object?[parameters.Length];
        for (int i = 0; i < parameters.Length; i++)
        {
            args[i] = parameters[i] == injected || parameters[i].Name == injected.Name
                ? Wrap(parameters[i].ParameterType, sample)
                : Filler(member, parameters[i]);
        }

        if (member is ConstructorInfo ctor)
        {
            return ctor.Invoke(args);
        }

        MethodInfo method = (MethodInfo)member;
        object? target = method.IsStatic ? null : Target(method.DeclaringType!);
        return method.Invoke(target, args);
    }

    private static object? Wrap(Type t, object? sample)
    {
        if (t == typeof(object[]))
        {
            return new[] { sample };
        }

        if (t == typeof(object[][]))
        {
            return new object?[][] { [sample] };
        }

        if (t == typeof(IEnumerable<object[]>))
        {
            return new List<object?[]> { new[] { sample } };
        }

        if (t == typeof((object, object)))
        {
            return (sample!, (object)1);
        }

        if (t == typeof((object, object)[]))
        {
            return new[] { (sample!, (object)1) };
        }

        if (t.IsArray && t != typeof(string))
        {
            System.Array array = System.Array.CreateInstance(t.GetElementType()!, 1);
            array.SetValue(sample, 0);
            return array;
        }

        if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(IReadOnlyCollection<>))
        {
            return new List<object?> { sample };
        }

        if (t == typeof(long) && sample is int i)
        {
            return (long)i;
        }

        if (t == typeof(double) && sample is int j)
        {
            return (double)j;
        }

        if (t == typeof(int) && sample is char c)
        {
            return (int)c;
        }

        if (t == typeof(int) && sample is short or byte)
        {
            return Convert.ToInt32(sample);
        }

        return sample;
    }

    private static object Target(Type declaring)
    {
        if (declaring == typeof(SqlExpression))
        {
            return T.Column("a");
        }

        if (declaring == typeof(PartitionByClause))
        {
            return PartitionBy(T.Column("g"));
        }

        if (declaring == typeof(MatchFunction))
        {
            return Match(T.Column("a"));
        }

        if (declaring == typeof(SearchedCaseWhenCondition))
        {
            return When(T.Column("b") > 0);
        }

        if (declaring == typeof(SimpleCaseWhenExpression))
        {
            return When(1);
        }

        foreach ((Type declared, Func<object> make) in StagePool()
            .OrderBy(e => e.Item1 == declaring ? 0 : 1))
        {
            if (declaring.IsAssignableFrom(declared))
            {
                return make();
            }
        }

        throw new NotSupportedException($"no target for {declaring.Name}");
    }

    private static (Type, Func<object>) S<TStage>(Func<TStage> make)
        where TStage : notnull => (typeof(TStage), () => make());

    /// <summary>Fresh builders, each keyed by the stage interface its last call
    /// declares, so a slot runs on the stage that offers it.</summary>
    private static IEnumerable<(Type, Func<object>)> StagePool()
    {
        DbTable s = new("vr_s", "s");
        DbTable u = new("vr_t");
        yield return S(() => With(new Cte("c").As(Select(T.Column("a")).From(T))));
        yield return S(() => Select(T.Column("a")).From(T));
        yield return S(() => Select(T.Column("a")).From(T).Where(T.Column("id") > 0));
        yield return S(() => Select(T.Column("a")).From(T).GroupBy(T.Column("g")));
        yield return S(() => Select(T.Column("a")).From(T).GroupBy(T.Column("g"))
            .Having(Count(T.Column("id")) > 0));
        yield return S(() => Select(T.Column("a")).From(T).GroupBy(T.Column("g")).WithRollup());
        yield return S(() => Select(T.Column("a")).From(T).OrderBy(T.Column("id")));
        yield return S(() => Select(T.Column("a")).From(T).OrderBy(T.Column("id")).Limit(1));
        yield return S(() => Select(T.Column("a")).From(T).OrderBy(T.Column("id")).OffsetRows(1));
        yield return S(() => InsertInto(u, u.Column("a")));
        yield return S(() => InsertInto(u));
        yield return S(() => InsertInto(u, u.Column("a")).Values(1));
        yield return S(() => InsertInto(u, u.Column("a")).Output(u.Column("a")));
        yield return S(() => InsertIgnoreInto(u, u.Column("a")));
        yield return S(() => InsertIgnoreInto(u));
        yield return S(() => InsertIgnoreInto(u, u.Column("a")).Values(1));
        yield return S(() => InsertInto(u, u.Column("a")).Values(1).OnConflict(u.Column("a"))
            .DoNothing());
        yield return S(() => Update(u).Set(u.Column("a") == 1));
        yield return S(() => DeleteFrom(u));
        yield return S(() => MergeInto(u).Using(s).On(u.Column("id") == s.Column("id"))
            .WhenNotMatched().ThenInsert(u.Column("a")));
        yield return S(() => MergeInto(u).Using(s).On(u.Column("id") == s.Column("id"))
            .WhenNotMatched(u.Column("b") > 0).ThenInsert(u.Column("a")));
    }

    private static object? Filler(MethodBase member, ParameterInfo parameter)
    {
        Type t = parameter.ParameterType;
        string name = member.Name;
        DbTable table = T;

        if (t == typeof(object))
        {
            return name is "Cube" or "Rollup" or "Group" or "PartitionBy" or "DistinctOn"
                or "OrderBy" or "GroupBy"
                ? table.Column("g")
                : 1;
        }

        if (t == typeof(object[]))
        {
            object[] filler = name is "Cube" or "Rollup" or "Group" or "PartitionBy"
                or "DistinctOn" or "OrderBy" or "GroupBy" or "Select" or "Output" or "Returning"
                ? [table.Column("g")]
                : [1];
            return filler;
        }

        return t switch
        {
            _ when t == typeof(string) => "x",
            _ when t == typeof(int) || t == typeof(int?) => 1,
            _ when t == typeof(char) => 'x',
            _ when t == typeof(double) => 0.5,
            _ when t == typeof(bool) => true,
            _ when t == typeof(DbType?) => DbType.Int32,
            _ when t == typeof(ParameterDirection?) => null,
            _ when t == typeof(DateTimePart) => name == "Numtodsinterval"
                ? DateTimePart.Day : DateTimePart.Year,
            _ when t == typeof(SearchModifier) => SearchModifier.InNaturalLanguageMode,
            _ when t == typeof(RegexpOptions) => RegexpOptions.CaseInsensitive,
            _ when t == typeof(object[][]) => new object[][] { [1] },
            _ when t == typeof(IEnumerable<object[]>) => new List<object[]> { new object[] { 1 } },
            _ when t == typeof(string[]) => new[] { "c1" },
            _ when t == typeof((object, object)) => ((object)1, (object)2),
            _ when t == typeof((object, object)[]) => new[] { ((object)1, (object)2) },
            _ when t == typeof(DbTableBase) => new DbTable("vr_s", "s"),
            _ when t == typeof(DbColumn) => table.Column("a"),
            _ when t == typeof(DbColumn[]) => new[] { table.Column("a") },
            _ when t == typeof(SqlExpression) => table.Column("a"),
            _ when t == typeof(SqlCondition) => table.Column("b") > 0,
            _ when t == typeof(ISubquery) => Select(new DbTable("vr_s", "s").Column("b"))
                .From(new DbTable("vr_s", "s")),
            _ when t == typeof(SearchedCaseWhenClause) => When(table.Column("b") > 0).Then(1),
            _ when t == typeof(SearchedCaseWhenClause[])
                => new[] { When(table.Column("b") > 0).Then(1) },
            _ when t == typeof(SimpleCaseWhenClause) => When(1).Then(2),
            _ when t == typeof(SimpleCaseWhenClause[]) => new[] { When(1).Then(2) },
            _ when t == typeof(CaseElseExpression) => Else(1),
            _ when t == typeof(DistinctKeyword) => Distinct,
            _ when t == typeof(DistinctOnKeyword) => DistinctOn(table.Column("g")),
            _ when t == typeof(AsteriskMarker) => Asterisk,
            _ when t == typeof(OrderByClause) => OrderBy(table.Column("id")),
            _ when t == typeof(SeparatorClause) => Separator(", "),
            _ when t == typeof(SqlHints) => Hints("INDEX(t)"),
            _ when t == typeof(TopClause) => Top(1),
            _ when t == typeof(GroupingSet) => Group(table.Column("g")),
            _ when t == typeof(GroupingSet[]) => new[] { Group(table.Column("g")) },
            _ when t == typeof(IntervalField) => Year(),
            _ when t == typeof(IntervalExpression) => Interval(1, DateTimePart.Day),
            _ when t == typeof(IntervalLiteralExpression)
                => IntervalLiteral("1-2", Year(), ToMonth),
            _ when t == typeof(DerivedTableBase) => new DerivedTable("d"),
            _ when t == typeof(int?) => null,
            _ when t == typeof(DbType) => DbType.Int32,
            _ => throw new NotSupportedException($"no filler for {t.Name} in {member.Name}"),
        };
    }

    private static SqlStatement? Embed(object? node, Dbms dbms)
    {
        DbTable t = T;
        return node switch
        {
            null => null,
            SqlStatement statement => statement,
            ISqlBuilder builder => builder.Build(dbms),
            CaseElseExpression @else => Select(Case(When(t.Column("b") > 0).Then(1), @else))
                .From(t).Build(dbms),
            ExpressionAlias alias => Select(alias).From(t).Build(dbms),
            SqlExpression expr => Select(expr).From(t).Build(dbms),
            SqlCondition cond => Select(t.Column("a")).From(t).Where(cond).Build(dbms),
            TableReference tableRef => Select(t.Column("a")).From(tableRef).Build(dbms),
            CommonTableExpression cte => With(cte).Select(t.Column("a")).From(t).Build(dbms),
            SortOrder order => Select(t.Column("a")).From(t).OrderBy(order).Build(dbms),
            GroupingSet set => Select(t.Column("g")).From(t).GroupBy(GroupingSets(set))
                .Build(dbms),
            OrderByClause orderBy => Select(RowNumber().Over(orderBy)).From(t).Build(dbms),
            PartitionByClause partition => Select(RowNumber().Over(
                partition.OrderBy(t.Column("id")))).From(t).Build(dbms),
            PartitionByAndOrderBy both => Select(RowNumber().Over(both)).From(t).Build(dbms),
            DistinctOnKeyword distinctOn => Select(distinctOn, t.Column("a")).From(t)
                .Build(dbms),
            TopClause top => Select(top, t.Column("a")).From(t).Build(dbms),
            SearchedCaseWhenClause searched => Select(Case(searched)).From(t).Build(dbms),
            SimpleCaseWhenClause simple => Select(Case(t.Column("b"), simple)).From(t)
                .Build(dbms),
            QuantifiedExpression or QuantifiedSubquery => Select(t.Column("a")).From(t)
                .Where(t.Column("b") == node).Build(dbms),
            IntervalExpression interval => Select(t.Column("d") + interval).From(t).Build(dbms),
            SqlPart part when part.GetType().Name is "GroupingElement" or "RollupElement"
                or "CubeElement" || part.GetType().BaseType?.Name == "GroupingElement"
                => Select(t.Column("g")).From(t).GroupBy(part).Build(dbms),
            FrameBound bound => Select(Sum(t.Column("b")).Over(
                OrderBy(t.Column("id")).Rows(bound))).From(t).Build(dbms),
            IntervalField field => Select(IntervalLiteral("1", field)).From(t).Build(dbms),
            SimpleCaseWhenExpression pending => Select(Case(t.Column("b"), pending.Then(1)))
                .From(t).Build(dbms),
            MatchFunction match => Select(t.Column("a")).From(t).Where(match.Against("x"))
                .Build(dbms),
            _ => Complete(node, dbms),
        };
    }

    // A pending type completes through its own Over(OrderByClause) or
    // WithinGroup(OrderByClause), found by name so each family needs no arm.
    private static SqlStatement? Complete(object node, Dbms dbms)
    {
        DbTable t = T;
        foreach (string name in new[] { "Over", "WithinGroup" })
        {
            MethodInfo? m = node.GetType().GetMethod(name, [typeof(OrderByClause)]);
            if (m is not null)
            {
                object? done = m.Invoke(node, [OrderBy(t.Column("id"))]);
                return done is SqlExpression e ? Select(e).From(t).Build(dbms) : null;
            }
        }

        if (node.GetType().Name == "WaitBehavior")
        {
            return null; // FOR UPDATE WAIT: Oracle-only clause, outside the hazard
        }

        return null;
    }
}
