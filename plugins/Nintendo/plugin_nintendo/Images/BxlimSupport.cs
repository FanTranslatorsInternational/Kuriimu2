using System;
using System.Collections.Generic;
using Kanvas.Encoding;
using Kanvas.Swizzle.Models;
using Kontract.Kanvas;
using Kontract.Models.Image;
using Kontract.Models.IO;
using plugin_nintendo.NW4C;

#pragma warning disable 649

namespace plugin_nintendo.Images
{
    class BclimHeader
    {
        public short width;
        public short height;
        public byte format;
        public CtrTransformation transformation;
        public short alignment;
        public int dataSize;
    }

    class BflimHeader
    {
        public short width;
        public short height;
        public short alignment;
        public byte format;
        public byte swizzleTileMode;
        public int dataSize;
    }

    class BxlimSupport
    {
        public static EncodingDefinition GetCtrDefinition()
        {
            var definition = new EncodingDefinition();
            definition.AddColorEncodings(Nw4cImageFormats.CtrFormats);

            return definition;
        }

        public static EncodingDefinition GetCafeDefinition()
        {
            var definition = new EncodingDefinition();
            definition.AddColorEncodings(Nw4cImageFormats.CafeFormats);

            return definition;
        }
    }
}
