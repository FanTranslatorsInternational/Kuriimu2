using Kanvas.Contract.Encoding;
using Kanvas.Encoding;
using Konnect.Contract.DataClasses.Management.Dialog;
using Konnect.Contract.DataClasses.Plugin.File.Image;
using Konnect.Contract.Enums.Management.Dialog;
using Konnect.Contract.Management.Dialog;
using Konnect.Plugin.File.Image;
using Index = Kanvas.Encoding.Index;

namespace plugin_shade.Images
{
    class ShtxHeader
    {
        public string Magic;
        public short Format; // 0x4646 (FF), 0x5346(FS), 0x3446(F4)
        public short Width;
        public short Height;
        public byte LogW;
        public byte LogH;
    }

    class ShtxSupport
    {

        public static IDictionary<int, IColorEncoding> EncodingsV1 = new Dictionary<int, IColorEncoding>
        {
            [0x4646] = new Rgba(8, 8, 8, 8, "ARGB")
        };
        public static IDictionary<int, IColorEncoding> PaletteEncodingsV1 = new Dictionary<int, IColorEncoding>
        {
            [0] = new Rgba(8, 8, 8, 8, "ARGB")
        };
        public static IDictionary<int, IColorEncoding> PaletteEncodingsV2 = new Dictionary<int, IColorEncoding>
        {
            [0] = new Rgba(8, 8, 8, 8, "ABGR")
        };

        public static IDictionary<int, IndexEncodingDefinition> IndexEncodings = new Dictionary<int, IndexEncodingDefinition>
        {
            [0x3446] = new() { IndexEncoding = new Index(4), PaletteEncodingIndices = [0] },
            [0x5346] = new() { IndexEncoding = new Index(8), PaletteEncodingIndices = [0] }
        };

        public static readonly IDictionary<string, IDictionary<int, IColorEncoding>> PlatformPaletteEncodingMapping =
            new Dictionary<string, IDictionary<int, IColorEncoding>>
            {
                ["Wii"] = PaletteEncodingsV1,
                ["PS Vita"] = PaletteEncodingsV2
            };

        public static async Task<EncodingDefinition> DetermineFormatMapping(IDialogManager dialogManager)
        {
            // Re-uses some of the code used in the Imgc plugin

            // Show a dialog to the user, selecting the platform
            var availablePlatforms = PlatformPaletteEncodingMapping.Keys.ToArray();
            var dialogField = new DialogField
            {
                Text = "Select the platform:",
                Type = DialogFieldType.DropDown,
                DefaultValue = availablePlatforms.First(),
                Options = availablePlatforms
            };

            await dialogManager.ShowDialog([dialogField]);

            var encodingDefinition = new EncodingDefinition();
            encodingDefinition.AddColorEncodings(EncodingsV1);
            encodingDefinition.AddPaletteEncodings(PlatformPaletteEncodingMapping[dialogField.Result]);
            encodingDefinition.AddIndexEncodings(IndexEncodings);

            return encodingDefinition;
        }

    }

}
