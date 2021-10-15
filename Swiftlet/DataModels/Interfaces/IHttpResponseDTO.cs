using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Swiftlet.DataModels.Interfaces
{
    public interface IHttpResponseDTO
    {
        string CharacterSet { get; }
        string ContentEncoding { get; }
        long ContentLength { get; }
        string ContentType { get; }
        bool IsFromCache { get; }
        DateTime LastModified { get; }
        string Method { get; }
        string ResponseUri { get; }
        string ResponseServer { get; }
        int StatusCode { get; }
        string StatusDescription { get; }
        bool SupportsHeaders { get; }
        string Content { get; }

        IHttpResponseDTO Duplicate();
    }
}
