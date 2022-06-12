using Grasshopper.GUI;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using Newtonsoft.Json.Linq;
using Swiftlet.Goo;
using Swiftlet.Util;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Swiftlet.Params
{
    public class HttpHeaderParam : GH_PersistentParam<HttpHeaderGoo>
    {
        public HttpHeaderParam()
            : base("Http Header", "H",
                 "A collection of Http Headers",
                 NamingUtility.CATEGORY, NamingUtility.REQUEST)
        {
        }

        public override GH_Exposure Exposure => GH_Exposure.quarternary;
        public override Guid ComponentGuid => new Guid("521dae84-7074-433e-9714-9144c42b92a4");

        protected override Bitmap Icon => Properties.Resources.Icons_http_header_param_24x24;

        protected override GH_GetterResult Prompt_Plural(ref List<HttpHeaderGoo> values)
        {
            return GH_GetterResult.cancel;
        }

        protected override GH_GetterResult Prompt_Singular(ref HttpHeaderGoo value)
        {
            return GH_GetterResult.cancel;
        }

        protected override ToolStripMenuItem Menu_CustomSingleValueItem()
        {
            string text = string.Empty;
            if (base.PersistentDataCount == 1)
            {
                HttpHeaderGoo headerGoo = base.PersistentData.get_FirstItem(false);
                if (headerGoo != null)
                {
                    text = headerGoo.Value.ToJsonString();
                }
            }
            ToolStripMenuItem toolStripMenuItem = new ToolStripMenuItem($"Set {this.TypeName}");
            GH_DocumentObject.Menu_AppendTextItem(toolStripMenuItem.DropDown, text, null, null, enabled: true, 200, lockOnFocus: false);
            return toolStripMenuItem;
        }

        protected override ToolStripMenuItem Menu_CustomMultiValueItem()
        {
            ToolStripMenuItem toolStripMenuItem = new ToolStripMenuItem("Set Multiple " + GH_Convert.ToPlural(this.TypeName));
            GH_DocumentObject.Menu_AppendTextItem(toolStripMenuItem.DropDown, null, null, null, enabled: false, 200, lockOnFocus: false);
            return toolStripMenuItem;
        }
    }
}
