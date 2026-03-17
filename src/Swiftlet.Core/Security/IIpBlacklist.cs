namespace Swiftlet.Core.Security;

public interface IIpBlacklist
{
    bool IsAddressBlacklisted(IPAddress address);

    bool IsHostBlacklisted(IPHostEntry hostEntry);
}
