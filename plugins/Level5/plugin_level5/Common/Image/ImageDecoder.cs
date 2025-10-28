using Kanvas;
using Kanvas.Encoding;
using Komponent.Contract.Enums;
using Konnect.Contract.DataClasses.Management.Dialog;
using Konnect.Contract.DataClasses.Plugin.File.Image;
using Konnect.Contract.Enums.Management.Dialog;
using Konnect.Contract.Management.Dialog;
using Konnect.Contract.Plugin.File.Image;
using Konnect.Plugin.File.Image;
using plugin_level5.Common.Image.Models;
using SixLabors.ImageSharp;
using ByteOrder = Komponent.Contract.Enums.ByteOrder;

namespace plugin_level5.Common.Image
{
    internal class ImageDecoder
    {
        private readonly IDialogManager _dialogManager;

        public ImageDecoder(IDialogManager dialogManager)
        {
            _dialogManager = dialogManager;
        }

        public async Task<ImageData> Decode(ImageRawData imageData)
        {
            EncodingDefinition definition = await GetEncodingDefinition(imageData);
            return GetImage(imageData, definition);
        }

        private ImageData GetImage(ImageRawData imageData, EncodingDefinition definition)
        {
            return new ImageData
            {
                Version = imageData.Version,
                Image = GetMainImage(imageData, definition),
                LegacyData = imageData.LegacyData
            };
        }

        private IImageFile GetMainImage(ImageRawData imageData, EncodingDefinition definition)
        {
            var imageInfo = new ImageFileInfo
            {
                BitDepth = imageData.BitDepth,
                ImageSize = new Size(imageData.Width, imageData.Height),
                ImageFormat = imageData.Format,
                ImageData = imageData.Data,
                MipMapData = imageData.MipMapData,
                RemapPixels = options => new ImgSwizzle(options, imageData.Version.Platform)
            };

            if (imageData.PaletteFormat >= 0)
            {
                imageInfo.PaletteBitDepth = imageData.PaletteBitDepth;
                imageInfo.PaletteFormat = imageData.PaletteFormat;
                imageInfo.PaletteData = imageData.PaletteData;
            }

            return new ImageFile(imageInfo, definition);
        }

        private async Task<EncodingDefinition> GetEncodingDefinition(ImageRawData imageData)
        {
            return imageData.Version.Platform switch
            {
                PlatformType.Ctr => await CreateCtrFormats(imageData),
                PlatformType.Psp => CreatePspFormats(),
                PlatformType.PsVita => CreateVitaFormats(),
                PlatformType.Switch => CreateSwitchFormats(),
                PlatformType.Android => CreateAndroidFormats(),
                _ => throw new InvalidOperationException($"Unsupported platform {imageData.Version.Platform} for image.")
            };
        }

        private async Task<EncodingDefinition> CreateCtrFormats(ImageRawData imageData)
        {
            EncodingDefinition[] encodingDefinitions =
            [
                CreateCtr1Formats(),
                CreateCtr2Formats()
            ];

            // If format does not exist in any
            if (encodingDefinitions.All(x => !x.ContainsColorEncoding(imageData.Format)))
                return EncodingDefinition.Empty;

            // If the format exists only in one of the mappings
            if (encodingDefinitions.Count(x => x.ContainsColorEncoding(imageData.Format)) == 1)
                return encodingDefinitions.First(x => x.ContainsColorEncoding(imageData.Format));

            // If format exists in more than one, compare bitDepth
            EncodingDefinition[] viableMappings = encodingDefinitions.Where(x => x.ContainsColorEncoding(imageData.Format)).ToArray();

            // If only one mapping matches the given bitDepth
            if (viableMappings.Count(x => x.GetColorEncoding(imageData.Format)?.BitDepth == imageData.BitDepth) == 1)
                return viableMappings.First(x => x.GetColorEncoding(imageData.Format)?.BitDepth == imageData.BitDepth);

            // Otherwise the heuristic could not determine a definite mapping
            // Show a dialog to the user, selecting the game
            return await RequestCtrEncodingDefinition();
        }

        private async Task<EncodingDefinition> RequestCtrEncodingDefinition()
        {
            var gameMapping = new Dictionary<string, int>
            {
                ["Fantasy Life"] = 1, // TODO: Unconfirmed
                ["Inazuma Eleven GO"] = 1, // TODO: Unconfirmed
                ["Inazuma Eleven GO: Chrono Stones"] = 1, // TODO: Unconfirmed
                ["Inazuma Eleven GO: Galaxy"] = 0,
                ["Laytons Mystery Journey"] = 1, // TODO: Unconfirmed
                ["Professor Layton 5"] = 1, // TODO: Unconfirmed
                ["Professor Layton 6"] = 1, // TODO: Unconfirmed
                ["Professor Layton vs Phoenix Wright"] = 1, // TODO: Unconfirmed
                ["Time Travelers"] = 1,
                ["The Snack World TreJarers"] = 1, // TODO: Unconfirmed
                ["Yo-Kai Watch"] = 1, // TODO: Unconfirmed
                ["Yo-Kai Watch 2"] = 1, // TODO: Unconfirmed
                ["Yo-Kai Watch 3"] = 1, // TODO: Unconfirmed
                ["Yo-Kai Watch Blasters"] = 1, // TODO: Unconfirmed
                ["Yo-Kai Watch Blasters 2"] = 1, // TODO: Unconfirmed
            };

            string[] availableGames = gameMapping.Keys.ToArray();
            var dialogField = new DialogField
            {
                Type = DialogFieldType.DropDown,
                Text = "Select the game:",
                DefaultValue = availableGames.First(),
                Options = availableGames
            };

            bool result = await _dialogManager.ShowDialog([dialogField]);
            int definitionIndex = gameMapping[result ? dialogField.Result ?? dialogField.DefaultValue : dialogField.DefaultValue];

            return definitionIndex switch
            {
                0 => CreateCtr1Formats(),
                1 => CreateCtr2Formats(),
                _ => throw new InvalidOperationException($"Invalid selected encoding definition index {definitionIndex}.")
            };
        }

