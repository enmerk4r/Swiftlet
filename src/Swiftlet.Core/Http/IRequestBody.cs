using System.Net.Http;

namespace Swiftlet.Core.Http;

public interface IRequestBody
{
    string ContentType { get; }

    object Value { get; }

    IRequestBody Duplicate();

    HttpContent ToHttpContent();

    byte[] ToByteArray();
}
