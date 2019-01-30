using System;
using System.Linq;

namespace plugin_criware.CRILAYLA
{
    /// <summary>
    /// 
    /// </summary>
    public class BitReader
    {
        private uint[] remaining;
        private uint bitsLeftInUInt;
        private uint lastUInt;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="newSource"></param>
        public BitReader(byte[] newSource)
        {
            if ((newSource.Length & 3) == 0)
            {
                // Convert from byte array to uint array? wtf!?
                //remaining = cast(uint[])source;
                throw new NotImplementedException();
            }
            else
            {
                // This does, something, make the array long enough to divide evenly into an array of uints?
                var off = (newSource.Length & 3);
                remaining = new uint[1];
                if (off == 3)
                    remaining[0] = (uint)((newSource[0] << 8) | (newSource[1] << 16) | (newSource[2] << 24));
                else if (off == 2)
                    remaining[0] = (uint)((newSource[0] << 16) | (newSource[1] << 24));
                else
                    remaining[0] = (uint)(newSource[0] << 24);

                // Convert the rest of source from byte array to uint array? wtf!?
                //if (source.Length > off)
                //    remaining = cast(uint[])source;
            }
            bitsLeftInUInt = 32;
            lastUInt = remaining[remaining.Length - 1];
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public bool ReadBit()
        {
            bool result;

            --bitsLeftInUInt;
            if (bitsLeftInUInt == 0)
            {
                result = (lastUInt & 1) != 0;
                NextRemaining();
            }
            else
            {
                uint uintMask = (uint)(1 << (int)bitsLeftInUInt);
                result = (lastUInt & uintMask) != 0;
            }

            return result;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public uint ReadByte()
        {
            uint result;

            var bitsMoreThanByte = (int)bitsLeftInUInt - 8;
            if (bitsMoreThanByte > 0)
            {
                result = (lastUInt >> bitsMoreThanByte) & 0xFF;
                bitsLeftInUInt = (uint)bitsMoreThanByte;
            }
            // this if prevents a crash: if remaining.length == 1, then
            // accessing remaining[$ - 2] is out of bounds
            else if (bitsMoreThanByte == 0)
            {
                result = lastUInt & 0xFF;
                NextRemaining(); // Shortened because I noticed it's the same code
            }
            else
            {
                // This needs review, C# doesn't like bit shifting uints
                var bitsFromSecondUInt = (uint)(8 - (int)bitsLeftInUInt);
                result = (uint)(lastUInt & ((1 << (int)bitsLeftInUInt) - 1));
                var x = (uint)((int)remaining[remaining.Length - 2] >> (int)(24 + bitsLeftInUInt));
                result = (result << (int)bitsFromSecondUInt) | x;

                NextRemaining();
                bitsLeftInUInt -= bitsFromSecondUInt;
            }

            return result;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="numBits"></param>
        /// <returns></returns>
        public uint Read(uint numBits)
        {
            uint result;

            if (numBits < bitsLeftInUInt)
            {
                var mask = (1 << (int)numBits) - 1;

                bitsLeftInUInt -= numBits;
                result = (uint)((lastUInt >> (int)bitsLeftInUInt) & mask);
            }
            else if (numBits == bitsLeftInUInt)
            {
                var mask = (1 << (int)numBits) - 1;
                result = (uint)(lastUInt & mask);

                NextRemaining();
            }
            else
            {
                // This needs review, C# doesn't like bit shifting uints
                var bitsFromSecondUInt = (uint)(numBits - (int)bitsLeftInUInt);
                result = (uint)(lastUInt & ((1 << (int)bitsLeftInUInt) - 1));
                var x = (uint)((int)remaining[remaining.Length - 2] >> (int)(32 - bitsFromSecondUInt));
                result = (result << (int)bitsFromSecondUInt) | x;

                NextRemaining();
                bitsLeftInUInt -= bitsFromSecondUInt;
            }

            return result;
        }

        /// <summary>
        /// 
        /// </summary>
        public void NextRemaining()
        {
            if (remaining.Length > 1)
            {
                // Cut off the last byte?
                //remaining = remaining[0 .. $ -1];
                var newRemaining = new uint[remaining.Length - 1];
                Array.Copy(remaining, newRemaining, remaining.Length - 1);
                remaining = newRemaining;
                
                // Set lastUInt to the last one?
                //lastUInt = remaining[$ -1];
                lastUInt = remaining.Last();

                bitsLeftInUInt = 32;
            }
            else
            {
                // Zero out remaining for reasons. remaining.Length = 0;
                remaining = new uint[0];
                bitsLeftInUInt = 0;
            }
        }
    }
}
