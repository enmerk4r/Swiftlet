using Swiftlet.DataModels.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Swiftlet.DataModels.Interfaces
{
    public interface IRequestBody
    {
        ContentType ContentType { get; }
        object Value { get; }

        IRequestBody Duplicate();
    }
}
