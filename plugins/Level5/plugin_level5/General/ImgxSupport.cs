using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Kanvas;
using Kanvas.Encoding;
using Kanvas.Swizzle;
using Komponent.IO.Attributes;
using Kontract.Interfaces.Managers;
using Kontract.Kanvas;
using Kontract.Kanvas.Model;
using Kontract.Models.Dialog;
using Kontract.Models.Image;
#pragma warning disable 649

namespace plugin_level5.General
{
    class ImgxHeader
    {
        [FixedLength(4)]
        public string magic; // IMGx
        public int const1; // 30 30 00 00
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

    class ImgxSwizzle : IImageSwizzle
    {
        private readonly MasterSwizzle _swizzle;

        public int Width { get; }
        public int Height { get; }

        public ImgxSwizzle(SwizzlePreparationContext context, string magic)
        {
            Width = (context.Size.Width + 7) & ~7;
            Height = (context.Size.Height + 7) & ~7;

            switch (magic)
            {
                case "IMGC":
                    _swizzle = new MasterSwizzle(Width, Point.Empty, new[] { (0, 1), (1, 0), (0, 2), (2, 0), (0, 4), (4, 0) });
                    break;

                default:
                    _swizzle = context.EncodingInfo.ColorsPerValue > 1 ? 
                        new MasterSwizzle(Width, Point.Empty, new[] { (1, 0), (2, 0), (0, 1), (0, 2), (4, 0), (8, 0) }) : 
                        new MasterSwizzle(Width, Point.Empty, new[] { (1, 0), (2, 0), (4, 0), (0, 1), (0, 2), (0, 4) });
                    break;
            }
        }

        public Point Transform(Point point) => _swizzle.Get(point.Y * Width + point.X);
    }

    class ImgxSupport
    {
        public static EncodingDefinition GetEncodingDefinition(string magic, int format, int bitDepth, IDialogManager dialogManager)
        {
            switch (magic)
            {
                case "IMGC":
                    return GetCitraDefinition(format, bitDepth, dialogManager);

                case "IMGV":
                    return VitaFormats.ToColorDefinition();

                case "IMGA":
                    return MobileFormats.ToColorDefinition();

                case "IMGN":
                    return SwitchFormats.ToColorDefinition();

                default:
                    throw new InvalidOperationException($"Invalid IMGx magic {magic}.");
            }
        }

        private static readonly Dictionary<int, IColorEncoding> SwitchFormats = new Dictionary<int, IColorEncoding>
        {
            [0] = new Rgba(8, 8, 8, 8, "ABGR")
        };

        private static readonly IDictionary<int, IColorEncoding> MobileFormats = new Dictionary<int, IColorEncoding>
        {
            [0x03] = ImageFormats.Rgb888()
        };

        private static readonly Dictionary<int, IColorEncoding> VitaFormats = new Dictionary<int, IColorEncoding>
        {
            [0x03] = ImageFormats.Rgb888(),

            [0x1E] = ImageFormats.Dxt1()
        };

        // This mapping was determined through Inazuma Eleven GO Big Bang
        private static readonly IDictionary<int, IColorEncoding> CitraFormats1 = new Dictionary<int, IColorEncoding>
        {
            [0x00] = ImageFormats.Rgba8888(),
            [0x01] = ImageFormats.Rgba4444(),
            [0x02] = ImageFormats.Rgba5551(),
            [0x03] = new Rgba(8, 8, 8, "BGR"),
            [0x04] = ImageFormats.Rgb565(),

            [0x0A] = ImageFormats.La88(),
            [0x0B] = ImageFormats.La44(),
            [0x0C] = ImageFormats.L8(),
            [0x0D] = ImageFormats.L4(),
            [0x0E] = ImageFormats.A8(),
            [0x0F] = ImageFormats.A4(),

            [0x1B] = ImageFormats.Etc1(true),
            [0x1C] = ImageFormats.Etc1A4(true)
        };

