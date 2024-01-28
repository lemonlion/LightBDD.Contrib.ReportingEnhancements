using System.Net;
using LightBDD.Core.Formatting;
using LightBDD.Core.Results;
using LightBDD.Framework.Reporting;

namespace LightBDD.Contrib.ReportingEnhancements.Reports
{
    public class CustomisableHtmlResultTextWriter : DefaultHtmlResultTextWriter
    {
        private int _currentToggleIdNumber = 0;
        public bool WriteRuntimeIds { get; set; }
        public bool IncludeDiagramsAsCode => DiagramAsCode.Any();
        public bool IncludeExecutionSummary { get; set; }
        public bool IncludeFeatureSummary { get; set; }
        public bool IncludeDurations { get; set; }
        public bool ShowStatusFilterToggles { get; set; }
        public bool ShowHappyPathToggle { get; set; } = true;
        public bool ShowExampleDiagramsToggle { get; set; } = true;
        public bool IncludeIgnoredTests { get; set; }
        public string Title { get; set; } = "Feature details";
        public DiagramAsCode[] DiagramAsCode { get; set; } = Array.Empty<DiagramAsCode>();
        public string DiagramsAsCodeCodeBehindTitle { get; set; }
        public bool StepsHiddenInitially { get; set; }

        public CustomisableHtmlResultTextWriter(Stream outputStream, IFeatureResult[] features) : base(outputStream, 
            features,
            stylesPath: "LightBDD.Contrib.ReportingEnhancements.Reports.styles.css",
            scriptsPath: "LightBDD.Contrib.ReportingEnhancements.Reports.scripts.js",
            favicoPath: "LightBDD.Contrib.ReportingEnhancements.Reports.lightbdd_small.ico",
            svgPath: "LightBDD.Contrib.ReportingEnhancements.Reports.lightbdd_opt.svg")
        { }

        public override void Write(HtmlReportFormatterOptions options)
        {
            var bodyContent = new List<IHtmlNode>();

            if (IncludeExecutionSummary)
                bodyContent.Add(WriteExecutionSummary());

            if (IncludeFeatureSummary)
                bodyContent.Add(WriteFeatureSummary());

            bodyContent.Add(WriteFeatureDetails());
            bodyContent.Add(Html.Tag(Html5Tag.Div).Class("footer").Content(Html.Text("Generated with "), Html.Tag(Html5Tag.A).Content("LightBDD v" + GetLightBddVersion()).Href("https://github.com/LightBDD/LightBDD")));
            bodyContent.Add(Html.Tag(Html5Tag.Script).Content("initialize();", false, false));

            _writer
                .WriteTag(Html.Text("<!DOCTYPE HTML>"))
                .WriteTag(Html.Tag(Html5Tag.Html).Attribute("lang", "en").Content(
                    Html.Tag(Html5Tag.Head).Content(
                        Html.Tag(Html5Tag.Meta).Attribute(Html5Attribute.Charset, "UTF-8"),
                        Html.Tag(Html5Tag.Meta).Attribute(Html5Attribute.Name, "viewport").Attribute(Html5Attribute.Content, "width=device-width, initial-scale=1"),
                        GetFavicon(options.CustomFavicon),
                        Html.Tag(Html5Tag.Title).Content("Summary"),
                        Html.Tag(Html5Tag.Style).Content(EmbedCssImages(options), false, false),
                        Html.Tag(Html5Tag.Style).Content(_styles, false, false),
                        Html.Tag(Html5Tag.Style).Content(options.CssContent, false, false).SkipEmpty(),
                        Html.Tag(Html5Tag.Script).Content(_scripts, false, false)),
                    Html.Tag(Html5Tag.Body).Content(bodyContent)));
        }

