using BenchmarkDotNet.Attributes;
using KydlCore.Features.Hub.Domain;

namespace KydlCore.Benchmarks;

/// <summary>Hub window stack, page rebuild decision and KY-Items id rules: run on every click / every 200 ms hub tick / every contribution.</summary>
[MemoryDiagnoser]
public class HubAndItemsBenchmarks
{
    WindowStack _stack;
    string _front, _back;

    [Params(1, 5, 20)]
    public int OpenWindows { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _stack = new WindowStack();
        for (int i = 0; i < OpenWindows; i++)
        {
            _stack.Open("window-" + i);
        }

        _front = "window-" + (OpenWindows - 1);
        _back = "window-0";
    }

    [Benchmark]
    public bool IsOpenFront() => _stack.IsOpen(_front);

    [Benchmark]
    public bool BringBackToFront()
    {
        // moves the back window to the front, then the old front again: the stack ends as it started
        _stack.BringToFront(_back);
        return _stack.BringToFront(_front);
    }

}
