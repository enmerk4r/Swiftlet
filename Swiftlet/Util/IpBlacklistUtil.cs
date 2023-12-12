using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;

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
                if (address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork && BlackListedIpBlocks[i].IsIPv4 && BlackListedIpBlocks[i].Match(address.GetAddressBytes()))
                {
                    return true;
                }
                else if (address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6 && !BlackListedIpBlocks[i].IsIPv4 && BlackListedIpBlocks[i].Match(address.GetAddressBytes()))
                {
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
        /// Loads blacklist of IPv4 and IPv6 CIDR blocks from environment variable "BLOCKED_SUBNETS". 
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
                    throw new ArgumentException("Expected an IPv4 CIDR block like X.Y.W.Z/B or an IPv6 CIDR block like {IPv6 address}/B.");
                }

                if (!byte.TryParse(cidrBlockParts[1], out byte numBits))
                {
                    throw new ArgumentException("Expected an IPv4 CIDR block like X.Y.W.Z/B or an IPv6 CIDR block like {IPv6 address}/B.");
                }

                bool isIPv4 = cidrBlockParts[0].Contains('.');

                if (isIPv4)
                {
                    if (numBits < 0 || numBits > 32)
                    {
                        throw new ArgumentException("B must be a number between 0 and 32 for IPv4 CIDR blocks like X.Y.W.Z/B.");
                    }

                    var addressParts = cidrBlockParts[0].Split('.');
                    if (addressParts.Length != 4)
                    {
                        throw new ArgumentException("Malformed IPv4 CIDR block.");
                    }

                    byte numBitsShifted = numBits;

                    List<ByteMask> byteMasks = new List<ByteMask>(4);
                    for (int i = 0; i < 4; i++)
                    {
                        if (!byte.TryParse(addressParts[i], out byte addressByte))
                        {
                            throw new ArgumentException("X, Y, W, Z must be numbers between 0 and 255 for IPv4 CIDR blocks like X.Y.W.Z/B.");
                        }

                        byte shift = numBitsShifted >= 8 ? (byte)0 : (byte)(8 - numBitsShifted);
                        numBitsShifted = (byte)Math.Max(0, numBitsShifted - 8);

                        if ((255 & (addressByte << (8 - shift))) != 0)
                        {
                            throw new ArgumentException($"Not a valid IPv4 CIDR block, netmask does not match address (all bits from bit {numBits} must be 0).");
                        }

                        byteMasks.Add(new ByteMask(addressByte, shift));
                    }

                    ByteMasks = byteMasks.ToArray();
                }
                else
                {
                    if (numBits < 0 || numBits > 128)
                    {
                        throw new ArgumentException("B must be a number between 0 and 128 for IPv6 CIDR blocks like {IPv6 address}/B.");
                    }

                    var addressParts = cidrBlockParts[0].Split(':');
                    if (addressParts.Length > 8)
                    {
                        throw new ArgumentException("Malformed IPv6 CIDR block.");
                    }
                    else if (addressParts.Length < 8)
                    {
                        if (addressParts.Where(p => p.Length == 0).Count() != 1)
                        {
                            throw new ArgumentException("Malformed IPv6 CIDR block.");
                        }
                        var completeAddressParts = new List<string>(8);
                        int fill = -1;
                        for (int i = 0; i < 8; i++)
                        {
                            if (fill < 0 && addressParts[i].Length == 0)
                            {
                                fill = 8 - addressParts.Length + 1;
                            }

                            if (fill < 0)
                            {
                                completeAddressParts.Add(addressParts[i]);
                            }
                            else if (fill == 0)
                            {
                                completeAddressParts.Add(addressParts[i - 8 + addressParts.Length]);
                            }
                            else if (fill > 0)
                            {
                                completeAddressParts.Add("0");
                                fill--;
                            }
                        }
                        addressParts = completeAddressParts.ToArray();
                    }

                    byte numBitsShifted = numBits;

                    List<ByteMask> byteMasks = new List<ByteMask>(8);
                    for (int i = 0; i < 8; i++)
                    {
                        if (!ushort.TryParse(addressParts[i], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out ushort addressShort))
                        {
                            throw new ArgumentException("Hex numbers must be between 0 and 65535 for IPv6 CIDR blocks.");
                        }

                        byte addressByte = (byte)(addressShort >> 8);
                        for (int j = 0; j < 2; j++)
                        {
                            byte shift = numBitsShifted >= 8 ? (byte)0 : (byte)(8 - numBitsShifted);
                            numBitsShifted = (byte)Math.Max(0, numBitsShifted - 8);

                            if ((255 & (addressByte << (8 - shift))) != 0)
                            {
                                throw new ArgumentException($"Not a valid IPv6 CIDR block, netmask does not match address (all bits from bit {numBits} must be 0).");
                            }

                            byteMasks.Add(new ByteMask(addressByte, shift));

                            addressByte = (byte)addressShort;
                        }
                    }

                    ByteMasks = byteMasks.ToArray();
                }
            }

            public ByteMask[] ByteMasks { get; private set; }

            public bool IsIPv4 => ByteMasks.Length == 4;

            public bool Match(byte[] addressBytes)
            {
                if (IsIPv4 && (addressBytes.Length != 4 || addressBytes.Length != ByteMasks.Length))
                {
                    throw new ArgumentException("Expected a byte representation of an IPv4 address.");
                }
                else if (!IsIPv4 && (addressBytes.Length != 16 || addressBytes.Length != ByteMasks.Length))
                {
                    throw new ArgumentException("Expected a byte representation of an IPv6 address.");
                }

                for (int i = 0, imax = ByteMasks.Length; i < imax; i++)
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
