using System;
using System.Collections.Generic;
using System.Windows.Forms;
using GH_IO.Serialization;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using Newtonsoft.Json.Linq;
using Rhino.Geometry;
using Swiftlet.DataModels.Implementations;
using Swiftlet.Goo;
using Swiftlet.Params;
using Swiftlet.Util;

namespace Swiftlet.Components
{
    public class CreateTextBody : GH_Component
    {
        private string _cType { get; set; }
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
            _cType = ContentTypeUtility.ApplicationJson;
            this.IsJsonChecked = true;
        }

        public override bool Read(GH_IReader reader)
        {
            this.IsTextChecked = reader.GetBoolean(nameof(this.IsTextChecked));
            this.IsJavascriptChecked = reader.GetBoolean(nameof(this.IsJavascriptChecked));
            this.IsJsonChecked = reader.GetBoolean(nameof(this.IsJsonChecked));
            this.IsHtmlChecked = reader.GetBoolean(nameof(this.IsHtmlChecked));
            this.IsXmlChecked = reader.GetBoolean(nameof(this.IsXmlChecked));

            if (this.IsTextChecked) _cType = ContentTypeUtility.TextPlain;
            else if (this.IsJavascriptChecked) _cType = ContentTypeUtility.JavaScript;
            else if (this.IsJsonChecked) _cType = ContentTypeUtility.ApplicationJson;
            else if (this.IsHtmlChecked) _cType =ContentTypeUtility.TextHtml;
            else if (this.IsXmlChecked) _cType = ContentTypeUtility.ApplicationXml;

            this.Message = ContentTypeUtility.ContentTypeToMessage(this._cType);
            return base.Read(reader);
        }

        

        public override bool Write(GH_IWriter writer)
        {
            writer.SetBoolean(nameof(this.IsTextChecked), this.IsTextChecked);
            writer.SetBoolean(nameof(this.IsJavascriptChecked), this.IsJavascriptChecked);
            writer.SetBoolean(nameof(this.IsJsonChecked), this.IsJsonChecked);
            writer.SetBoolean(nameof(this.IsHtmlChecked), this.IsHtmlChecked);
            writer.SetBoolean(nameof(this.IsXmlChecked), this.IsXmlChecked);
            return base.Write(writer);
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Content", "C", "Text contents of your request body", GH_ParamAccess.item);
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
            object input = null;
            string txt = string.Empty;

            DA.GetData(0, ref input);

            if (input == null) { } // Do nothing
            else if (input is GH_String)
            {
                GH_String str = input as GH_String;
                if (str != null)
                {
                    txt = str.ToString();
                }
            }
            else if (input is JArrayGoo)
            {
                JArrayGoo arrayGoo = input as JArrayGoo;
                if (arrayGoo != null)
                {
                    txt = arrayGoo.Value.ToString();
                }
            }
            else if (input is JObjectGoo)
            {
                JObjectGoo objectGoo = input as JObjectGoo;
                if (objectGoo != null)
                {
                    txt = objectGoo.Value.ToString();
                }
            }
            else if (input is JTokenGoo)
            {
                JTokenGoo tokenGoo = input as JTokenGoo;
                if (tokenGoo != null)
                {
                    txt = tokenGoo.Value.ToString();
                }
            }
            else
            {
                throw new Exception(" Content must be a string, a JObject or a JArray");
            }

            RequestBodyText txtBody = new RequestBodyText(this._cType, txt);
            RequestBodyGoo goo = new RequestBodyGoo(txtBody);

            DA.SetData(0, goo);
            this.Message = ContentTypeUtility.ContentTypeToMessage(this._cType);
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
            this._cType = ContentTypeUtility.TextPlain;
            this.UncheckAll();
            this.IsTextChecked = true;
            this.ExpireSolution(true);
        }

        private void Menu_JavascriptClick(object sender, EventArgs args)
        {
            this._cType = ContentTypeUtility.JavaScript;
            this.UncheckAll();
            this.IsJavascriptChecked = true;
            this.ExpireSolution(true);
        }

        private void Menu_JsonClick(object sender, EventArgs args)
        {
            this._cType = ContentTypeUtility.ApplicationJson;
            this.UncheckAll();
            this.IsJsonChecked = true;
            this.ExpireSolution(true);
        }

        private void Menu_HtmlClick(object sender, EventArgs args)
        {
            this._cType = ContentTypeUtility.TextHtml;
            this.UncheckAll();
            this.IsHtmlChecked = true;
            this.ExpireSolution(true);
        }

        private void Menu_XmlClick(object sender, EventArgs args)
        {
            this._cType = ContentTypeUtility.ApplicationXml;
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
                return Properties.Resources.Icons_create_text_body_24x24;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("d5bf7d4a-9fc3-4984-90f7-feb32aa96d9f"); }
        }
    }
}