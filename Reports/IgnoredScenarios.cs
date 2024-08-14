namespace LightBDD.Contrib.ReportingEnhancements.Reports;

/// <summary>
/// Use this to keep track of scenarios that are ignored using StepExecution.Current.IgnoreScenario().
/// LightBDD doesn't include ignored scenarios when it calls IReportFormatter.Format().
/// </summary>
public abstract class IgnoredScenarios
{
    private static int _count = 0;

    public static int Count => Interlocked.Add(ref _count, 0);

    public static void Increment() => Interlocked.Increment(ref _count);
}