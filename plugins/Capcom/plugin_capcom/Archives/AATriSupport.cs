using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Komponent.IO;
using Kontract.Interfaces.Managers;
using Kontract.Models.Dialog;

namespace plugin_capcom.Archives
{
    class AATriFileEntry
    {
        public uint offset;
        public uint flags;
        public uint uncompSize;
        public uint compSize;
        public uint hash;
    }

    enum AAVersion
    {
        None,
        AATri,
        ApolloJustice
    }

    partial class AATriSupport
    {
        public static AAVersion GetVersion(IDialogManager dialogManager)
        {
            var dialogField = new DialogField(DialogFieldType.DropDown, "Game Version:", AAVersion.AATri.ToString(),
                Enum.GetNames(typeof(AAVersion)));
            dialogManager.ShowDialog(new[] { dialogField });

            return Enum.Parse<AAVersion>(dialogField.Result);
        }

        public static IDictionary<uint, string> GetMapping(AAVersion version)
        {
            switch (version)
            {
                case AAVersion.AATri:
                    return AaTriMapping;
            }

            return new Dictionary<uint, string>();
        }

        public static string DetermineExtension(Stream input)
        {
            input.Position += 4;
            var magicSamples = CollectMagicSamples(input);
            input.Position -= 4;

            if (magicSamples.Any(x => x.Contains("BCH")))
                return ".bch";

            if (magicSamples.Any(x => x.Contains(".ans")))
                return ".ans";

            if (magicSamples.Any(x => x.Contains("FFNT")))
                return ".bffnt";

            if (magicSamples.Any(x => x.Contains("mcol")))
                return ".mcol";

            return ".bin";
        }

        private static IList<string> CollectMagicSamples(Stream input)
        {
            var bkPos = input.Position;

            using var br = new BinaryReaderX(input, true);

            // Get 3 samples to check magic with compression
            input.Position = bkPos;
            var magic1 = br.ReadString(4);
            input.Position = bkPos + 1;
            var magic2 = br.ReadString(4);
            input.Position = bkPos + 2;
            var magic3 = br.ReadString(4);

            return new[] { magic1, magic2, magic3 };
        }

        public static uint CreateHash(string input)
        {
            var hashResult = 0u;

            input = input.ToUpper();
            for (var position = 0; position < input.Length; position++)
            {
                var seed = GetSeed(position, input.Length);
                hashResult = (uint)(input[position] * seed + hashResult);
            }

            return hashResult;
        }

        private static int GetSeed(int position, int length)
        {
            var leastBit = GetLeastBit(position, length);
            var seed = leastBit == 1 ? 0x1F : 1;

            while (length - position - 1 > leastBit)
            {
                leastBit += 2;
                seed *= 0x3c1;
            }

            return seed;
        }

        private static int GetLeastBit(int position, int length)
        {
            if (position < length - 1)
                return ~(length - position) & 1;

            return 0;
        }
    }
}
