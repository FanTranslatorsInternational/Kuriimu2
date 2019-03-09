using Komponent.IO.Attributes;
using System;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;

namespace Komponent.IO
{
    public enum ByteOrder : ushort
    {
        LittleEndian = 0xFEFF,
        BigEndian = 0xFFFE
    }

    public enum BitOrder : byte
    {
        Inherit,
        LSBFirst,
        MSBFirst,
        LowestAddressFirst,
        HighestAddressFirst
    }

    internal class MemberAttributeInfo
    {
        MemberInfo _member;

        public MemberAttributeInfo(MemberInfo member)
        {
            _member = member ?? throw new ArgumentNullException(nameof(member));
        }

        public EndiannessAttribute EndiannessAttribute => _member.GetCustomAttribute<EndiannessAttribute>();
        public FixedLengthAttribute FixedLengthAttribute => _member.GetCustomAttribute<FixedLengthAttribute>();
        public VariableLengthAttribute VariableLengthAttribute => _member.GetCustomAttribute<VariableLengthAttribute>();
        public BitFieldInfoAttribute BitFieldInfoAttribute => _member.GetCustomAttribute<BitFieldInfoAttribute>();
        public AlignmentAttribute AlignmentAttribute => _member.GetCustomAttribute<AlignmentAttribute>();
    }
}