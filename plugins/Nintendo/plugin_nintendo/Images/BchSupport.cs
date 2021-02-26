using System;
using System.Collections.Generic;
using System.Text;
using Kanvas.Encoding;
using Komponent.IO.Attributes;
using Kontract.Kanvas;
using Kontract.Models.Image;

namespace plugin_nintendo.Images
{
    class BchHeader
    {
        [FixedLength(4)]
        public string magic;
        public byte backwardComp;
        public byte forwardComp;
        public ushort version;

        public uint mainHeaderOffset;
        public uint nameTableOffset;
        public uint gpuCommandsOffset;
        public uint dataOffset;
        [Condition("backwardComp", ConditionComparer.Greater, 0x20)]
        public uint dataExtOffset = 0;
        public uint relocTableOffset;

        public uint mainHeaderSize;
        public uint nameTableSize;
        public uint gpuCommandsSize;
        public uint dataSize;
        [Condition("backwardComp", ConditionComparer.Greater, 0x20)]
        public uint dataExtSize = 0;
        public uint relocTableSize;

        public uint uninitDataSectionSize;
        public uint uninitDescSectionSize;

        [Condition("backwardComp", ConditionComparer.Greater, 7)]
        public ushort flags;
        [Condition("backwardComp", ConditionComparer.Greater, 7)]
        public ushort addressCount;
    }

    class BchSupport
    {
        public static EncodingDefinition GetEncodingDefinition()
        {
            var formats = new Dictionary<int, IColorEncoding>
            {
                [0] = new Rgba(8, 8, 8, 8),
                [1] = new Rgba(8, 8, 8),
                [2] = new Rgba(5, 5, 5, 1),
                [3] = new Rgba(5, 6, 5),
                [4] = new Rgba(4, 4, 4, 4),
                [5] = new La(8, 8),
                [6] = new Rgba(8, 8, 0),
                [7] = new La(8, 0),
                [8] = new La(0, 8),
                [9] = new La(4, 4),
                [10] = new La(4, 0),
                [11] = new La(0, 4),
                [12] = new Etc1(false, true),
                [13] = new Etc1(true, true)
            };

            var definition = EncodingDefinition.Empty;
            definition.AddColorEncodings(formats);

            return definition;
        }
    }
}
