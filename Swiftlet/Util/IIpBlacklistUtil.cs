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
    public interface IIpBlacklistUtil
    {
        bool IsIpHostBlacklisted(IPHostEntry hostEntry);

        bool IsIpAddressBlacklisted(IPAddress address);

        void LoadBlacklist();
    }
}
