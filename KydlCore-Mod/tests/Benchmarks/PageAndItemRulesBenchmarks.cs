using BenchmarkDotNet.Attributes;
using KydlCore.Features.Hub.Domain;
using KydlCore.Features.Items.Domain;

namespace KydlCore.Benchmarks;

/// <summary>Page rebuild decision and KY-Items id rules: no scale axis, so no [Params] (every extra case costs ~20 s).</summary>
[MemoryDiagnoser]
public class PageAndItemRulesBenchmarks
{
    string _shown, _next, _guid, _id;

    [GlobalSetup]
    public void Setup()
    {
        _shown = "settings|General|3";
        _next = "settings|General|3";
        _guid = "com.kydldev.WhipItUp";
        _id = "whipitup:flaming-whip";
    }

    [Benchmark]
    public bool PageNeedsRebuild() => PageRules.NeedsRebuild(_shown, _next);

    [Benchmark]
    public bool IsUserGuid() => KyItemRules.IsUserGuid(_guid);

    [Benchmark]
    public string ModId() => KyItemRules.ModId(_guid);

    [Benchmark]
    public bool ValidateId() => KyItemRules.ValidateId(_id, _guid).IsSuccess;
}
