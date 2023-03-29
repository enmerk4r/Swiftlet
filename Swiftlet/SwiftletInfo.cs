using System;
using System.Drawing;
using System.Reflection;
using Grasshopper;
using Grasshopper.Kernel;
using Rhino.Geometry;

namespace Swiftlet
{
    public class SwiftletInfo : GH_AssemblyInfo
    {
        public override string Name
        {
            get
            {
                return "Swiftlet";
            }
        }
        public override Bitmap Icon
        {
            get
            {
                //Return a 24x24 pixel bitmap to represent this GHA library.
                return Properties.Resources.Logo_24x24;
            }
        }
        public override string Description
        {
            get
            {
                //Return a short string describing the purpose of this GHA library.
                return "Grasshopper plugin for accessing Web APIs";
            }
        }
        public override Guid Id
        {
            get
            {
                return new Guid("dab9af34-1544-4374-96b2-288f6f4788dc");
            }
        }

        public override string AuthorName
        {
            get
            {
                //Return a string identifying you or your company.
                return "Sergey Pigach";
            }
        }
        public override string AuthorContact
        {
            get
            {
                //Return a string representing your preferred contact details.
                return "";
            }
        }
        public override string Version
        {
            get
            {
                return AssemblyVersion;
            }
        }

        public override string AssemblyVersion
        {
            get
            {
                var assembly = Assembly.GetExecutingAssembly();
                var assemblyName = new AssemblyName(assembly.FullName);
                return assemblyName.Version.ToString();
            }
        }

    }

    public class SwiftletCategoryIcon : GH_AssemblyPriority
    {
        public object Properties { get; private set; }

        public override GH_LoadingInstruction PriorityLoad()
        {
            Instances.ComponentServer.AddCategoryIcon("Swiftlet", Swiftlet.Properties.Resources.Logo_16x16);
            Instances.ComponentServer.AddCategorySymbolName("Swiftlet", 'S');
            return GH_LoadingInstruction.Proceed;
        }
    }
}
