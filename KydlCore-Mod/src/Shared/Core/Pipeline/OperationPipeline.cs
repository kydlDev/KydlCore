using System;
using System.Collections.Generic;

namespace KydlCore.Core;

/// <summary>
/// Kydl.Core (Architecture V2, SteveTheTroll D-169): input -> validation steps (in order, the first that is not valid stops the run with its status) -> the deciding step -> typed response.
/// The executor that acts on the response is NOT part of the pipeline (the domain decides, the execution layer performs). Built once, run every frame: Run allocates nothing
/// unless a trace sink is set (debug only). Game-free.
/// </summary>
public sealed class OperationPipeline<TInput, TOutput>
{
    readonly string _name;
    readonly List<string> _names = new();
    readonly List<Func<TInput, ValidationResult>> _checks = new();
    readonly Func<TInput, OperationResponse<TOutput>> _decide;

    /// Trace sink (ModLog.Verbose in game); null = silent.
    public Action<string> Trace { get; set; }

    public OperationPipeline(string name, Func<TInput, OperationResponse<TOutput>> decide)
    {
        if (CoreLog.On) CoreLog.Verbose($"OperationPipeline.OperationPipeline(name={name}, decide={decide})");
        if (decide == null) throw new ArgumentNullException("decide");
        _name = name ?? "pipeline";
        _decide = decide;
    }

    public OperationPipeline(string name, IOperationStep<TInput, TOutput> step) : this(name, step != null ? (Func<TInput, OperationResponse<TOutput>>)step.Run : null) { }

    public int StepCount => _checks.Count;

    /// Adds a validation step (runs in the order added). Expected outcome: the pipeline has one more step; returns itself for chaining.
    public OperationPipeline<TInput, TOutput> Validate(string name, Func<TInput, ValidationResult> check)
    {
        if (CoreLog.On) CoreLog.Verbose($"OperationPipeline.Validate(name={name}, check={check})");
        if (check == null) throw new ArgumentNullException("check");
        _names.Add(name ?? ("check " + (_checks.Count + 1)));
        _checks.Add(check);
        { var returnedAt40 = this; if (CoreLog.On) CoreLog.Verbose($"OperationPipeline.Validate: returns {returnedAt40} (line 40)"); return returnedAt40; }
    }

    /// One run. Expected outcome: the first non-valid check's status (Rejected / Ignored) and reason (the step name goes to the trace only: no allocation), else the deciding step's response.
    public OperationResponse<TOutput> Run(TInput input)
    {
        if (CoreLog.On) CoreLog.Every("OperationPipeline.Run", 2d, $"OperationPipeline.Run(input={input})");
        for (int i = 0; i < _checks.Count; i++)
        {
            var v = _checks[i](input);
            if (v.IsValid) continue;
            var stop = new OperationResponse<TOutput>(v.Status, default(TOutput), v.Reason);
            if (Trace != null) Trace(_name + ": stopped at " + _names[i] + " - " + stop);
            { var returnedAt53 = stop; if (CoreLog.On) CoreLog.Every("OperationPipeline.Run:53", 2d, $"OperationPipeline.Run: returns {returnedAt53} (line 53)"); return returnedAt53; }
        }
        var r = _decide(input);
        if (Trace != null) Trace(_name + ": " + _checks.Count + " checks passed - " + r);
        { var returnedAt57 = r; if (CoreLog.On) CoreLog.Every("OperationPipeline.Run:57", 2d, $"OperationPipeline.Run: returns {returnedAt57} (line 57)"); return returnedAt57; }
    }
}
