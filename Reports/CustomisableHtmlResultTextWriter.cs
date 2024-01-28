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
        public bool FormatResult { get; set; }

        public CustomisableHtmlResultTextWriter(Stream outputStream, IFeatureResult[] features) : base(outputStream, 
            features,
            stylesPath: "LightBDD.Contrib.ReportingEnhancements.Reports.styles.css",
            scriptsPath: "LightBDD.Contrib.ReportingEnhancements.Reports.scripts.js",
            favicoPath: "LightBDD.Contrib.ReportingEnhancements.Reports.lightbdd_small.ico",
            svgPath: "LightBDD.Contrib.ReportingEnhancements.Reports.lightbdd_opt.svg")
        { }

        public override void Write(HtmlReportFormatterOptions options)
        {
            _html = new Html(FormatResult);
            _stepNameDecorator = new HtmlStepNameDecorator(_html);

            var bodyContent = new List<IHtmlNode>();

            if (IncludeExecutionSummary)
                bodyContent.Add(WriteExecutionSummary());

            if (IncludeFeatureSummary)
                bodyContent.Add(WriteFeatureSummary());

            bodyContent.Add(WriteFeatureDetails());
            bodyContent.Add(_html.Tag(Html5Tag.Div).Class("footer").Content(Html.Text("Generated with "), _html.Tag(Html5Tag.A).Content("LightBDD v" + GetLightBddVersion()).Href("https://github.com/LightBDD/LightBDD")));
            bodyContent.Add(_html.Tag(Html5Tag.Script).Content("initialize();", false, false));

            _writer
                .WriteTag(Html.Text("<!DOCTYPE HTML>"))
                .WriteTag(_html.Tag(Html5Tag.Html).Attribute("lang", "en").Content(
                    _html.Tag(Html5Tag.Head).Content(
                        _html.Tag(Html5Tag.Meta).Attribute(Html5Attribute.Charset, "UTF-8"),
                        _html.Tag(Html5Tag.Meta).Attribute(Html5Attribute.Name, "viewport").Attribute(Html5Attribute.Content, "width=device-width, initial-scale=1"),
                        GetFavicon(options.CustomFavicon),
                        _html.Tag(Html5Tag.Title).Content("Summary"),
                        _html.Tag(Html5Tag.Style).Content(EmbedCssImages(options), false, false),
                        _html.Tag(Html5Tag.Style).Content(_styles, false, false),
                        _html.Tag(Html5Tag.Style).Content(options.CssContent, false, false).SkipEmpty(),
                        _html.Tag(Html5Tag.Script).Content(_scripts, false, false)),
                    _html.Tag(Html5Tag.Body).Content(bodyContent)));
        }

        protected override IHtmlNode GetScenario(IScenarioResult scenario, int featureIndex, int scenarioIndex)
        {
            var scenarioContent = new List<IHtmlNode>
            {

                _html.Tag(Html5Tag.Div).Class("categories")
                    .Content(scenario.Info.Categories.Select(GetCategory))
                    .SkipEmpty(),
                _html.Tag(Html5Tag.Div).Class("scenario-steps").Content(scenario.GetSteps().Select(GetStep))
            };

            if (IncludeDiagramsAsCode)
            {
                var diagram = DiagramAsCode.SingleOrDefault(x => x.TestRuntimeId == scenario.Info.RuntimeId);

                if (diagram is not null)
                {
                    scenarioContent.Add(_html.Tag(Html5Tag.Details).Class("example-diagrams").Attribute("open", "").Content
                    (
                        _html.Tag(Html5Tag.Summary).Content("Example Diagram").Class("h4"),
                        _html.Tag(Html5Tag.Details).Class("example").Content
                        (
                            _html.Tag(Html5Tag.Summary).Class("example-image").Content
                            (
                                _html.Tag(Html5Tag.Img).Attribute(Html5Attribute.Src, diagram.ImgSrc)
                            ),
                            _html.Tag(Html5Tag.Div).Class("raw-plantuml").Content
                            (
                                _html.Tag(Html5Tag.H4).Content(DiagramsAsCodeCodeBehindTitle),
                                _html.Tag(Html5Tag.Pre).Content(diagram.CodeBehind)
                            )
                        )
                    ));
                }
            }

            var toggleId = $"toggle{featureIndex}_{scenarioIndex}";
            var scenarioId = $"scenario{featureIndex}_{scenarioIndex + 1}";
            var checkbox = _html.Checkbox().Id(toggleId).Class("toggle toggleS");

            if (!StepsHiddenInitially)
                checkbox.Checked();

            return _html.Tag(Html5Tag.Div).Class("scenario " + GetStatusClass(scenario.Status))
                .Attribute("data-categories", GetScenarioCategories(scenario))
                .Content(
                    checkbox,
                    _html.Tag(Html5Tag.H3).Id(scenarioId).Class("header title").Content(
                        _html.Tag(Html5Tag.Label).For(toggleId).Class("controls").Content(
                            GetCheckBoxTag(),
                            GetStatus(scenario.Status)),
                        _html.Tag(Html5Tag.Span).Content(
                            Html.Text(scenario.Info.Name.Format(_stepNameDecorator)),
                            _html.Tag(Html5Tag.Span).Content(scenario.Info.Labels.Select(GetLabel)).SkipEmpty(),
                            GetDuration(scenario.ExecutionTime),
                            GetSmallLink(scenarioId))),
                    _html.Tag(Html5Tag.Div).Class("content").Content(scenarioContent),
                    _html.Tag(Html5Tag.Div).Class("details").Content(
                        GetStatusDetails(scenario.StatusDetails),
                        GetComments(scenario.GetAllSteps()),
                        GetAttachments(scenario.GetAllSteps())).SkipEmpty());
        }


        protected override IEnumerable<IHtmlNode> GetFeatureDetailsContent()
        {
            yield return _html.Tag(Html5Tag.H1).Id("featureDetails").Content(Html.Text(Title), GetSmallLink("featureDetails"));
            yield return _html.Tag(Html5Tag.Div).Class("optionsPanel").Content(
                GetToggleNodes(),
                GetStatusFilterNodes(),
                GetCategoryFilterNodes(),
                GetFilterFreeTextNodes(),
                _html.Tag(Html5Tag.A).Class("shareable").Href("").Content("filtered link", false, false).Id("optionsLink").SpaceBefore());

            for (var i = 0; i < _features.Length; ++i)
                yield return GetFeatureDetails(_features[i], i + 1);
        }

        protected virtual IHtmlNode GetFilterFreeTextNodes()
        {
            return _html.Tag(Html5Tag.Div).Class("options filterFreeTextPanel").Content(
                _html.Tag(Html5Tag.Span).Content("Filter:"),
                _html.Tag(Html5Tag.Span).Content(
                    _html.Tag(Html5Tag.Input).Id("searchbar").Attribute("type", "input").Attribute("onkeyup", "search_scenarios()")));
        }

        protected override IHtmlNode GetStatusFilterNodes()
        {
            var optionsClasses = "options";
            if (!ShowStatusFilterToggles)
                optionsClasses += " hide";

            return _html.Tag(Html5Tag.Div).Class(optionsClasses).Content(
                _html.Tag(Html5Tag.Span).Content("Filter:"),
                _html.Tag(Html5Tag.Span).Content(
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
                    _html.Checkbox().Checked().SpaceBefore().OnClick("checkAll('toggleF',toggleFeatures.checked)"),
                    "Features"),
                GetOptionNode(
                    "toggleScenarios",
                    _html.Checkbox().Checked().SpaceBefore().OnClick("checkAll('toggleS',toggleScenarios.checked)"),
                    "Scenarios"),
                GetOptionNode(
                    "toggleSubSteps",
                    _html.Checkbox().Checked().SpaceBefore().OnClick("checkAll('toggleSS',toggleSubSteps.checked)"),
                    "Sub Steps"),
                GetOptionNode(
                    "toggleExampleDiagrams",
                    _html.Checkbox().Checked().SpaceBefore().OnClick("toggleDiagrams(this.checked)"),
                    "Diagrams",
                    !ShowExampleDiagramsToggle),
                GetOptionNode(
                    "toggleHappyPath",
                    _html.Checkbox().SpaceBefore().OnClick("toggleHappyPathsOnly(this.checked)"),
                    "Happy Paths Only",
                    !ShowHappyPathToggle)
            };

            return _html.Tag(Html5Tag.Div).Class("options").Content(
                _html.Tag(Html5Tag.Span).Content("Toggle:"),
                _html.Tag(Html5Tag.Span).Content(toggles));
        }

        protected override IHtmlNode GetOptionNode(string elementId, TagBuilder element, string labelContent, bool hide = false)
        {
            var optionClasses = "option";

            if (hide)
                optionClasses += " hide";

            return _html.Tag(Html5Tag.Span).Class(optionClasses).Content(element.Id(elementId),
                _html.Tag(Html5Tag.Label).Content(GetCheckBoxTag(), Html.Text(labelContent)).For(elementId));
        }

        protected override IHtmlNode GetStep(IStepResult step)
        {
            var toggleId = WriteRuntimeIds ? step.Info.RuntimeId.ToString() : "toggleId" + GetNewToggleId();
            var hasSubSteps = step.GetSubSteps().Any();

            var checkbox = hasSubSteps
                ? _html.Checkbox().Id(toggleId).Class("toggle toggleSS").Checked()
                : Html.Nothing();

            var container = hasSubSteps
                ? _html.Tag(Html5Tag.Label).For(toggleId)
                : _html.Tag(Html5Tag.Span);

            return _html.Tag(Html5Tag.Div).Class("step").Content(
                checkbox,
                _html.Tag(Html5Tag.Div).Class("header").Content(
                    container.Class("controls").Content(
                        GetCheckBoxTag(!hasSubSteps),
                        GetStatus(step.Status)),
                    _html.Tag(Html5Tag.Span).Content(
                        Html.Text($"{WebUtility.HtmlEncode(step.Info.GroupPrefix)}{step.Info.Number}. {step.Info.Name.Format(_stepNameDecorator)}").Trim(),
                        GetDuration(step.ExecutionTime))),
                _html.Tag(Html5Tag.Div).Class("step-parameters")
                    .Content(step.Parameters.Select(GetStepParameter))
                    .SkipEmpty(),
                _html.Tag(Html5Tag.Div).Class("sub-steps").Content(step.GetSubSteps().Select(GetStep))
                    .SkipEmpty());
        }

        protected override IHtmlNode GetDuration(ExecutionTime executionTime)
        {
            return _html.Tag(Html5Tag.Span)
                .Class("duration")
                .Content(executionTime != null && IncludeDurations ? $"({executionTime.Duration.FormatPretty()})" : string.Empty)
                .SkipEmpty()
                .SpaceBefore();
        }

        private int GetNewToggleId() => _currentToggleIdNumber++;
    }
}