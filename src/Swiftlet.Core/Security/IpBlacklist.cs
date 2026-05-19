using System.Globalization;

namespace Swiftlet.Core.Security;

public sealed class IpBlacklist : IIpBlacklist
{
    private readonly IpBlock[] _blockedIpBlocks;

    public IpBlacklist(IEnumerable<string>? cidrBlocks)
    {
        _blockedIpBlocks = ParseBlocks(cidrBlocks).ToArray();
    }

    public static IpBlacklist Empty { get; } = new(Array.Empty<string>());

    public static IpBlacklist FromEnvironment(string variableName = "BLOCKED_SUBNETS")
    {
        string? rawValue = Environment.GetEnvironmentVariable(variableName);
        return FromSemicolonSeparatedCidrs(rawValue);
    }

    public static IpBlacklist FromSemicolonSeparatedCidrs(string? rawValue)
    {
        if (string.IsNullOrWhiteSpace(rawValue))
        {
            return Empty;
        }

        string[] cidrBlocks = rawValue
            .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        try
        {
            return new IpBlacklist(cidrBlocks);
        }
        catch (ArgumentException)
        {
            // A parsing failure is treated as suspicious configuration: block everything.
            return new IpBlacklist(["0.0.0.0/0"]);
        }
    }

    public bool IsAddressBlacklisted(IPAddress address)
    {
        ArgumentNullException.ThrowIfNull(address);

        if (address.IsIPv4MappedToIPv6)
        {
            address = address.MapToIPv4();
        }

        byte[] addressBytes = address.GetAddressBytes();
        bool isIpv4 = address.AddressFamily == AddressFamily.InterNetwork;
        bool isIpv6 = address.AddressFamily == AddressFamily.InterNetworkV6;

        foreach (IpBlock block in _blockedIpBlocks)
        {
            if ((isIpv4 && block.IsIpv4) || (isIpv6 && !block.IsIpv4))
            {
                if (block.Match(addressBytes))
                {
                    return true;
                }
            }
        }

        return false;
    }

    public bool IsHostBlacklisted(IPHostEntry hostEntry)
    {
        ArgumentNullException.ThrowIfNull(hostEntry);
        return hostEntry.AddressList.Any(IsAddressBlacklisted);
    }

    private static IEnumerable<IpBlock> ParseBlocks(IEnumerable<string>? cidrBlocks)
    {
        if (cidrBlocks is null)
        {
            yield break;
        }

        foreach (string block in cidrBlocks)
        {
            if (string.IsNullOrWhiteSpace(block))
            {
                continue;
            }

            yield return new IpBlock(block);
        }
    }

    private readonly struct ByteMask
    {
        public ByteMask(byte value, byte shift)
        {
            Value = value;
            Shift = shift;
        }

        public byte Value { get; }

        public byte Shift { get; }

        public bool Match(byte value)
        {
            return (value >> Shift) == (Value >> Shift);
        }
    }

    private sealed class IpBlock
    {
        public IpBlock(string cidrBlock)
        {
            string[] cidrBlockParts = cidrBlock.Split('/');
            if (cidrBlockParts.Length != 2 || !byte.TryParse(cidrBlockParts[1], out byte numBits))
            {
                throw new ArgumentException("Expected an IPv4 CIDR block like X.Y.W.Z/B or an IPv6 CIDR block like {IPv6 address}/B.");
            }

            if (cidrBlockParts[0].Contains('.', StringComparison.Ordinal))
            {
                IsIpv4 = true;
                ByteMasks = BuildIpv4Masks(cidrBlockParts[0], numBits);
            }
            else
            {
                IsIpv4 = false;
                ByteMasks = BuildIpv6Masks(cidrBlockParts[0], numBits);
            }
        }

        public bool IsIpv4 { get; }

        public ByteMask[] ByteMasks { get; }

        public bool Match(byte[] addressBytes)
        {
            if (IsIpv4 && addressBytes.Length != 4)
            {
                throw new ArgumentException("Expected a byte representation of an IPv4 address.");
            }

            if (!IsIpv4 && addressBytes.Length != 16)
            {
                throw new ArgumentException("Expected a byte representation of an IPv6 address.");
            }

            for (int i = 0; i < ByteMasks.Length; i++)
            {
                if (!ByteMasks[i].Match(addressBytes[i]))
                {
                    return false;
                }
            }

            return true;
        }

