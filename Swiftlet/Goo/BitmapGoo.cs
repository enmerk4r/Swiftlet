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
            if (this.Value == null) return "Null Bitmap";
            return $"BITMAP [ {this.Value.Width} x {this.Value.Height} ]";
        }

        public override bool CastTo<Q>(ref Q target)
        {
            // Cast to System.Drawing.Bitmap
            if (typeof(Q).IsAssignableFrom(typeof(Bitmap)))
            {
                if (this.Value != null)
                {
                    object obj = this.Value;
                    target = (Q)obj;
                    return true;
                }
            }

            return base.CastTo(ref target);
        }

        public override bool CastFrom(object source)
        {
            if (source == null) return false;

            // Cast from System.Drawing.Bitmap
            if (source is Bitmap bitmap)
            {
                this.Value = bitmap;
                return true;
            }

            // Cast from another BitmapGoo
            if (source is BitmapGoo goo)
            {
                this.Value = goo.Value;
                return true;
            }

            return base.CastFrom(source);
        }
    }
}