        // This mapping was determined through Time Travelers
        private static readonly IDictionary<int, IColorEncoding> CitraFormats2 = new Dictionary<int, IColorEncoding>
        {
            [0x00] = ImageFormats.Rgba8888(),
            [0x01] = ImageFormats.Rgba4444(),
            [0x02] = ImageFormats.Rgba5551(),
            [0x03] = new Rgba(8, 8, 8, "BGR"),
            [0x04] = ImageFormats.Rgb565(),

            [0x0B] = ImageFormats.La88(),
            [0x0C] = ImageFormats.La44(),
            [0x0D] = ImageFormats.L8(),
            [0x0E] = ImageFormats.L4(),
            [0x0F] = ImageFormats.A8(),
            [0x10] = ImageFormats.A4(),

            [0x1B] = ImageFormats.Etc1(true),
            [0x1C] = ImageFormats.Etc1(true),
            [0x1D] = ImageFormats.Etc1A4(true)
        };

        private static EncodingDefinition GetCitraDefinition(int format, int bitDepth, IDialogManager dialogManager)
        {
            var encodingDefinitions = new[]
            {
                CitraFormats1.ToColorDefinition(),
                CitraFormats2.ToColorDefinition()
            };

            // If format does not exist in any
            if (encodingDefinitions.All(x => !x.ContainsColorEncoding(format)))
                return EncodingDefinition.Empty;

            // If the format exists only in one of the mappings
            if (encodingDefinitions.Count(x => x.ContainsColorEncoding(format)) == 1)
                return encodingDefinitions.First(x => x.ContainsColorEncoding(format));

            // If format exists in more than one, compare bitDepth
            var viableMappings = encodingDefinitions.Where(x => x.ContainsColorEncoding(format)).ToArray();

            // If all mappings are the same encoding
            var encodingName = viableMappings[0].GetColorEncoding(format).FormatName;
            if (viableMappings.All(x => x.GetColorEncoding(format).FormatName == encodingName))
                return viableMappings[0];

            // If only one mapping matches the given bitDepth
            if (viableMappings.Count(x => x.GetColorEncoding(format).BitDepth == bitDepth) == 1)
                return viableMappings.First(x => x.GetColorEncoding(format).BitDepth == bitDepth);

            // Otherwise the heuristic could not determine a definite mapping
            // Show a dialog to the user, selecting the game
            var availableGames = GameMapping.Keys.ToArray();
            var dialogField = new DialogField(DialogFieldType.DropDown, "Select the game:", availableGames.First(), availableGames);

            dialogManager.ShowDialog(new[] { dialogField });
            return GameMapping[dialogField.Result].ToColorDefinition();
        }

        private static readonly IDictionary<string, IDictionary<int, IColorEncoding>> GameMapping =
            new Dictionary<string, IDictionary<int, IColorEncoding>>
            {
                ["Fantasy Life"] = CitraFormats2, // TODO: Unconfirmed
                ["Inazuma Eleven GO"] = CitraFormats2, // TODO: Unconfirmed
                ["Inazuma Eleven GO: Chrono Stones"] = CitraFormats2, // TODO: Unconfirmed
                ["Inazuma Eleven GO: Galaxy"] = CitraFormats1,
                ["Laytons Mystery Journey"] = CitraFormats2, // TODO: Unconfirmed
                ["Professor Layton 5"] = CitraFormats2, // TODO: Unconfirmed
                ["Professor Layton 6"] = CitraFormats2, // TODO: Unconfirmed
                ["Professor Layton vs Phoenix Wright"] = CitraFormats2, // TODO: Unconfirmed
                ["Time Travelers"] = CitraFormats2,
                ["Yo-Kai Watch"] = CitraFormats2, // TODO: Unconfirmed
                ["Yo-Kai Watch 2"] = CitraFormats2, // TODO: Unconfirmed
                ["Yo-Kai Watch 3"] = CitraFormats2, // TODO: Unconfirmed
                ["Yo-Kai Watch Blasters"] = CitraFormats2, // TODO: Unconfirmed
                ["Yo-Kai Watch Blasters 2"] = CitraFormats2, // TODO: Unconfirmed
                ["Yo-Kai Watch Sangokushi"] = CitraFormats2, // TODO: Unconfirmed
            };
    }
}
