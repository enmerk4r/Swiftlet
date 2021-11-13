using System;
using System.Drawing;
using Grasshopper.Kernel;

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
                return null;
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
    }
}