        protected override IHtmlNode GetScenario(IScenarioResult scenario, int featureIndex, int scenarioIndex)
        {
            var scenarioContent = new List<IHtmlNode>
            {

                Html.Tag(Html5Tag.Div).Class("categories")
                    .Content(scenario.Info.Categories.Select(GetCategory))
                    .SkipEmpty(),
                Html.Tag(Html5Tag.Div).Class("scenario-steps").Content(scenario.GetSteps().Select(GetStep))
            };

            if (IncludeDiagramsAsCode)
            {
                var diagram = DiagramAsCode.SingleOrDefault(x => x.TestRuntimeId == scenario.Info.RuntimeId);

                if (diagram is not null)
                {
                    scenarioContent.Add(Html.Tag(Html5Tag.Details).Class("example-diagrams").Attribute("open", "").Content
                    (
                        Html.Tag(Html5Tag.Summary).Content("Example Diagram").Class("h4"),
                        Html.Tag(Html5Tag.Details).Class("example").Content
                        (
                            Html.Tag(Html5Tag.Summary).Class("example-image").Content
                            (
                                Html.Tag(Html5Tag.Img).Attribute(Html5Attribute.Src, diagram.ImgSrc)
                            ),
                            Html.Tag(Html5Tag.Div).Class("raw-plantuml").Content
                            (
                                Html.Tag(Html5Tag.H4).Content(DiagramsAsCodeCodeBehindTitle),
                                Html.Tag(Html5Tag.Pre).Content(diagram.CodeBehind)
                            )
                        )
                    ));
                }
            }

            var toggleId = $"toggle{featureIndex}_{scenarioIndex}";
            var scenarioId = $"scenario{featureIndex}_{scenarioIndex + 1}";
            var checkbox = Html.Checkbox().Id(toggleId).Class("toggle toggleS");

            if (!StepsHiddenInitially)
                checkbox.Checked();

            return Html.Tag(Html5Tag.Div).Class("scenario " + GetStatusClass(scenario.Status))
                .Attribute("data-categories", GetScenarioCategories(scenario))
                .Content(
                    checkbox,
                    Html.Tag(Html5Tag.H3).Id(scenarioId).Class("header title").Content(
                        Html.Tag(Html5Tag.Label).For(toggleId).Class("controls").Content(
                            GetCheckBoxTag(),
                            GetStatus(scenario.Status)),
                        Html.Tag(Html5Tag.Span).Content(
                            Html.Text(scenario.Info.Name.Format(StepNameDecorator)),
                            Html.Tag(Html5Tag.Span).Content(scenario.Info.Labels.Select(GetLabel)).SkipEmpty(),
                            GetDuration(scenario.ExecutionTime),
                            GetSmallLink(scenarioId))),
                    Html.Tag(Html5Tag.Div).Class("content").Content(scenarioContent),
                    Html.Tag(Html5Tag.Div).Class("details").Content(
                        GetStatusDetails(scenario.StatusDetails),
                        GetComments(scenario.GetAllSteps()),
                        GetAttachments(scenario.GetAllSteps())).SkipEmpty());
        }


        protected override IEnumerable<IHtmlNode> GetFeatureDetailsContent()
        {
            yield return Html.Tag(Html5Tag.H1).Id("featureDetails").Content(Html.Text(Title), GetSmallLink("featureDetails"));
            yield return Html.Tag(Html5Tag.Div).Class("optionsPanel").Content(
                GetToggleNodes(),
                GetStatusFilterNodes(),
                GetCategoryFilterNodes(),
                GetFilterFreeTextNodes(),
                Html.Tag(Html5Tag.A).Class("shareable").Href("").Content("filtered link", false, false).Id("optionsLink").SpaceBefore());

            for (var i = 0; i < _features.Length; ++i)
                yield return GetFeatureDetails(_features[i], i + 1);
        }

        protected virtual IHtmlNode GetFilterFreeTextNodes()
        {
            return Html.Tag(Html5Tag.Div).Class("options filterFreeTextPanel").Content(
                Html.Tag(Html5Tag.Span).Content("Filter:"),
                Html.Tag(Html5Tag.Span).Content(
                    Html.Tag(Html5Tag.Input).Id("searchbar").Attribute("type", "input").Attribute("onkeyup", "search_scenarios()")));
        }

        protected override IHtmlNode GetStatusFilterNodes()
        {
            var optionsClasses = "options";
            if (!ShowStatusFilterToggles)
                optionsClasses += " hide";

            return Html.Tag(Html5Tag.Div).Class(optionsClasses).Content(
                Html.Tag(Html5Tag.Span).Content("Filter:"),
                Html.Tag(Html5Tag.Span).Content(
                    GetOptionNode("showPassed", GetStatusFilter(ExecutionStatus.Passed), "Passed"),
                    GetOptionNode("showBypassed", GetStatusFilter(ExecutionStatus.Bypassed), "Bypassed"),
                    GetOptionNode("showFailed", GetStatusFilter(ExecutionStatus.Failed), "Failed"),
                    GetOptionNode("showIgnored", GetStatusFilter(ExecutionStatus.Ignored), "Ignored"),
                    GetOptionNode("showNotRun", GetStatusFilter(ExecutionStatus.NotRun), "Not Run")));
        }

