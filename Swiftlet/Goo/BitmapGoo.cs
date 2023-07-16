using Grasshopper.Kernel.Types;
using Swiftlet.DataModels.Implementations;
using Swiftlet.DataModels.Interfaces;
using Swiftlet.Util;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Swiftlet.Goo
{
    public class BitmapGoo : GH_Goo<Bitmap>
    {
        public override bool IsValid => this.Value != null;

        public override string TypeName => "Bitmap";

        public override string TypeDescription => "Grasshopper wrapper for a bitmap";

        public BitmapGoo()
        {
            this.Value = null;
        }

        public BitmapGoo(Bitmap map)
        {
            this.Value = map;
        }

        public override IGH_Goo Duplicate()
        {
            return new BitmapGoo(this.Value);
        }

        public override string ToString()
        {
            return $"BITMAP [ {this.Value.Width} x {this.Value.Height} ]";
        }
    }
}
