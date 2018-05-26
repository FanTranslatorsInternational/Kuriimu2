using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Komponent.IO
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
    public class BitFieldInfo : Attribute
    {
        public int BlockSize = 32;
        public BitOrder Orientation = BitOrder.MSBFirst;
    }

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
    public class Endianness : Attribute
    {
        public ByteOrder ByteOrder = ByteOrder.LittleEndian;
    }

    [AttributeUsage(AttributeTargets.Field)]
    public class BitField : Attribute
    {
        public int _bitsToRead { get; }
        public BitField(int bitsToRead)
        {
            _bitsToRead = bitsToRead;
        }
    }

    [AttributeUsage(AttributeTargets.Field)]
    public class Length : Attribute
    {
        public int _length { get; }
        public Length(int length)
        {
            _length = length;
        }
    }
}
