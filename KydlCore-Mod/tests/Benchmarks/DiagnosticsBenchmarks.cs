using BenchmarkDotNet.Attributes;
using KydlCore.Features.Diagnostics.Domain;

namespace KydlCore.Benchmarks;

/// <summary>
/// Flight recorder hot paths (coverage suite, skill microbenchmarking use case 1): the ring takes a sample for every tracked object every
/// sample tick, the trigger rules look at every log line of the lab, the gate decides every dump. Comparison axis: scale (ring capacity).
/// </summary>
[MemoryDiagnoser]
public class DiagnosticsBenchmarks
{
    const string LabLine = "Test KydlCore/hub-open-close: Failed - the hub did not open within 15 s";
    const string OtherLine = "Loaded 15 plugins from BepInEx/plugins";

    FlightRing<FlightSample> _ring;
    FlightSample _sample;
    DumpGate _gate;
    string _source, _lab, _other, _reason;
    double _now;
    int _slugMax;

    [Params(256, 4096)]
    public int Capacity { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _ring = new FlightRing<FlightSample>(Capacity);
        for (int i = 0; i < Capacity; i++)
        {
            _ring.Add(new FlightSample(i * 0.1, i, 0, -i, 3.5, "grounded"));
        }

        _sample = new FlightSample(1.0, 2.0, 3.0, 4.0, 5.0, "swimming");
        _gate = new DumpGate();
        _source = TriggerRules.LabSource;
        _lab = LabLine;
        _other = OtherLine;
        _reason = "check-off:SteveTheTroll spot return too late";
        _slugMax = 40;
        _now = 100.0;
    }

    [Benchmark]
    public int RingAdd() => _ring.Add(_sample);

    [Benchmark]
    public int RingItemsAll() => _ring.Items().Count;

    [Benchmark]
    public int RingItemsFiltered() => _ring.Items(s => s.Speed > 2.0).Count;

    [Benchmark]
    public bool LabResultMatch() => TriggerRules.TryLabResult(_source, _lab, out _, out _, out _);

    [Benchmark]
    public bool LabResultOtherLine() => TriggerRules.TryLabResult(_source, _other, out _, out _, out _);

    [Benchmark]
    public string SlugOfReason() => TriggerRules.Slug(_reason, _slugMax);

    [Benchmark]
    public bool GateRefusesSameKind()
    {
        // same kind inside the gap: the common case while a test keeps failing
        _gate.Allow("testlab", _now, 1e9, 1);
        return _gate.Allow("testlab", _now + 1, 1e9, 1);
    }
}
