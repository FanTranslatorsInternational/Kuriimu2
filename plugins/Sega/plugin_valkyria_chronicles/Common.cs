using System;
using System.IO;
using Komponent.IO.Attributes;

namespace plugin_valkyria_chronicles
{
    public static class Common
    {
        /// <summary>
        /// The size in bytes of the Packet Header.
        /// </summary>
        public const int PacketHeaderSize = 0x10;

        /// <summary>
        /// The size in bytes of the Extended Packet Header.
        /// </summary>
        public const int PacketHeaderXSize = 0x20;
    }

    /// <summary>
    /// PacketHeader is the standard header in most VC file formats.
    /// </summary>
    public sealed class PacketHeader
    {
        [FixedLength(4)]
        public string Magic;

        /// <summary>
        /// The size of this packet after this header and just before the EOFC including nested packets.
        /// </summary>
        public int PacketSize;
        public int HeaderSize;
        public int Flags;
    }

    /// <summary>
    /// PacketHeaderX is an extended header in many VC file formats.
    /// </summary>
    public sealed class PacketHeaderX
    {
        [FixedLength(4)]
        public string Magic;

        /// <summary>
        /// The size of this packet after this header and just before the EOFC including nested packets.
        /// </summary>
        public int PacketSize;
        public int HeaderSize;
        public byte Flags1;
        public byte Flags2;
        public byte Flags3;
        public byte Flags4;
        public int Unk1;

        /// <summary>
        /// The size of the data in the packet after this header and just before any nested packets.
        /// </summary>
        public int DataSize;
        public int Unk2;
        public int Unk3;
    }

    public static class Extensions
    {
        /// <summary>
        /// Read bytes that are ROTn obfuscated.
        /// </summary>
        /// <param name="br">A BinaryReader.</param>
        /// <param name="count">The number of bytes to read. This value must be 0 or a non-negative number or an exception will occur.</param>
        /// <param name="rot">The number that each byte needs to be rotated by.</param>
        /// <returns></returns>
        public static byte[] ReadROTnBytes(this BinaryReader br, int count, int rot = 1)
        {
            var bytes = br.ReadBytes(count);
            for (var i = 0; i < bytes.Length; i++)
                if (bytes[i] >= rot) bytes[i] -= (byte)rot;
            return bytes;
        }

        /// <summary>
        /// Read an Int32 that is ROTn obfuscated.
        /// </summary>
        /// <param name="br">A BinaryReader.</param>
        /// <param name="rot">The number that each byte needs to be rotated by.</param>
        /// <returns></returns>
        public static int ReadROTnInt32(this BinaryReader br, int rot = 1)
        {
            return BitConverter.ToInt32(br.ReadROTnBytes(4, rot), 0);
        }

        public static void WriteROTnBytes(this BinaryWriter bw, byte[] value, int rot = 1)
        {
            for (var i = 0; i < value.Length; i++)
                if (rot <= 0xFF - value[i])
                    bw.Write(value[i] += (byte)rot);
        }

        public static void WriteROTnInt32(this BinaryWriter bw, int value, int rot = 1)
        {
            bw.WriteROTnBytes(BitConverter.GetBytes(value), rot);
        }
    }
}
