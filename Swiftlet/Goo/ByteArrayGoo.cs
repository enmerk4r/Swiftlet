using Grasshopper.Kernel.Types;
using Swiftlet.DataModels.Implementations;
using Swiftlet.DataModels.Interfaces;
using Swiftlet.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Swiftlet.Goo
{
    public class ByteArrayGoo : GH_Goo<byte[]>
    {
        public override bool IsValid => this.Value != null;

        public override string TypeName => "Byte Array";

        public override string TypeDescription => "Grasshopper wrapper for a byte array";

        public ByteArrayGoo()
        {
            this.Value = new byte[0];
        }

        public ByteArrayGoo(byte[] data)
        {
            this.Value = data.ToArray();
        }

        public override IGH_Goo Duplicate()
        {
            return new ByteArrayGoo(this.Value);
        }

        public override string ToString()
        {
            return $"BYTE ARRAY [ {this.Value.Length} ]";
        }
    }
}
