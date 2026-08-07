using System.Collections.Generic;
using Microsoft.CodeAnalysis.Diagnostics;

namespace SqlArtisan.Analyzers.Tests;

/// <summary>A minimal <see cref="AnalyzerConfigOptions"/> backed by a plain dictionary, for resolver unit tests.</summary>
internal sealed class TestAnalyzerConfigOptions : AnalyzerConfigOptions
{
    private readonly Dictionary<string, string> _values;

    public TestAnalyzerConfigOptions(Dictionary<string, string> values) => _values = values;

    public override bool TryGetValue(string key, out string value) => _values.TryGetValue(key, out value!);

    public override IEnumerable<string> Keys => _values.Keys;
}

/// <summary>
/// A host whose <see cref="AnalyzerConfigOptions.Keys"/> throws
/// <see cref="System.NotImplementedException"/> — the base class's own default,
/// which real Roslyn hosts (4.8.0+) override but a custom implementation
/// (including this project's own <see cref="TestAnalyzerConfigOptions"/> before
/// #432) may not. Exercises the degrade-key-name-validation-only path.
/// </summary>
internal sealed class KeysThrowingAnalyzerConfigOptions : AnalyzerConfigOptions
{
    private readonly Dictionary<string, string> _values;

    public KeysThrowingAnalyzerConfigOptions(Dictionary<string, string> values) => _values = values;

    public override bool TryGetValue(string key, out string value) => _values.TryGetValue(key, out value!);
}
