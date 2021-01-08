using System.Collections.Generic;
using System.Linq;
using Kanvas.Encoding;
using Komponent.IO.Attributes;
using Kontract.Kanvas;
using Kontract.Models.Image;
using Kontract.Interfaces.Managers;
using Kontract.Models.Dialog;

namespace plugin_shade.Images
{
    class ShtxHeader
    {
        [FixedLength(4)]
        public string Magic;
        public short Format; // 0x4646 (FF), 0x5346(FS), 0x3446(F4)
        public short Width;
        public short Height;
        public byte unk1;
        public byte unk2;
    }

    class ShtxSupport
    {

        public static IDictionary<int, IColorEncoding> EncodingsV1 = new Dictionary<int, IColorEncoding>
        {
            [0x4646] = new Rgba(8, 8, 8, 8)
        };
        public static IDictionary<int, IColorEncoding> EncodingsV2 = new Dictionary<int, IColorEncoding>
        {
            [0x4646] = new Rgba(8, 8, 8, 8)
        };
        public static IDictionary<int, IColorEncoding> PaletteEncodingsV1 = new Dictionary<int, IColorEncoding>
        {
            [0x3446] = new Rgba(8, 8, 8, 8, "ARGB"),
            [0x5346] = new Rgba(8, 8, 8, 8, "ARGB")
        };
        public static IDictionary<int, IColorEncoding> PaletteEncodingsV2 = new Dictionary<int, IColorEncoding>
        {
            [0x3446] = new Rgba(8, 8, 8, 8, "ABGR"),
            [0x5346] = new Rgba(8, 8, 8, 8, "ABGR")
        };


        public static IDictionary<int, IndexEncodingDefinition> IndexEncodings = new Dictionary<int, IndexEncodingDefinition>
        {
            [0x3446] = new IndexEncodingDefinition(new Index(4), new[] { 0x3446 }),
            [0x5346] = new IndexEncodingDefinition(new Index(8), new[] { 0x5346 })
        };

        public static readonly IDictionary<string, IDictionary<int, IColorEncoding>> PlatformEncodingMapping =
            new Dictionary<string, IDictionary<int, IColorEncoding>>
            {
                ["Wii"] = EncodingsV1,
                ["PS Vita"] = EncodingsV2
            };
        public static readonly IDictionary<string, IDictionary<int, IColorEncoding>> PlatformPaletteEncodingMapping =
            new Dictionary<string, IDictionary<int, IColorEncoding>>
            {
                ["Wii"] = PaletteEncodingsV1,
                ["PS Vita"] = PaletteEncodingsV2
            };

        public static EncodingDefinition DetermineFormatMapping(IDialogManager dialogManager)
        {
            // Re-uses some of the code used in the Imgc plugin
            
            // Show a dialog to the user, selecting the platform
            var availablePlatforms = PlatformEncodingMapping.Keys.ToArray();
            var dialogField = new DialogField(DialogFieldType.DropDown, "Select the platform:", availablePlatforms.First(), availablePlatforms);

            dialogManager.ShowDialog(new[] { dialogField });

            var encodingDefinition = PlatformEncodingMapping[dialogField.Result].ToColorDefinition();
            encodingDefinition.AddPaletteEncodings(PlatformPaletteEncodingMapping[dialogField.Result]);

            return encodingDefinition;
        }

    }

}