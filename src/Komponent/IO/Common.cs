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

    //public enum EffectiveBitOrder : byte
    //{
    //    LSBFirst,
    //    MSBFirst
    //}
}