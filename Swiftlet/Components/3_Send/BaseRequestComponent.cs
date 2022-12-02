using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Grasshopper.Kernel;
using Rhino.Geometry;
using Swiftlet.DataModels.Implementations;
using Swiftlet.Goo;
using Swiftlet.Params;
using Swiftlet.Util;

namespace Swiftlet.Components
{
    /// <summary>
    /// A common base for all components making HTTP requests.
    /// </summary>
    public abstract class BaseRequestComponent : GH_TaskCapableComponent<HttpRequestSolveResults>
    {
        /// <summary>
        /// Initializes a new instance of the BaseRequestComponent class.
        /// </summary>
        public BaseRequestComponent(string name, string nickname, string description, string category, string subCategory)
            : base(name, nickname, description, category, subCategory)
        {
            IpBlacklistUtil = new IpBlacklistUtil();
        }

        /// <summary>
        /// IP blacklist utils.
        /// </summary>
        IIpBlacklistUtil IpBlacklistUtil;

        /// <summary>
        /// Validates the given url according to the following criteria: 
        ///   * valid url in general
        ///   * https or http scheme
        ///   * no query string (query parameters are added separately)
        ///   * no fragment
        ///   * IP address not contained in blacklist
        /// </summary>
        /// <param name="url">The url to validate.</param>
        /// <param name="throwOnInvalid">Whether to throw given an invalid url.</param>
        /// <returns></returns>
        protected bool ValidateUrl(string url, bool throwOnInvalid = true)
        {
            if (String.IsNullOrEmpty(url))
            {
                return InvalidUrlReturnValue("Invalid URL.", throwOnInvalid);
            }

            if (!Uri.IsWellFormedUriString(url, UriKind.Absolute))
            {
                return InvalidUrlReturnValue("URL is not well formed.", throwOnInvalid);
            }

            Uri uri = new Uri(url);
            if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
            {
                return InvalidUrlReturnValue("Please make sure your URL starts with 'http' or 'https'.", throwOnInvalid);
            }

            if (!String.IsNullOrEmpty(uri.Query))
            {
                return InvalidUrlReturnValue("Please do not include query parameters in your URL.", throwOnInvalid);
            }

            if (!String.IsNullOrEmpty(uri.Fragment))
            {
                return InvalidUrlReturnValue("Please do not include a fragment in your URL.", throwOnInvalid);
            }

            if (uri.HostNameType == UriHostNameType.Dns)
            {
                IPHostEntry hostEntry = null;
                try
                {
                    hostEntry = Dns.GetHostEntry(uri.Host);
                }
                catch (System.Net.Sockets.SocketException)
                {
                    return InvalidUrlReturnValue("Please use a valid hostname or IP address.", throwOnInvalid);
                }
                if (IpBlacklistUtil.IsIpHostBlacklisted(hostEntry))
                {
                    return InvalidUrlReturnValue("The given hostname or IP address is blacklisted.", throwOnInvalid);
                }
            }
            else if (uri.HostNameType == UriHostNameType.IPv6 || uri.HostNameType == UriHostNameType.IPv4)
            {
                if ( !IPAddress.TryParse(uri.Host, out IPAddress ipAddress) )
                {
                    return InvalidUrlReturnValue("Please use a valid hostname or IP address.", throwOnInvalid);
                }
                if (IpBlacklistUtil.IsIpAddressBlacklisted(ipAddress))
                {
                    return InvalidUrlReturnValue("The given hostname or IP address is blacklisted.", throwOnInvalid);
                }
            }
            else
            {
                return InvalidUrlReturnValue("The given hostname or IP address is invalid.", throwOnInvalid);
            }

            return true;
        }

        /// <summary>
        /// Make return value for <see cref="ValidateUrl(string, bool)"/>
        /// </summary>
        /// <param name="message"></param>
        /// <param name="throwOnInvalid"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        private bool InvalidUrlReturnValue(string message, bool throwOnInvalid)
        {
            if (throwOnInvalid)
            {
                throw new Exception(message);
            }
            return false;
        }

    }
}