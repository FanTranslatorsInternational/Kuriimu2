using Komponent.IO;
using Komponent.Contract.Enums;
using Komponent.Streams;
using Kryptography.Encryption;
using System.Reflection.PortableExecutable;
using System.Runtime.InteropServices;

namespace plugin_mt_framework.Texts
{
    class GmdHeader
    {
        public string magic;
        public int version;
        public GmdLanguage language;
        public long unk1;
        public int labelCount;
        public int sectionCount;
        public int labelSize;
        public int sectionSize;
        public int nameSize;
    }

    class Gmdv1LabelEntry
    {
        public int sectionId;
        public int labelOffset;
    }

    class Gmdv2LabelEntry
    {
        public int sectionId;
        public uint hash1;
        public uint hash2;
        public int labelOffset;
        public int listLink;
    }

    class Gmdv2MobileLabelEntry
    {
        public int sectionId;
        public uint hash1;
        public uint hash2;
        public uint zeroPadding = 0;
        public long labelOffset;
        public long listLink;
    }

    enum GmdVersion
    {
        v1,
        v2
    }

    enum GmdLanguage
    {
        Japanese,
        English,
        French,
        Spanish,
        German,
        Italian
    }

    class GmdSupport
    {
        public static bool TryGetVersion(Stream stream, out GmdVersion version)
        {
            version = default;

            using var br = new BinaryReaderX(stream);

            string magic = br.ReadString(4);
            bool isGmd = magic is "GMD\0" or "\0DMG";
            if (!isGmd)
            {
                br.BaseStream.Position -= 4;
                return false;
            }

            br.ByteOrder = magic is "\0DMG" ? ByteOrder.BigEndian : ByteOrder.LittleEndian;

            uint versionNumber = br.ReadUInt32();
            br.BaseStream.Position -= 8;

            switch (versionNumber)
            {
                case 0x00010201:
                    version = GmdVersion.v1;
                    return true;

                case 0x00010302:
                    version = GmdVersion.v2;
                    return true;
            }

            return false;
        }

        private static string[] _key1 = ["fjfajfahajra;tira9tgujagjjgajgoa", "e43bcc7fcab+a6c4ed22fcd433/9d2e6cb053fa462-463f3a446b19"];
        private static string[] _key2 = ["mva;eignhpe/dfkfjgp295jtugkpejfu", "861f1dca05a0;9ddd5261e5dcc@6b438e6c.8ba7d71c*4fd11f3af1"];

        public static int DetectKeypair(Stream input, long position)
        {
            input.Position = input.Length - 1;
            int lastByte = input.ReadByte();

            input.Position = position;

            if (lastByte is 0)
                return -1;

            for (var i = 0; i < _key1.Length; i++)
            {
                var keyPos = (int)((input.Length - position - 1) % _key1[i].Length);

                if ((lastByte ^ _key1[i][keyPos] ^ _key2[i][keyPos]) is not 0)
                    continue;

                return i;
            }

            return -2;
        }

        public static Stream GetXorStream(Stream input, long position, int keyPair)
        {
            if (keyPair < -1)
                throw new InvalidOperationException("Could not determine key pair.");

            if (keyPair is -1)
                return new SubStream(input, position);

            string key1 = _key1[keyPair];
            string key2 = _key2[keyPair];

            var key = new byte[key1.Length];
            for (var i = 0; i < key.Length; i++)
                key[i] = (byte)(key1[i] ^ key2[i]);

            return new XorStream(new SubStream(input, position), key);
        }

        public static Stream GetXorStream(Stream input, int keyPair)
        {
            if (keyPair < -1)
                throw new InvalidOperationException("Could not determine key pair.");

            if (keyPair is -1)
                return input;

            string key1 = _key1[keyPair];
            string key2 = _key2[keyPair];

            var key = new byte[key1.Length];
            for (var i = 0; i < key.Length; i++)
                key[i] = (byte)(key1[i] ^ key2[i]);

            return new XorStream(input, key);
        }
    }
}
