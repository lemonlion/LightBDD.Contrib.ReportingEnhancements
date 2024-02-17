using System.Text;
using LightBDD.Core.Results;
using LightBDD.Framework.Reporting.Formatters;

namespace LightBDD.Contrib.ReportingEnhancements.Reports;

/// <summary>
/// Formats feature results as XML.
/// </summary>
public class YamlReportFormatter : IReportFormatter
{
    #region IReportFormatter Members

    public YamlReportOptions? Options { get; set; }

    /// <summary>
    /// Formats provided feature results and writes to the <paramref name="stream"/>.
    /// </summary>
    /// <param name="stream">Stream to write formatted results to.</param>
    /// <param name="features">Feature results to format.</param>
    public void Format(Stream stream, params IFeatureResult[] features)
    {
        Options ??= new YamlReportOptions();
        var scenariosRun = features.SelectMany(x => x.GetScenarios()).ToList();

        if (Options.OnlyCreateReportOnFullySuccessfulTestRun)
        {
            if (scenariosRun.Any(x => x.Status == ExecutionStatus.Failed))
                return;
        }

        if (Options.OnlyCreateReportOnFullTestRun)
        {
            var numberOfTestsInRun = scenariosRun.Count;
            var totalNumberOfTests = Options.TestAssembly.CountNumberOfTestsInAssembly();
            if (numberOfTestsInRun != totalNumberOfTests)
                return;
        }

        using var writer = new StreamWriter(stream, new UTF8Encoding(false));
        writer.Write(ToYamlDocument(features, Options));
    }

    #endregion

    private static string ToYamlDocument(IFeatureResult[] features, YamlReportOptions? options)
    {
        features = features.OrderBy(x => x.Info.Name.ToString()).ToArray();

        var yml = new StringBuilder();
        yml.Append("Title: " + options.Title + "\n");
        yml.Append("Features:\n");

        const string HappyPathLabel = "Happy Path";

        foreach (var feature in features)
        {
            yml.Append("  - Feature: " + feature.Info.Name.ToString().SanitiseForYml() + "\n");
            yml.Append("  - Scenarios:\n");
            var scenarios = feature.GetScenarios().OrderBy(x => x.Info.Labels.Contains(HappyPathLabel)).ThenBy(x => x.Info.Name.ToString()).ToArray();
            foreach (var scenario in scenarios)
            {
                yml.Append("    - Scenario: " + scenario.Info.Name.ToString().SanitiseForYml() + "\n");
                yml.Append("      IsHappyPath: " + scenario.Info.Labels.Any(x => x == HappyPathLabel).ToString().ToLower() + "\n");
                var steps = scenario.GetSteps();
                yml.Append("      Definition: " + "\n");
                CreateSteps(steps, yml);
                yml.Append("\n\n");
            }
        }

        return yml.ToString();
    }

    private static void CreateSteps(IEnumerable<IStepResult> steps, StringBuilder yml, string indent = "    ")
    {
        indent += "    ";
        foreach (var step in steps)
        {
            yml.Append(indent + step.Info.Name.ToString().SanitiseForYml() + $" (STEP {step.Info.GroupPrefix}{step.Info.Number})" + "\n");
            var subSteps = step.GetSubSteps()?.ToArray();
            if (subSteps is not null && subSteps.Any())
            {
                CreateSteps(subSteps, yml, indent);
            }
        }
    }
}