using BenchmarkDotNet.Running;
using SqlArtisan.Benchmark;

// `dotnet run -- validate` runs the one-time output-equivalence check; anything else is
// forwarded to BenchmarkDotNet's switcher (e.g. `-- --filter *SqlBuilderBenchmarks*`).
if (args.Length > 0 && args[0].Equals("validate", StringComparison.OrdinalIgnoreCase))
{
    return BenchmarkValidation.Run();
}

BenchmarkSwitcher
    .FromAssembly(typeof(Program).Assembly)
    .Run(args);

return 0;
