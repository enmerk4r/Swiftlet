using Grasshopper.Kernel.Types;
using Swiftlet.DataModels.Implementations;
using System;
using System.Linq;

namespace Swiftlet.Goo
{
    public class MultipartFieldGoo : GH_Goo<MultipartField>
    {
        public override bool IsValid => this.Value != null;

        public override string TypeName => "Multipart Field";

        public override string TypeDescription => "A multipart/form-data field";

        public MultipartFieldGoo()
        {
            this.Value = new MultipartField(string.Empty, new byte[0]);
        }

        public MultipartFieldGoo(MultipartField field)
        {
            this.Value = field?.Duplicate();
        }

        public override IGH_Goo Duplicate()
        {
            return new MultipartFieldGoo(this.Value?.Duplicate());
        }

        public override string ToString()
        {
            if (this.Value == null)
            {
                return "MULTIPART FIELD";
            }

            string name = string.IsNullOrEmpty(this.Value.Name) ? "<unnamed>" : this.Value.Name;
            int length = this.Value.Bytes?.Length ?? 0;
            return $"MULTIPART FIELD [ {name} | {length} bytes ]";
        }
    }
}
