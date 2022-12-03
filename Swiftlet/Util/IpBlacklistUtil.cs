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

            for (int i = 0; i < BlackListedIpBlocks.Length; i++)
            {
                if (BlackListedIpBlocks[i].Match(address.GetAddressBytes())) {
                    return true;
                }
            }
          
            return false;
        }

        public bool IsIpHostBlacklisted(IPHostEntry hostEntry)
        {
            if (hostEntry.AddressList.Where(addr => IsIpAddressBlacklisted(addr)).Any())
                return true;
            return false;
        }

        /// <summary>
        /// Loads blacklist of CIDR blocks from environment variable "BLOCKED_SUBNETS". 
        /// Example blacklist: "192.168.0.0/24;192.168.128.0/17"
        /// Example invalid blacklist (last item fails): "192.168.0.0/24;192.168.128.0/17;192.168.128.0/16"
        /// </summary>
        public void LoadBlacklist()
        {
            List<IpBlock> ipBlocks = new List<IpBlock>();

            var cidrBlocksToBlock = Environment.GetEnvironmentVariable("BLOCKED_SUBNETS");
            if (!String.IsNullOrEmpty(cidrBlocksToBlock))
            {
                var cidrBlocks = cidrBlocksToBlock.Split(';');
                try
                {
                    for (int i = 0; i < cidrBlocks.Length; i++)
                    {
                        ipBlocks.Add(new IpBlock(cidrBlocks[i]));
                    }
                }
                catch
                {
                    // a failure in parsing is suspicious, block everything
                    ipBlocks.Add(new IpBlock("0.0.0.0/0"));
                }
            }

            BlackListedIpBlocks = ipBlocks.ToArray();
        }

        IpBlock[] BlackListedIpBlocks { get; set; }

        struct ByteMask
        {
            public ByteMask(byte value, byte shift)
            {
                Value = value;
                Shift = shift;
            }

            public byte Value { get; private set; }
            public byte Shift { get; private set; }

            public bool Match(byte b)
            {
                return (b >> Shift) == (Value >> Shift);
            }
        }

        class IpBlock
        {
            public IpBlock(string cidrBlock)
            {
                var cidrBlockParts = cidrBlock.Split('/');
                if (cidrBlockParts.Length != 2)
                {
                    throw new ArgumentException("Expected a CIDR block like X.Y.W.Z/B.");
                }

                if (!byte.TryParse(cidrBlockParts[1], out byte numBits))
                {
                    throw new ArgumentException("Expected a CIDR block like X.Y.W.Z/B where B is a number between 0 and 32.");
                }

                if (numBits < 0 || numBits > 32)
                {
                    throw new ArgumentException("Expected a CIDR block like X.Y.W.Z/B where B is a number between 0 and 32.");
                }

                var addressParts = cidrBlockParts[0].Split('.');
                if (addressParts.Length != 4)
                {
                    throw new ArgumentException("Expected a CIDR block like X.Y.W.Z/B.");
                }

                byte numBitsShifted = numBits;

                List<ByteMask> byteMasks = new List<ByteMask>(4);
                for (int i = 0; i < 4; i++)
                {
                    if (!byte.TryParse(addressParts[i], out byte addressByte))
                    {
                        throw new ArgumentException("Expected a CIDR block like X.Y.W.Z/B where X, Y, W, Z are numbers between 0 and 255.");
                    }

                    byte shift = numBitsShifted >= 8 ? (byte)0 : (byte)(8 - numBitsShifted);
                    numBitsShifted = (byte)Math.Max(0, numBitsShifted - 8);

                    if ((255 & (addressByte << (8 - shift))) != 0)
                    {
                        throw new ArgumentException($"Not a valid CIDR block, netmask does not match address (all bits from bit {numBits} must be 0).");
                    }

                    byteMasks.Add(new ByteMask(addressByte, shift));
                }

                ByteMasks = byteMasks.ToArray();
            }

            public ByteMask[] ByteMasks { get; private set; }

            public bool Match(byte[] addressBytes)
            {
                if (addressBytes.Length != 4)
                {
                    throw new ArgumentException("Expected a byte representation of an IPV4 address.");
                }

                for (int i = 0; i < 4; i++)
                {
                    if (!ByteMasks[i].Match(addressBytes[i]))
                    {
                        return false;
                    }
                }

                return true;
            }
        }


    }
}
