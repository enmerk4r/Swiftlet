using Swiftlet.DataModels.Implementations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Swiftlet.DataModels.Interfaces
{
    public interface IHttpResponseDTO
    {
        string Version { get; }
        int StatusCode { get; }
        string ReasonPhrase { get; }
        List<HttpHeader> Headers { get; }
        bool IsSuccessStatusCode { get; }
        string Content { get; }

        IHttpResponseDTO Duplicate();
    }
}
