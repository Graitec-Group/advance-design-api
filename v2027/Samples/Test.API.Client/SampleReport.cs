using System.Threading;

namespace Test.API.Client
{
  /// <summary>
  /// Final outcome of a sample run, decided explicitly from the checks the sample recorded
  /// through <see cref="SampleReport"/> (never by parsing the console text).
  /// </summary>
  internal enum SampleOutcome
  {
    NotRun,
    Running,
    Passed,
    Failed,
    Error,
    CompletedNoChecks,
    Canceled
  }

  internal static class SampleOutcomeExtensions
  {
    /// <summary>
    /// Single source of the status wording, shared by the console summary, the GUI list, the
    /// details window and the exported reports.
    /// </summary>
    internal static string ToDisplayText(this SampleOutcome outcome) => outcome switch
    {
      SampleOutcome.NotRun            => "Not run",
      SampleOutcome.Running           => "Running",
      SampleOutcome.Passed            => "Passed",
      SampleOutcome.Failed            => "Failed",
      SampleOutcome.Error             => "Error",
      SampleOutcome.CompletedNoChecks => "Completed",
      SampleOutcome.Canceled          => "Canceled",
      _                               => outcome.ToString()
    };

    /// <summary>
    /// True when the outcome must make a batch run fail. A sample without verification is not a
    /// failure: it declares no expectation to begin with.
    /// </summary>
    internal static bool IsFailure(this SampleOutcome outcome) =>
      outcome is SampleOutcome.Failed or SampleOutcome.Error;
  }

  /// <summary>
  /// Accumulates the verification checks recorded while a single sample runs.
  /// </summary>
  internal sealed class SampleRunReport  {
    private const int MaxRecordedFailures = 200;

    private readonly List<string> _failures = new();
    private readonly object _sync = new();
    private int _passCount;
    private int _failCount;

    internal SampleRunReport(string label) => Label = label;

    internal string Label { get; }

    internal int PassCount => Volatile.Read(ref _passCount);

    internal int FailCount => Volatile.Read(ref _failCount);

    internal int TotalCount => PassCount + FailCount;

    /// <summary>First unrecoverable error reported through <see cref="SampleReport.Fatal"/>, if any.</summary>
    internal string? FatalMessage { get; private set; }

    internal IReadOnlyList<string> Failures
    {
      get { lock (_sync) return _failures.ToArray(); }
    }

    internal void AddPass() => Interlocked.Increment(ref _passCount);

    internal void AddFailure(string description)
    {
      Interlocked.Increment(ref _failCount);
      lock (_sync)
      {
        if (_failures.Count < MaxRecordedFailures)
          _failures.Add(description);
      }
    }

    internal void SetFatal(string message)
    {
      lock (_sync)
      {
        FatalMessage ??= message;
      }
      AddFailure(message);
    }

    /// <summary>
    /// Resolves the outcome. An exception always wins; then recorded failures; then recorded
    /// passes. A sample that recorded nothing at all is reported as "completed, no checks".
    /// </summary>
    internal SampleOutcome Resolve(Exception? exception, bool canceled)
    {
      if (canceled)
        return SampleOutcome.Canceled;
      if (exception is not null)
        return SampleOutcome.Error;
      if (FailCount > 0)
        return SampleOutcome.Failed;
      if (PassCount > 0)
        return SampleOutcome.Passed;
      return SampleOutcome.CompletedNoChecks;
    }

    /// <summary>Longest reason kept in the summary, so it still fits a status column.</summary>
    private const int MaxReasonLength = 160;

    /// <summary>
    /// Short human readable summary, e.g. "12/12 checks passed" or
    /// "8/9 checks passed - Mesh.SpacingFactor: expected=1.2, actual=1 (+2 more)".
    /// <para>
    /// The first failure is part of it on purpose: this text is what the console prints, what the
    /// sample list shows and what a CI server reads as the failure message, and a bare count says
    /// nothing about <em>what</em> went wrong.
    /// </para>
    /// </summary>
    internal string Describe()
    {
      if (TotalCount == 0)
        return "no checks recorded";

      string counts = $"{PassCount}/{TotalCount} checks passed";
      if (FailCount == 0)
        return counts;

      var failures = Failures;
      if (failures.Count == 0)
        return counts;

      string first = failures[0];
      if (first.Length > MaxReasonLength)
        first = first[..MaxReasonLength] + "...";

      string more = FailCount > 1 ? $" (+{FailCount - 1} more)" : string.Empty;
      return $"{counts} - {first}{more}";
    }
  }

  /// <summary>
  /// Ambient recorder used by the samples to declare the outcome of their verifications.
  /// <para>
  /// The samples keep writing their usual console text; they simply also record the very same
  /// verdicts here, so the GUI runner (and the console summary) can report Pass/Fail from the
  /// code itself instead of parsing the output.
  /// </para>
  /// <para>
  /// All members are no-ops when no run is active, so calling them is always safe.
  /// </para>
  /// </summary>
  internal static class SampleReport
  {
    private static readonly AsyncLocal<SampleRunReport?> Context = new();

    internal static SampleRunReport? Current => Context.Value;

    /// <summary>Starts recording for one sample. Dispose the returned scope when the sample ends.</summary>
    internal static SampleReportScope BeginSample(string label)
    {
      var previous = Context.Value;
      var report = new SampleRunReport(label);
      Context.Value = report;
      return new SampleReportScope(report, previous);
    }

    internal static void Pass(string label)
    {
      _ = label;
      Context.Value?.AddPass();
    }

    internal static void Fail(string label, string? reason = null)
    {
      var report = Context.Value;
      if (report is null)
        return;

      report.AddFailure(string.IsNullOrEmpty(reason) ? label : $"{label}: {reason}");
    }

    /// <summary>Records a single boolean verification.</summary>
    internal static void Check(bool condition, string label, string? failureReason = null)
    {
      if (condition)
        Pass(label);
      else
        Fail(label, failureReason);
    }

    /// <summary>Records an unrecoverable error that prevents the sample from verifying anything.</summary>
    internal static void Fatal(string message) => Context.Value?.SetFatal(message);

    internal sealed class SampleReportScope : IDisposable
    {
      private readonly SampleRunReport? _previous;
      private bool _disposed;

      internal SampleReportScope(SampleRunReport report, SampleRunReport? previous)
      {
        Report = report;
        _previous = previous;
      }

      internal SampleRunReport Report { get; }

      public void Dispose()
      {
        if (_disposed)
          return;

        _disposed = true;
        Context.Value = _previous;
      }
    }
  }
}