        protected virtual IHtmlNode GetToggleNodes()
        {
            var toggles = new List<IHtmlNode>
            {

                GetOptionNode(
                    "toggleFeatures",
                    Html.Checkbox().Checked().SpaceBefore().OnClick("checkAll('toggleF',toggleFeatures.checked)"),
                    "Features"),
                GetOptionNode(
                    "toggleScenarios",
                    Html.Checkbox().Checked().SpaceBefore().OnClick("checkAll('toggleS',toggleScenarios.checked)"),
                    "Scenarios"),
                GetOptionNode(
                    "toggleSubSteps",
                    Html.Checkbox().Checked().SpaceBefore().OnClick("checkAll('toggleSS',toggleSubSteps.checked)"),
                    "Sub Steps"),
                GetOptionNode(
                    "toggleExampleDiagrams",
                    Html.Checkbox().Checked().SpaceBefore().OnClick("toggleDiagrams(this.checked)"),
                    "Diagrams",
                    !ShowExampleDiagramsToggle),
                GetOptionNode(
                    "toggleHappyPath",
                    Html.Checkbox().SpaceBefore().OnClick("toggleHappyPathsOnly(this.checked)"),
                    "Happy Paths Only",
                    !ShowHappyPathToggle)
            };

            return Html.Tag(Html5Tag.Div).Class("options").Content(
                Html.Tag(Html5Tag.Span).Content("Toggle:"),
                Html.Tag(Html5Tag.Span).Content(toggles));
        }

        protected override IHtmlNode GetOptionNode(string elementId, TagBuilder element, string labelContent, bool hide = false)
        {
            var optionClasses = "option";

            if (hide)
                optionClasses += " hide";

            return Html.Tag(Html5Tag.Span).Class(optionClasses).Content(element.Id(elementId),
                Html.Tag(Html5Tag.Label).Content(GetCheckBoxTag(), Html.Text(labelContent)).For(elementId));
        }

        protected override IHtmlNode GetStep(IStepResult step)
        {
            var toggleId = WriteRuntimeIds ? step.Info.RuntimeId.ToString() : "toggleId" + GetNewToggleId();
            var hasSubSteps = step.GetSubSteps().Any();

            var checkbox = hasSubSteps
                ? Html.Checkbox().Id(toggleId).Class("toggle toggleSS").Checked()
                : Html.Nothing();

            var container = hasSubSteps
                ? Html.Tag(Html5Tag.Label).For(toggleId)
                : Html.Tag(Html5Tag.Span);

            return Html.Tag(Html5Tag.Div).Class("step").Content(
                checkbox,
                Html.Tag(Html5Tag.Div).Class("header").Content(
                    container.Class("controls").Content(
                        GetCheckBoxTag(!hasSubSteps),
                        GetStatus(step.Status)),
                    Html.Tag(Html5Tag.Span).Content(
                        Html.Text($"{WebUtility.HtmlEncode(step.Info.GroupPrefix)}{step.Info.Number}. {step.Info.Name.Format(StepNameDecorator)}").Trim(),
                        GetDuration(step.ExecutionTime))),
                Html.Tag(Html5Tag.Div).Class("step-parameters")
                    .Content(step.Parameters.Select(GetStepParameter))
                    .SkipEmpty(),
                Html.Tag(Html5Tag.Div).Class("sub-steps").Content(step.GetSubSteps().Select(GetStep))
                    .SkipEmpty());
        }

        protected virtual IHtmlNode GetDuration(ExecutionTime executionTime)
        {
            return Html.Tag(Html5Tag.Span)
                .Class("duration")
                .Content(executionTime != null && IncludeDurations ? $"({executionTime.Duration.FormatPretty()})" : string.Empty)
                .SkipEmpty()
                .SpaceBefore();
        }

        private int GetNewToggleId() => _currentToggleIdNumber++;
    }
}