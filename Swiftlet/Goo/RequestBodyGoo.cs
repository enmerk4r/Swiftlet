using Grasshopper.Kernel.Types;
using Swiftlet.DataModels.Enums;
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
    public class RequestBodyGoo : GH_Goo<IRequestBody>
    {
        public override bool IsValid => this.Value != null;

        public override string TypeName => "Request Body";

        public override string TypeDescription => "A request body";

        public RequestBodyGoo()
        {
            this.Value = new RequestBodyText();
        }

        public RequestBodyGoo(IRequestBody body)
        {
            this.Value = body.Duplicate();
        }

        public override IGH_Goo Duplicate()
        {
            return new RequestBodyGoo(this.Value);
        }

        public override string ToString()
        {
            return $"REQUEST BODY [ {HeaderUtility.GetContentType(this.Value.ContentType)} ]";
        }
    }
}
