using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Swiftlet.DataModels.Interfaces
{
    public interface IRequestBody
    {
        string ContentType { get; }
        object Value { get; }

        IRequestBody Duplicate();
        HttpContent ToHttpContent();

        byte[] ToByteArray();
    }
}
