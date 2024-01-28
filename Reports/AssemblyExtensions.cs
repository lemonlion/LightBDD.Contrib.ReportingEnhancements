using System.Reflection;
using LightBDD.XUnit2;
using Xunit;

namespace LightBDD.Contrib.ReportingEnhancements.Reports;

public static class AssemblyExtensions
{
    public static int CountNumberOfTestsInAssembly(this Assembly assembly)
    {
        return assembly.GetTypes()
            .SelectMany(t => t.GetMethods().AsParallel())
            .Where(m => m.GetCustomAttributes<ScenarioAttribute>().Any())
            .Sum(x =>
            {
                // Each InlineData counts as an additional test
                var inlineData = x.GetCustomAttributes<InlineDataAttribute>().ToArray();
                return inlineData.Any() ? inlineData.Length : 1;
            });
    }
}