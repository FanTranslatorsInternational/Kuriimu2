using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using Kanvas;
using Kanvas.Encoding;
using Kanvas.Swizzle;
using Kanvas.Swizzle.Models;
using Komponent.IO;
using Komponent.IO.Attributes;
using Kontract.Interfaces.Managers;
using Kontract.Kanvas;
using Kontract.Kanvas.Model;
using Kontract.Models.Dialog;
using Kontract.Models.Image;
using Kontract.Models.IO;

namespace plugin_gust.Images
{
    class G1tHeader
    {
        [FixedLength(8)]
        public string magic;
        public int fileSize;
        public int dataOffset;
        public int texCount;
        public int unk1;
        public int unk2;
    }

    class G1tEntry
    {
        public byte mipUnk;
        public byte format;
        public byte dimension;
        public byte zero0;

        public byte swizzle;
        public byte unk3;
        public byte unk4;
        public byte extHeader;

        [Condition(nameof(extHeader), ConditionComparer.Greater, 0)]
        public int extHeaderSize;

        [Condition(nameof(extHeader), ConditionComparer.Greater, 0)]
        [VariableLength(nameof(extHeaderSize), Offset = -4)]
        public byte[] extHeaderContent;

        public int Height
        {
            get => (int)Math.Pow(2, (dimension & 0xF0) >> 4);
            set => dimension = (byte)((dimension & 0x0F) | (((int)Math.Log(value - 1, 2) + 1) << 4));
        }

        public int Width
        {
            get => (int)Math.Pow(2, dimension & 0x0F);
            set => dimension = (byte)((dimension & 0xF0) | (((int)Math.Log(value - 1, 2) + 1) & 0x0F));
        }

        public int MipCount
        {
            get => mipUnk >> 4;
            set => mipUnk = (byte)((mipUnk & 0x0F) | (value << 4));
        }
    }

    enum G1tPlatform
    {
        N3DS,
        Vita,
        PlayStation,
        PC,
        Switch
    }

    class G1tImageInfo : ImageInfo
    {
        public G1tEntry Entry { get; }

        public G1tImageInfo(byte[] imageData, int imageFormat, Size imageSize, G1tEntry entry) : base(imageData, imageFormat, imageSize)
        {
            Entry = entry;
        }
    }

    class G1tSupport
    {
        private static readonly IDictionary<int, IColorEncoding> CitraFormats = new Dictionary<int, IColorEncoding>
        {
            [0x09] = ImageFormats.Rgba8888(),

            [0x3B] = ImageFormats.Rgb565(),
            [0x3C] = ImageFormats.Rgba5551(),
            [0x3D] = ImageFormats.Rgba4444(),

            [0x44] = ImageFormats.A4(BitOrder.LeastSignificantBitFirst),
            [0x45] = ImageFormats.Rgb888(),

            [0x47] = ImageFormats.Etc1(true),
            [0x48] = ImageFormats.Etc1A4(true)
        };

        private static readonly IDictionary<int, IColorEncoding> VitaFormats = new Dictionary<int, IColorEncoding>
        {
            [0x00] = ImageFormats.Rgba8888(),
            [0x01] = new Rgba(8, 8, 8, 8, "BGRA"),

            [0x06] = ImageFormats.Dxt1(),

            [0x08] = ImageFormats.Dxt5(),

            [0x10] = ImageFormats.Dxt1(),

            [0x12] = ImageFormats.Dxt5()
        };

        private static readonly IDictionary<int, IColorEncoding> PlayStationFormats = new Dictionary<int, IColorEncoding>
        {
            [0x01] = ImageFormats.Rgba4444(),

            [0x59] = ImageFormats.Dxt1(),

            [0x5B] = ImageFormats.Dxt5(),

            [0x5F] = ImageFormats.Rgba8888(ByteOrder.BigEndian)
        };

        private static readonly IDictionary<int, IColorEncoding> PcFormats = new Dictionary<int, IColorEncoding>
        {
            [0x01] = new Rgba(8, 8, 8, 8, "ARGB")
        };

        private static readonly IDictionary<int, IColorEncoding> SwitchFormats = new Dictionary<int, IColorEncoding>
        {
            [0x01] = new Rgba(8, 8, 8, 8, "ARGB"),

            [0x59] = ImageFormats.Dxt1(),

            [0x5B] = ImageFormats.Dxt5(),

            [0x5F] = ImageFormats.Bc7()
        };

