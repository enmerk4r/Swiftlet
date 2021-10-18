using System;
using System.Collections.Generic;
using System.Windows.Forms;
using Grasshopper.Kernel;
using Rhino.Geometry;
using Swiftlet.DataModels.Enums;
using Swiftlet.DataModels.Implementations;
using Swiftlet.Goo;
using Swiftlet.Params;
using Swiftlet.Util;

namespace Swiftlet.Components
{
    public class CreateTextBody : GH_Component
    {
        private ContentType _cType;

        private bool IsTextChecked { get; set; }
        private bool IsJavascriptChecked { get; set; }
        private bool IsJsonChecked { get; set; }
        private bool IsHtmlChecked { get; set; }
        private bool IsXmlChecked { get; set; }

        /// <summary>
        /// Initializes a new instance of the CreatePostBody class.
        /// </summary>
        public CreateTextBody()
          : base("Create Text Body", "CTB",
              "Create a Request Body that supports text formats",
              NamingUtility.CATEGORY, NamingUtility.REQUEST)
        {
            _cType = ContentType.JSON;
            this.IsJsonChecked = true;
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("Content", "C", "Text contents of your request body", GH_ParamAccess.item, string.Empty);
            pManager[0].Optional = true;
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddParameter(new RequestBodyParam(), "Body", "B", "Request Body", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            string txt = string.Empty;
            DA.GetData(0, ref txt);

            RequestBodyText txtBody = new RequestBodyText(this._cType, txt);
            RequestBodyGoo goo = new RequestBodyGoo(txtBody);

            DA.SetData(0, goo);
            this.Message = this._cType.ToString();
        }

        public override void AppendAdditionalMenuItems(ToolStripDropDown menu)
        {
            base.AppendAdditionalMenuItems(menu);
            Menu_AppendItem(menu, "Text", Menu_TextClick, true, this.IsTextChecked);
            Menu_AppendItem(menu, "JavaScript", Menu_JavascriptClick, true, this.IsJavascriptChecked);
            Menu_AppendItem(menu, "JSON", Menu_JsonClick, true, this.IsJsonChecked);
            Menu_AppendItem(menu, "HTML", Menu_HtmlClick, true, this.IsHtmlChecked);
            Menu_AppendItem(menu, "XML", Menu_XmlClick, true, this.IsXmlChecked);
        }

        private void Menu_TextClick(object sender, EventArgs args)
        {
            this._cType = ContentType.Text;
            this.UncheckAll();
            this.IsTextChecked = true;
            this.ExpireSolution(true);
        }

        private void Menu_JavascriptClick(object sender, EventArgs args)
        {
            this._cType = ContentType.JavaScript;
            this.UncheckAll();
            this.IsJavascriptChecked = true;
            this.ExpireSolution(true);
        }

        private void Menu_JsonClick(object sender, EventArgs args)
        {
            this._cType = ContentType.JSON;
            this.UncheckAll();
            this.IsJsonChecked = true;
            this.ExpireSolution(true);
        }

        private void Menu_HtmlClick(object sender, EventArgs args)
        {
            this._cType = ContentType.HTML;
            this.UncheckAll();
            this.IsHtmlChecked = true;
            this.ExpireSolution(true);
        }

        private void Menu_XmlClick(object sender, EventArgs args)
        {
            this._cType = ContentType.XML;
            this.UncheckAll();
            this.IsXmlChecked = true;
            this.ExpireSolution(true);
        }

        public void UncheckAll()
        {
            this.IsTextChecked = false;
            this.IsJavascriptChecked = false;
            this.IsJsonChecked = false;
            this.IsHtmlChecked = false;
            this.IsXmlChecked = false;
        }

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                //You can add image files to your project resources and access them like this:
                // return Resources.IconForThisComponent;
                return null;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("87d53fb2-ba5d-46dc-bc7b-92eac5aebc6c"); }
        }
    }
}