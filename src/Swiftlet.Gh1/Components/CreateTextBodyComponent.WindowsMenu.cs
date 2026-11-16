using System.Windows.Forms;
using Swiftlet.Core.Http;

namespace Swiftlet.Gh.Rhino8.Components;

public sealed partial class CreateTextBodyComponent
{
    public override void AppendAdditionalMenuItems(ToolStripDropDown menu)
    {
        base.AppendAdditionalMenuItems(menu);
        Menu_AppendItem(menu, "Text", MenuTextClick, true, _isTextChecked);
        Menu_AppendItem(menu, "JavaScript", MenuJavascriptClick, true, _isJavascriptChecked);
        Menu_AppendItem(menu, "JSON", MenuJsonClick, true, _isJsonChecked);
        Menu_AppendItem(menu, "HTML", MenuHtmlClick, true, _isHtmlChecked);
        Menu_AppendItem(menu, "XML", MenuXmlClick, true, _isXmlChecked);
    }

    private void MenuTextClick(object sender, EventArgs args)
    {
        _contentType = ContentTypes.TextPlain;
        UncheckAll();
        _isTextChecked = true;
        ExpireSolution(true);
    }

    private void MenuJavascriptClick(object sender, EventArgs args)
    {
        _contentType = ContentTypes.JavaScript;
        UncheckAll();
        _isJavascriptChecked = true;
        ExpireSolution(true);
    }

    private void MenuJsonClick(object sender, EventArgs args)
    {
        _contentType = ContentTypes.ApplicationJson;
        UncheckAll();
        _isJsonChecked = true;
        ExpireSolution(true);
    }

    private void MenuHtmlClick(object sender, EventArgs args)
    {
        _contentType = ContentTypes.TextHtml;
        UncheckAll();
        _isHtmlChecked = true;
        ExpireSolution(true);
    }

    private void MenuXmlClick(object sender, EventArgs args)
    {
        _contentType = ContentTypes.ApplicationXml;
        UncheckAll();
        _isXmlChecked = true;
        ExpireSolution(true);
    }
}