        public static G1tPlatform DeterminePlatform(Stream input, IDialogManager manager)
        {
            using var br = new BinaryReaderX(input, true);

            // Collect formats
            input.Position = 0xC;
            var dataOffset = br.ReadInt32();
            var texCount = br.ReadInt32();

            input.Position = dataOffset;
            var offsets = br.ReadMultiple<int>(texCount);

            var formats = new List<byte>();
            foreach (var offset in offsets)
            {
                input.Position = dataOffset + offset + 1;
                formats.Add(br.ReadByte());
            }

            // Match formats to platform
            var givenFormats = new List<ICollection<int>> { CitraFormats.Keys, VitaFormats.Keys, PlayStationFormats.Keys, PcFormats.Keys, SwitchFormats.Keys };
            var givenPlatforms = new List<G1tPlatform> { G1tPlatform.N3DS, G1tPlatform.Vita, G1tPlatform.PlayStation, G1tPlatform.PC, G1tPlatform.Switch };

            foreach (var format in formats)
            {
                var existingCount = givenFormats.Count(gf => gf.Contains(format));

                // If format does not exist on any platform, throw exception
                if (existingCount == 0)
                    throw new InvalidOperationException($"Unknown image format {format}.");

                // If format is unique through all platforms, return platform monicker
                if (existingCount == 1)
                    return givenPlatforms[givenFormats.IndexOf(givenFormats.First(gf => gf.Contains(format)))];

                // If format is found multiple times, just continue to see if another format can identify uniquely
            }

            // If a platform could still not be uniquely identified, ask the user for the platform to use that could make sense
            var possiblePlatforms = givenFormats.Select((x, i) => (x, i)).Where(x => formats.All(y => x.x.Contains(y))).Select(x => givenPlatforms[x.i]);
            var platformNames = possiblePlatforms.Select(x => Enum.GetName(typeof(G1tPlatform), x)).ToArray();
            var field = new DialogField(DialogFieldType.DropDown, "Platform", platformNames[0], platformNames);

            manager.ShowDialog(new[] { field });

            return Enum.Parse<G1tPlatform>(field.Result);
        }

        public static EncodingDefinition GetEncodingDefinition(G1tPlatform platform)
        {
            var definition = new EncodingDefinition();

            switch (platform)
            {
                case G1tPlatform.N3DS:
                    definition.AddColorEncodings(CitraFormats);
                    break;

                case G1tPlatform.Vita:
                    definition.AddColorEncodings(VitaFormats);
                    break;

                case G1tPlatform.PlayStation:
                    definition.AddColorEncodings(PlayStationFormats);
                    break;

                case G1tPlatform.PC:
                    definition.AddColorEncodings(PcFormats);
                    break;

                case G1tPlatform.Switch:
                    definition.AddColorEncodings(SwitchFormats);
                    break;

                default:
                    throw new InvalidOperationException($"Unsupported platform {platform}.");
            }

            return definition;
        }

        public static int GetBitDepth(int format, G1tPlatform platform)
        {
            switch (platform)
            {
                case G1tPlatform.N3DS:
                    return CitraFormats[format].BitDepth;

                case G1tPlatform.Vita:
                    return VitaFormats[format].BitDepth;

                case G1tPlatform.PlayStation:
                    return PlayStationFormats[format].BitDepth;

                case G1tPlatform.PC:
                    return PcFormats[format].BitDepth;

                case G1tPlatform.Switch:
                    return SwitchFormats[format].BitDepth;

                default:
                    throw new InvalidOperationException($"Unsupported platform {platform}.");
            }
        }

        public static IImageSwizzle GetSwizzle(SwizzlePreparationContext context, int format, G1tPlatform platform)
        {
            switch (platform)
            {
                case G1tPlatform.N3DS:
                    return new CtrSwizzle(context, CtrTransformation.YFlip);

                case G1tPlatform.Vita:
                    return new VitaSwizzle(context);

                //case G1tPlatform.Switch:
                //    return new NxSwizzle(context);

                // TODO: Remove with prepend swizzle
                case G1tPlatform.PlayStation:
                    if (PlayStationFormats[format].ColorsPerValue > 1)
                        return new BcSwizzle(context);

                    return null;

                case G1tPlatform.Switch:
                    if (SwitchFormats[format].ColorsPerValue > 1)
                        return new BcSwizzle(context);

                    return null;

                case G1tPlatform.PC:
                    return null;

                default:
                    throw new InvalidOperationException($"Unsupported platform {platform}.");
            }
        }
    }
}
