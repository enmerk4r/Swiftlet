using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
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
    public class SaveWebResponse : GH_Component
    {
        private bool _textChecked;
        private bool _binaryChecked;

        /// <summary>
        /// Initializes a new instance of the CreatePostBody class.
        /// </summary>
        public SaveWebResponse()
          : base("Save Web Response", "SWR",
              "Save Web Response to disk",
              NamingUtility.CATEGORY, NamingUtility.UTILITIES)
        {
            this._binaryChecked = true;
            this.UpdateMessage();
        }

        public override GH_Exposure Exposure => GH_Exposure.primary;

        public override bool Read(GH_IReader reader)
        {
            this._textChecked = reader.GetBoolean(nameof(this._textChecked));
            this._binaryChecked = reader.GetBoolean(nameof(this._binaryChecked));

            this.UpdateMessage();
            return base.Read(reader);
        }



        public override bool Write(GH_IWriter writer)
        {
            writer.SetBoolean(nameof(this._textChecked), this._textChecked);
            writer.SetBoolean(nameof(this._binaryChecked), this._binaryChecked);

            return base.Write(writer);
        }

        public void UpdateMessage()
        {
            if (_textChecked) this.Message = "Text";
            else if (_binaryChecked) this.Message = "Binary";
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddParameter(new HttpWebResponseParam(), "Response", "R", "Full Http response object (with metadata)", GH_ParamAccess.item);
            pManager.AddTextParameter("Path", "P", "Path to file", GH_ParamAccess.item);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddIntegerParameter("Bytes", "B", "Size of saved file in bytes", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            HttpWebResponseGoo goo = null;
            string path = string.Empty;

            DA.GetData(0, ref goo);
            DA.GetData(1, ref path);

            HttpResponseDTO response = goo.Value;

            if (this._binaryChecked)
            {
                using (BinaryWriter writer = new BinaryWriter(new FileStream(path, FileMode.Create)))
                {
                    writer.Write(response.Bytes);
                }
            }
            else if (this._textChecked)
            {
                using (StreamWriter writer = new StreamWriter(path))
                {
                    writer.Write(response.Content);
                }
            }

            long length = new System.IO.FileInfo(path).Length;

            DA.SetData(0, length);
        }

        public override void AppendAdditionalMenuItems(ToolStripDropDown menu)
        {
            base.AppendAdditionalMenuItems(menu);
            Menu_AppendItem(menu, "Text", Menu_TextClick, true, this._textChecked);
            Menu_AppendItem(menu, "Binary", Menu_JavascriptClick, true, this._binaryChecked);
        }

        private void Menu_TextClick(object sender, EventArgs args)
        {
            this.UncheckAll();
            this._textChecked = true;
            this.UpdateMessage();
        }

        private void Menu_JavascriptClick(object sender, EventArgs args)
        {
            this.UncheckAll();
            this._binaryChecked = true;
            this.UpdateMessage();
        }

        private void UncheckAll()
        {
            this._textChecked = false;
            this._binaryChecked = false;
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
                return Properties.Resources.Icons_save_web_response_24x24;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("e978195c-b6dc-4be5-bf98-0246bbacec7c"); }
        }
    }

}