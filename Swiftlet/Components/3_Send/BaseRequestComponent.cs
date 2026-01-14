using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows.Forms;
using GH_IO.Serialization;
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
        /// Available timeout options in seconds.
        /// </summary>
        protected static readonly int[] TimeoutOptions = { 1, 5, 10, 15, 30, 60, 100, 300, 600, 900 };

        /// <summary>
        /// Current timeout setting in seconds.
        /// </summary>
        protected int TimeoutSeconds { get; set; } = HttpClientFactory.DefaultTimeoutSeconds;

        /// <summary>
        /// Initializes a new instance of the BaseRequestComponent class.
        /// </summary>
        public BaseRequestComponent(string name, string nickname, string description, string category, string subCategory)
            : base(name, nickname, description, category, subCategory)
        {
            IpBlacklistUtil = new IpBlacklistUtil();
            IpBlacklistUtil.LoadBlacklist();
        }

        /// <summary>
        /// IP blacklist utils.
        /// </summary>
        IIpBlacklistUtil IpBlacklistUtil;

        #region Serialization

        public override bool Read(GH_IReader reader)
        {
            if (reader.ItemExists("TimeoutSeconds"))
            {
                TimeoutSeconds = reader.GetInt32("TimeoutSeconds");
            }
            return base.Read(reader);
        }

        public override bool Write(GH_IWriter writer)
        {
            writer.SetInt32("TimeoutSeconds", TimeoutSeconds);
            return base.Write(writer);
        }

        #endregion

        #region Context Menu

        public override void AppendAdditionalMenuItems(ToolStripDropDown menu)
        {
            base.AppendAdditionalMenuItems(menu);

            ToolStripMenuItem timeoutMenu = new ToolStripMenuItem("Timeout");

            foreach (int timeout in TimeoutOptions)
            {
                string label = timeout >= 60
                    ? $"{timeout / 60} min" + (timeout % 60 > 0 ? $" {timeout % 60} s" : "")
                    : $"{timeout} s";

                if (timeout == HttpClientFactory.DefaultTimeoutSeconds)
                {
                    label += " (default)";
                }

                ToolStripMenuItem item = new ToolStripMenuItem(label, null, OnTimeoutClick);
                item.Tag = timeout;
                item.Checked = (TimeoutSeconds == timeout);
                timeoutMenu.DropDownItems.Add(item);
            }

            menu.Items.Add(timeoutMenu);
        }

        private void OnTimeoutClick(object sender, EventArgs e)
        {
            ToolStripMenuItem item = sender as ToolStripMenuItem;
            if (item != null && item.Tag is int timeout)
            {
                TimeoutSeconds = timeout;
                ExpireSolution(true);
            }
        }

        #endregion

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
                return InvalidUrlReturnValue(" Invalid URL.", throwOnInvalid);
            }

            

            if (!url.StartsWith("http://") && !url.StartsWith("https://"))
            {
                return InvalidUrlReturnValue(" URL must include a scheme (http:// or https://)", throwOnInvalid);
            }

            Uri uri = new Uri(url);

            if (!Uri.IsWellFormedUriString(url, UriKind.Absolute))
            {
                return InvalidUrlReturnValue(" URL is not well formed.", throwOnInvalid);
            }

            
            if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
            {
                return InvalidUrlReturnValue(" Please make sure your URL starts with 'http' or 'https'.", throwOnInvalid);
            }

            if (!String.IsNullOrEmpty(uri.Query))
            {
                return InvalidUrlReturnValue(" Please do not include query parameters in your URL. Use the Params (P) input instead.", throwOnInvalid);
            }

            if (!String.IsNullOrEmpty(uri.Fragment))
            {
                return InvalidUrlReturnValue(" Please do not include a fragment in your URL.", throwOnInvalid);
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
                    return InvalidUrlReturnValue(" Please use a valid hostname or IP address.", throwOnInvalid);
                }
                if (IpBlacklistUtil.IsIpHostBlacklisted(hostEntry))
                {
                    return InvalidUrlReturnValue(" The given hostname or IP address is blacklisted.", throwOnInvalid);
                }
            }
            else if (uri.HostNameType == UriHostNameType.IPv6 || uri.HostNameType == UriHostNameType.IPv4)
            {
                if ( !IPAddress.TryParse(uri.Host, out IPAddress ipAddress) )
                {
                    return InvalidUrlReturnValue(" Please use a valid hostname or IP address.", throwOnInvalid);
                }
                if (IpBlacklistUtil.IsIpAddressBlacklisted(ipAddress))
                {
                    return InvalidUrlReturnValue(" The given hostname or IP address is blacklisted.", throwOnInvalid);
                }
            }
            else
            {
                return InvalidUrlReturnValue(" The given hostname or IP address is invalid.", throwOnInvalid);
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