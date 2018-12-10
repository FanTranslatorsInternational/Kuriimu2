using System;
using System.Diagnostics;
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

    public enum EffectiveBitOrder : byte
    {
        LSBFirst,
        MSBFirst
    }

    [DebuggerDisplay("{(string)this}")]
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct Magic
    {
        private int value;
        public static implicit operator string(Magic magic) => Encoding.ASCII.GetString(BitConverter.GetBytes(magic.value));
        public static implicit operator Magic(string s) => new Magic { value = BitConverter.ToInt32(Encoding.ASCII.GetBytes(s), 0) };
    }

    [DebuggerDisplay("{(string)this}")]
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct Magic8
    {
        private long value;
        public static implicit operator string(Magic8 magic) => Encoding.ASCII.GetString(BitConverter.GetBytes(magic.value));
        public static implicit operator Magic8(string s) => new Magic8 { value = BitConverter.ToInt64(Encoding.ASCII.GetBytes(s), 0) };
    }
}