using System;
using System.Collections.Generic;
using System.Xml;
using Grasshopper.Kernel;
using Swiftlet.Goo;
using Swiftlet.Params;
using Swiftlet.Util;

namespace Swiftlet.Components
{
    public class CreateXmlElementComponent : GH_Component
    {
        public CreateXmlElementComponent()
          : base("Create XML Element", "CXE",
              "Create an XML element with optional attributes and child elements",
              NamingUtility.CATEGORY, NamingUtility.REQUEST)
        {
        }

        public override GH_Exposure Exposure => GH_Exposure.quinary;

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("Name", "N", "Element tag name", GH_ParamAccess.item);
            pManager.AddTextParameter("Text", "T", "Inner text content", GH_ParamAccess.item);
            pManager.AddTextParameter("Namespace", "NS", "XML namespace URI", GH_ParamAccess.item);
            pManager.AddTextParameter("Attr Names", "AN", "List of attribute names", GH_ParamAccess.list);
            pManager.AddTextParameter("Attr Values", "AV", "List of attribute values", GH_ParamAccess.list);
            pManager.AddParameter(new XmlNodeParam(), "Children", "C", "Child XML elements", GH_ParamAccess.list);

            pManager[1].Optional = true;
            pManager[2].Optional = true;
            pManager[3].Optional = true;
            pManager[4].Optional = true;
            pManager[5].Optional = true;
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddParameter(new XmlNodeParam(), "Element", "E", "Created XML element", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            string name = string.Empty;
            string text = null;
            string ns = null;
            List<string> attrNames = new List<string>();
            List<string> attrValues = new List<string>();
            List<XmlNodeGoo> children = new List<XmlNodeGoo>();

            if (!DA.GetData(0, ref name)) return;
            DA.GetData(1, ref text);
            DA.GetData(2, ref ns);
            DA.GetDataList(3, attrNames);
            DA.GetDataList(4, attrValues);
            DA.GetDataList(5, children);

            if (string.IsNullOrWhiteSpace(name))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Element name cannot be empty");
                return;
            }

            if (attrNames.Count != attrValues.Count)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Attribute names and values must have the same count");
                return;
            }

            // Create a document to host the element
            XmlDocument doc = new XmlDocument();
            XmlElement element;

            if (!string.IsNullOrEmpty(ns))
            {
                element = doc.CreateElement(name, ns);
            }
            else
            {
                element = doc.CreateElement(name);
            }

            // Set inner text if provided
            if (!string.IsNullOrEmpty(text))
            {
                element.InnerText = text;
            }

            // Add attributes
            for (int i = 0; i < attrNames.Count; i++)
            {
                string attrName = attrNames[i];
                string attrValue = attrValues[i];

                if (string.IsNullOrWhiteSpace(attrName))
                {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, $"Skipping attribute at index {i}: name is empty");
                    continue;
                }

                element.SetAttribute(attrName, attrValue ?? string.Empty);
            }

            // Add child elements
            foreach (XmlNodeGoo childGoo in children)
            {
                if (childGoo == null || childGoo.Value == null) continue;

                // Import the child node into this document
                XmlNode importedChild = doc.ImportNode(childGoo.Value, true);
                element.AppendChild(importedChild);
            }

            DA.SetData(0, new XmlNodeGoo(element));
        }

        protected override System.Drawing.Bitmap Icon => Properties.Resources.Icons_create_xml_element;

        public override Guid ComponentGuid => new Guid("F7E8D9C0-B1A2-4C3D-8E5F-6A7B8C9D0E1F");
    }
}
