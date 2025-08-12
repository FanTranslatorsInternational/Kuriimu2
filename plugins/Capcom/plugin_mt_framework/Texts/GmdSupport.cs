using System.Text;
using Kaligraphy.Contract.DataClasses.Parsing;
using Kaligraphy.Contract.Parsing;
using Kaligraphy.DataClasses.Parsing;
using Kaligraphy.Parsing;
using Komponent.IO;
using Komponent.Contract.Enums;
using Komponent.Streams;
using Kryptography.Encryption;

namespace plugin_mt_framework.Texts
{
    class Gmdv1Header
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
        public int labelOffset; //relative to LabelDataOffset and after subtracting (_v1Constant + Header.LabelCount * 0x80)
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

        public static Stream GetXorStream(Stream input, long position)
        {
            input.Position = input.Length - 1;
            int lastByte = input.ReadByte();

            if (lastByte is 0)
                return new SubStream(input, position);

            (string? key1, string? key2) = GetXorKeys(lastByte, input.Length - position);
            if (key1 is null || key2 is null)
                throw new InvalidOperationException("Could not determine keys for decryption.");

            var key = new byte[key1.Length];
            for (var i = 0; i < key.Length; i++)
                key[i] = (byte)(key1[i] ^ key2[i]);

            return new XorStream(new SubStream(input, position), key);
        }

        private static (string?, string?) GetXorKeys(int checkValue, long length)
        {
            for (var i = 0; i < _key1.Length; i++)
            {
                var keyPos = (int)((length - 1) % _key1[i].Length);

                if ((checkValue ^ _key1[i][keyPos] ^ _key2[i][keyPos]) is not 0)
                    continue;

                return (_key1[i], _key2[i]);
            }

            return (null, null);
        }
    }
}
