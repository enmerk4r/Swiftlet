using Swiftlet.DataModels.Implementations;
using Swiftlet.DataModels.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Swiftlet.Util
{
    public class IpBlacklistUtil : IIpBlacklistUtil
    {
        public bool IsIpAddressBlacklisted(IPAddress address)
        {
            if (address.IsIPv4MappedToIPv6)
            {
                address = address.MapToIPv4();
            }

            // TODO replace this dummy check
            if (address.GetAddressBytes()[0] == 192)
                return true;

            return false;
        }

        public bool IsIpHostBlacklisted(IPHostEntry hostEntry)
        {
            if (hostEntry.AddressList.Where(addr => IsIpAddressBlacklisted(addr)).Any())
                return true;
            return false;
        }

        public void LoadBlacklist()
        {
            // TODO load blacklist
        }
    }
}
