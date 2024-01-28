namespace LightBDD.Contrib.ReportingEnhancements.Reports
{
    public interface IHtmlNode
    {
        HtmlTextWriter Write(HtmlTextWriter writer);
        bool IsEmpty();
    }
}