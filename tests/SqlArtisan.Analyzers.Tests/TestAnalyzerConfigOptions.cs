using System.Collections.Generic;
using Microsoft.CodeAnalysis.Diagnostics;

namespace SqlArtisan.Analyzers.Tests;

/// <summary>A minimal <see cref="AnalyzerConfigOptions"/> backed by a plain dictionary, for resolver unit tests.</summary>
internal sealed class TestAnalyzerConfigOptions : AnalyzerConfigOptions
{
    private readonly Dictionary<string, string> _values;

    public TestAnalyzerConfigOptions(Dictionary<string, string> values) => _values = values;

    public override bool TryGetValue(string key, out string value) => _values.TryGetValue(key, out value!);
}
