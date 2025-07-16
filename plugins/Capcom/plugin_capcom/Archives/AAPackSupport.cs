using Konnect.Contract.DataClasses.Management.Dialog;
using Konnect.Contract.DataClasses.Plugin.File.Archive;
using Konnect.Contract.Enums.Management.Dialog;
using Konnect.Contract.Management.Dialog;
using Konnect.Plugin.File.Archive;

namespace plugin_capcom.Archives
{
    class AAPackFileEntry
    {
        public uint offset;
        public uint flags;
        public uint uncompSize;
        public uint compSize;
        public uint hash;
    }

    class AAPackArchiveFile : ArchiveFile
    {
        public AAPackFileEntry Entry { get; }

        public AAPackArchiveFile(ArchiveFileInfo fileInfo, AAPackFileEntry entry) : base(fileInfo)
        {
            Entry = entry;
        }
    }

    partial class AAPackSupport
    {
        public static async Task<string> GetVersion(IDialogManager dialogManager)
        {
            var dialogField = new DialogField
            {
                Text = "Game Version:",
                Type = DialogFieldType.DropDown,
                DefaultValue = "None",
                Options = ["None", "Ace Attorney Trilogy", "Apollo Justice"]
            };
            await dialogManager.ShowDialog([dialogField]);

            return dialogField.Result;
        }

        public static IDictionary<uint, string> GetMapping(string version)
        {
            switch (version)
            {
                case "Ace Attorney Trilogy":
                    return AaTriMapping;

                case "Apollo Justice":
                    return AjMapping;
            }

            return new Dictionary<uint, string>();
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
