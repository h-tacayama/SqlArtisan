---
name: sa-run-sql-harness
description: Build and run a throwaway console that references SqlArtisan to observe the REAL emitted SQL, exercise failure modes, and measure allocations — instead of reasoning about DBMS grammar or output from memory. Use when asked to run/try SqlArtisan, check what SQL a builder call produces, confirm a function works across dialects, verify a misuse fails loudly, or measure build-path allocations. The sa-review-changes skill uses this for its empirical step.
---

# Run a SqlArtisan verification harness

SqlArtisan has no app to launch — it's a library that turns C# into a SQL string
plus parameters. The fastest way to *know* what a change does (not guess) is a
disposable console that references the project and prints the real output. Build
it under `/tmp` so nothing lands in the repo.

## Set up (throwaway, not committed)

```bash
mkdir -p /tmp/sa-harness && cd /tmp/sa-harness
cat > Demo.csproj <<'XML'
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net8.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="/home/user/SqlArtisan/src/SqlArtisan/SqlArtisan.csproj" />
  </ItemGroup>
</Project>
XML
# write Program.cs (template below), then:
dotnet run -c Release
```

(Adjust the `ProjectReference` path to wherever the repo is cloned.)

## Program.cs template

Define a table inline so you don't depend on the test fixtures, and add a small
printer. With alias `""` columns render bare (`name`); pass an alias (`"u"`) to
see dialect-specific quoting (`"u".name`, `` `u`.name ``).

```csharp
using System;
using SqlArtisan;
using static SqlArtisan.Sql;

internal sealed class T : DbTableBase
{
    public T() : base("users", "") { Name = new DbColumn("", "name"); Id = new DbColumn("", "id"); }
    public DbColumn Name { get; }
    public DbColumn Id { get; }
}

internal static class Program
{
    private static void Show(string label, SqlStatement sql)
    {
        Console.WriteLine($"-- {label}\n   {sql.Text}");
        sql.Parameters.ForEach((n, v) => Console.WriteLine($"      {n} = {v.Value}"));
    }

    private static void Main()
    {
        T u = new();
        Show("example", Select(u.Name).From(u).Build(Dbms.PostgreSql));
    }
}
```

## Three things worth checking

**1. SQL per dialect.** Call `Build(Dbms.X)` for every DBMS the construct targets
and read `sql.Text`. Confirm it is valid for *that* DBMS's grammar (don't trust
memory — this is the whole point). Print parameters via `sql.Parameters.ForEach`.

```csharp
foreach (Dbms d in new[]{ Dbms.PostgreSql, Dbms.MySql, Dbms.Oracle, Dbms.Sqlite, Dbms.SqlServer })
    Show(d.ToString(), Select(/* construct */).From(u).Build(d));
```

**2. Negative / enforcement.** A misuse should fail loudly, not emit invalid SQL.
A mandatory-clause "pending" type (e.g. `Listagg` before `.WithinGroup(...)`) is
not a `SqlExpression`, so `Select(...)` throws `ArgumentException` ("Invalid type
for SelectItem"). Prove the guard fires:

```csharp
try { var s = Select(Listagg(u.Name, ", ")).From(u).Build(Dbms.Oracle);
      Console.WriteLine("LEAK: " + s.Text); }      // bad: invalid SQL escaped
catch (Exception ex) { Console.WriteLine($"GUARD: {ex.GetType().Name}"); }
```

**3. Allocation (ADR 0006).** A quick thread-allocation probe is enough to back a
claim in chat; the *official* benchmark is `tests/SqlArtisan.Benchmark`
(BenchmarkDotNet) — cite that for anything that ships in docs.

```csharp
static long Alloc(Action a, int n)
{
    a(); // warm up / JIT
    long b = GC.GetAllocatedBytesForCurrentThread();
    for (int i = 0; i < n; i++) a();
    return (GC.GetAllocatedBytesForCurrentThread() - b) / n;
}
// e.g. compare a literal path vs a bound-parameter path:
Console.WriteLine(Alloc(() => GC.KeepAlive(
    Select(GroupConcat(u.Name, Separator(", "))).From(u).Build(Dbms.MySql)), 50_000) + " B/op");
```

Facts confirmed this way (reuse, don't re-derive): `string.Replace` returns the
same reference (0 B) when there is no match; `char.ToString()` costs ~24 B; the
inline-literal separator path is lighter than the bound-parameter path because
binding grows the parameter list.

## Notes

- This is for **observation**, not a substitute for `tests/SqlArtisan.Tests` —
  durable expectations belong in xUnit exact-SQL tests.
- Keep it in `/tmp`; never commit the harness.
- Complements the built-in `run`/`verify` skills with the SqlArtisan-specific
  project reference and the SQL/parameter/allocation probes above.