        private EncodingDefinition CreateCtr1Formats()
        {
            var result = new EncodingDefinition();

            result.AddColorEncoding(0x00, ImageFormats.Rgba8888());
            result.AddColorEncoding(0x01, ImageFormats.Rgba4444());
            result.AddColorEncoding(0x02, ImageFormats.Rgba5551());
            result.AddColorEncoding(0x03, new Rgba(8, 8, 8, "BGR"));
            result.AddColorEncoding(0x04, ImageFormats.Rgb565());
            result.AddColorEncoding(0x0A, ImageFormats.La88());
            result.AddColorEncoding(0x0B, ImageFormats.La44());
            result.AddColorEncoding(0x0C, ImageFormats.L8());
            result.AddColorEncoding(0x0D, ImageFormats.L4());
            result.AddColorEncoding(0x0E, ImageFormats.A8());
            result.AddColorEncoding(0x0F, ImageFormats.A4());
            result.AddColorEncoding(0x1B, ImageFormats.Etc1(true));
            result.AddColorEncoding(0x1C, ImageFormats.Etc1A4(true));

            return result;
        }

        private EncodingDefinition CreateCtr2Formats()
        {
            var result = new EncodingDefinition();

            result.AddColorEncoding(0x00, ImageFormats.Rgba8888());
            result.AddColorEncoding(0x01, ImageFormats.Rgba4444());
            result.AddColorEncoding(0x02, ImageFormats.Rgba5551());
            result.AddColorEncoding(0x03, new Rgba(8, 8, 8, "BGR"));
            result.AddColorEncoding(0x04, ImageFormats.Rgb565());
            result.AddColorEncoding(0x0B, ImageFormats.La88());
            result.AddColorEncoding(0x0C, ImageFormats.La44());
            result.AddColorEncoding(0x0D, ImageFormats.L8());
            result.AddColorEncoding(0x0E, ImageFormats.L4());
            result.AddColorEncoding(0x0F, ImageFormats.A8());
            result.AddColorEncoding(0x10, ImageFormats.A4());
            result.AddColorEncoding(0x1B, ImageFormats.Etc1(true));
            result.AddColorEncoding(0x1C, ImageFormats.Etc1(true));
            result.AddColorEncoding(0x1D, ImageFormats.Etc1A4(true));

            return result;
        }

        private EncodingDefinition CreatePspFormats()
        {
            var result = new EncodingDefinition();

            result.AddPaletteEncoding(0x00, ImageFormats.Rgba8888(ByteOrder.BigEndian));
            result.AddPaletteEncoding(0x01, new Rgba(4, 4, 4, 4, "ARGB"));
            result.AddPaletteEncoding(0x02, new Rgba(5, 5, 5, 1, "ABGR"));

            result.AddColorEncoding(0x00, ImageFormats.Rgba8888(ByteOrder.BigEndian));
            result.AddIndexEncoding(0x10, ImageFormats.I8(), [0, 1, 2]);
            result.AddIndexEncoding(0x11, ImageFormats.I8(), [0, 1, 2]);
            result.AddIndexEncoding(0x13, ImageFormats.I8(), [0, 1, 2]);
            result.AddIndexEncoding(0x15, ImageFormats.I4(BitOrder.LeastSignificantBitFirst), [0, 1, 2]);
            result.AddIndexEncoding(0x17, ImageFormats.I4(BitOrder.LeastSignificantBitFirst), [0, 1, 2]);

            return result;
        }

        private static EncodingDefinition CreateVitaFormats()
        {
            var result = new EncodingDefinition();

            result.AddColorEncoding(0x03, ImageFormats.Rgb888());
            result.AddColorEncoding(0x1E, ImageFormats.Dxt1());

            return result;
        }

        private static EncodingDefinition CreateSwitchFormats()
        {
            var result = new EncodingDefinition();

            result.AddColorEncoding(0x00, new Rgba(8, 8, 8, 8, "ABGR"));
            result.AddColorEncoding(0x0E, ImageFormats.A8());

            return result;
        }

        private static EncodingDefinition CreateAndroidFormats()
        {
            var result = new EncodingDefinition();

            result.AddColorEncoding(0x03, ImageFormats.Rgb888());

            return result;
        }
    }
}
