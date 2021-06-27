﻿using System.Collections.Generic;
using Kanvas;
using Komponent.IO.Attributes;
using Kontract.Kanvas;
using Kontract.Models.Image;

namespace plugin_ganbarion.Images
{
    class JtexHeader
    {
        [FixedLength(4)]
        public string magic = "jIMG";
        public int fileSize;
        public short width;
        public short height;

        public byte format;
        public byte unkCount;
        public byte unk1;
        public byte unk2;

        [VariableLength(nameof(unkCount))]
        public int[] unkList;

        public short unk3;
        public short dataOffset;
        public int dataSize;
    }

    class JtexSupport
    {
        private static readonly IDictionary<int, IColorEncoding> Formats = new Dictionary<int, IColorEncoding>
        {
            [0] = ImageFormats.Rgba8888(),
            [8] = ImageFormats.Etc1(true),
            [11] = ImageFormats.Etc1A4(true),
            [15] = ImageFormats.Etc1(true),
            [16] = ImageFormats.A8()
        };

        public static EncodingDefinition GetEncodingDefinition()
        {
            var definition = new EncodingDefinition();
            definition.AddColorEncodings(Formats);

            return definition;
        }
    }
}
