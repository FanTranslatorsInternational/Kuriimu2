using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Kanvas.Encoding;
using Kanvas.Swizzle;
using Komponent.IO.Attributes;
using Kontract.Interfaces.Managers;
using Kontract.Kanvas;
using Kontract.Kanvas.Model;
using Kontract.Models.Dialog;
using Kontract.Models.Image;
#pragma warning disable 649

namespace plugin_level5._3DS.Images
{
    class ImgcHeader
    {
        [FixedLength(8)]
        public string magic; // IMGC
        public short const2; // 30 00
        public byte imageFormat;
        public byte const3; // 01
        public byte imageCount;
        public byte bitDepth;
        public short bytesPerTile;
        public short width;
        public short height;
        public int const4; // 30 00 00 00
        public int const5; // 30 00 01 00
        public int tableDataOffset; // always 0x48
        public int const6; // 03 00 00 00
        public int const7; // 00 00 00 00
        public int const8; // 00 00 00 00
        public int const9; // 00 00 00 00
        public int const10; // 00 00 00 00
        public int tileTableSize;
        public int tileTableSizePadded;
        public int imgDataSize;
        public int const11; // 00 00 00 00
        public int const12; // 00 00 00 00
    }

    // TODO: Implement into CtrSwizzle as a config value at ctor?
    class ImgcSwizzle : IImageSwizzle
    {
        private readonly MasterSwizzle _zOrder;

        public int Width { get; }
        public int Height { get; }

        public ImgcSwizzle(SwizzlePreparationContext context)
        {
            Width = (context.Size.Width + 0x7) & ~0x7;
            Height = (context.Size.Height + 0x7) & ~0x7;

            _zOrder = new MasterSwizzle(Width, Point.Empty, new[] { (0, 1), (1, 0), (0, 2), (2, 0), (0, 4), (4, 0) });
        }

        public Point Transform(Point point)
        {
            return _zOrder.Get(point.Y * Width + point.X);
        }
    }

    class ImgcSupport
    {
        public static EncodingDefinition DetermineFormatMapping(int imgFormat, int bitDepth, IDialogManager dialogManager)
        {
            var encodingDefinitions = new[]
            {
                ImgcFormatsV1.ToColorDefinition(),
                ImgcFormatsV2.ToColorDefinition()
            };

            // If format does not exist in any
            if (encodingDefinitions.All(x => !x.ContainsColorEncoding(imgFormat)))
                return EncodingDefinition.Empty;

            // If the format exists only in one of the mappings
            if (encodingDefinitions.Count(x => x.ContainsColorEncoding(imgFormat)) == 1)
                return encodingDefinitions.First(x => x.ContainsColorEncoding(imgFormat));

            // If format exists in more than one, compare bitDepth
            var viableMappings = encodingDefinitions.Where(x => x.ContainsColorEncoding(imgFormat)).ToArray();

            // If all mappings are the same encoding
            var encodingName = viableMappings[0].GetColorEncoding(imgFormat).FormatName;
            if (viableMappings.All(x => x.GetColorEncoding(imgFormat).FormatName == encodingName))
                return viableMappings[0];

            // If only one mapping matches the given bitDepth
            if (viableMappings.Count(x => x.GetColorEncoding(imgFormat).BitDepth == bitDepth) == 1)
                return viableMappings.First(x => x.GetColorEncoding(imgFormat).BitDepth == bitDepth);

            // Otherwise the heuristic could not determine a definite mapping
            // Show a dialog to the user, selecting the game
            var availableGames = GameMapping.Keys.ToArray();
            var dialogField = new DialogField(DialogFieldType.DropDown, "Select the game:", availableGames.First(), availableGames);

            dialogManager.ShowDialog(new[] { dialogField });
            return GameMapping[dialogField.Result].ToColorDefinition();
        }

        // This mapping was determined through Inazuma Eleven GO Big Bang
        public static IDictionary<int, IColorEncoding> ImgcFormatsV1 = new Dictionary<int, IColorEncoding>
        {
            [0x00] = new Rgba(8, 8, 8, 8),
            [0x01] = new Rgba(4, 4, 4, 4),
            [0x02] = new Rgba(5, 5, 5, 1),
            [0x03] = new Rgba(8, 8, 8, "BGR"),
            [0x04] = new Rgba(5, 6, 5),

            [0x0A] = new La(8, 8),
            [0x0B] = new La(4, 4),
            [0x0C] = new La(8, 0),
            [0x0D] = new La(4, 0),
            [0x0E] = new La(0, 8),
            [0x0F] = new La(0, 4),

            [0x1B] = new Etc1(false, true),
            [0x1C] = new Etc1(true, true)
        };

        // This mapping was determined through Time Travelers
        public static IDictionary<int, IColorEncoding> ImgcFormatsV2 = new Dictionary<int, IColorEncoding>
        {
            [0x00] = new Rgba(8, 8, 8, 8),
            [0x01] = new Rgba(4, 4, 4, 4),
            [0x02] = new Rgba(5, 5, 5, 1),
            [0x03] = new Rgba(8, 8, 8, "BGR"),
            [0x04] = new Rgba(5, 6, 5),

            [0x0B] = new La(8, 8),
            [0x0C] = new La(4, 4),
            [0x0D] = new La(8, 0),
            [0x0E] = new La(4, 0),
            [0x0F] = new La(0, 8),
            [0x10] = new La(0, 4),

            [0x1B] = new Etc1(false, true),
            [0x1C] = new Etc1(false, true),
            [0x1D] = new Etc1(true, true)
        };

        private static readonly IDictionary<string, IDictionary<int, IColorEncoding>> GameMapping =
            new Dictionary<string, IDictionary<int, IColorEncoding>>
            {
                ["Fantasy Life"] = ImgcFormatsV2, // TODO: Unconfirmed
                ["Inazuma Eleven GO"] = ImgcFormatsV2, // TODO: Unconfirmed
                ["Inazuma Eleven GO: Chrono Stones"] = ImgcFormatsV2, // TODO: Unconfirmed
                ["Inazuma Eleven GO: Galaxy"] = ImgcFormatsV1,
                ["Laytons Mystery Journey"] = ImgcFormatsV2, // TODO: Unconfirmed
                ["Professor Layton 5"] = ImgcFormatsV2, // TODO: Unconfirmed
                ["Professor Layton 6"] = ImgcFormatsV2, // TODO: Unconfirmed
                ["Professor Layton vs Phoenix Wright"] = ImgcFormatsV2, // TODO: Unconfirmed
                ["Time Travelers"] = ImgcFormatsV2,
                ["Yo-Kai Watch"] = ImgcFormatsV2, // TODO: Unconfirmed
                ["Yo-Kai Watch 2"] = ImgcFormatsV2, // TODO: Unconfirmed
                ["Yo-Kai Watch 3"] = ImgcFormatsV2, // TODO: Unconfirmed
                ["Yo-Kai Watch Blasters"] = ImgcFormatsV2, // TODO: Unconfirmed
                ["Yo-Kai Watch Blasters 2"] = ImgcFormatsV2, // TODO: Unconfirmed
                ["Yo-Kai Watch Sangokushi"] = ImgcFormatsV2, // TODO: Unconfirmed
            };
    }
}