        private static ByteMask[] BuildIpv4Masks(string address, byte numBits)
        {
            if (numBits > 32)
            {
                throw new ArgumentException("B must be a number between 0 and 32 for IPv4 CIDR blocks like X.Y.W.Z/B.");
            }

            string[] addressParts = address.Split('.');
            if (addressParts.Length != 4)
            {
                throw new ArgumentException("Malformed IPv4 CIDR block.");
            }

            byte bitsRemaining = numBits;
            List<ByteMask> byteMasks = new(4);

            foreach (string addressPart in addressParts)
            {
                if (!byte.TryParse(addressPart, out byte addressByte))
                {
                    throw new ArgumentException("X, Y, W, Z must be numbers between 0 and 255 for IPv4 CIDR blocks like X.Y.W.Z/B.");
                }

                byte shift = bitsRemaining >= 8 ? (byte)0 : (byte)(8 - bitsRemaining);
                bitsRemaining = (byte)Math.Max(0, bitsRemaining - 8);

                if ((255 & (addressByte << (8 - shift))) != 0)
                {
                    throw new ArgumentException($"Not a valid IPv4 CIDR block, netmask does not match address (all bits from bit {numBits} must be 0).");
                }

                byteMasks.Add(new ByteMask(addressByte, shift));
            }

            return byteMasks.ToArray();
        }

        private static ByteMask[] BuildIpv6Masks(string address, byte numBits)
        {
            if (numBits > 128)
            {
                throw new ArgumentException("B must be a number between 0 and 128 for IPv6 CIDR blocks like {IPv6 address}/B.");
            }

            string[] addressParts = ExpandIpv6Address(address);
            byte bitsRemaining = numBits;
            List<ByteMask> byteMasks = new(16);

            foreach (string addressPart in addressParts)
            {
                if (!ushort.TryParse(addressPart, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out ushort addressShort))
                {
                    throw new ArgumentException("Hex numbers must be between 0 and 65535 for IPv6 CIDR blocks.");
                }

                byte highByte = (byte)(addressShort >> 8);
                byte lowByte = (byte)addressShort;

                byteMasks.Add(CreateByteMask(highByte, numBits, ref bitsRemaining, "IPv6"));
                byteMasks.Add(CreateByteMask(lowByte, numBits, ref bitsRemaining, "IPv6"));
            }

            return byteMasks.ToArray();
        }

        private static string[] ExpandIpv6Address(string address)
        {
            string normalizedAddress = address.EndsWith("::", StringComparison.Ordinal) ? address + "0" : address;
            string[] addressParts = normalizedAddress.Split(':');

            if (addressParts.Length > 8)
            {
                throw new ArgumentException("Malformed IPv6 CIDR block.");
            }

            if (addressParts.Length == 8)
            {
                return addressParts;
            }

            if (addressParts.Count(part => part.Length == 0) != 1)
            {
                throw new ArgumentException("Malformed IPv6 CIDR block.");
            }

            List<string> completed = new(8);
            int fill = -1;

            for (int i = 0; i < 8; i++)
            {
                if (fill < 0 && addressParts[i].Length == 0)
                {
                    fill = 8 - addressParts.Length + 1;
                }

                if (fill < 0)
                {
                    completed.Add(addressParts[i]);
                }
                else if (fill == 0)
                {
                    completed.Add(addressParts[i - 8 + addressParts.Length]);
                }
                else
                {
                    completed.Add("0");
                    fill--;
                }
            }

            return completed.ToArray();
        }

        private static ByteMask CreateByteMask(byte addressByte, byte numBits, ref byte bitsRemaining, string family)
        {
            byte shift = bitsRemaining >= 8 ? (byte)0 : (byte)(8 - bitsRemaining);
            bitsRemaining = (byte)Math.Max(0, bitsRemaining - 8);

            if ((255 & (addressByte << (8 - shift))) != 0)
            {
                throw new ArgumentException($"Not a valid {family} CIDR block, netmask does not match address (all bits from bit {numBits} must be 0).");
            }

            return new ByteMask(addressByte, shift);
        }
    }
}
