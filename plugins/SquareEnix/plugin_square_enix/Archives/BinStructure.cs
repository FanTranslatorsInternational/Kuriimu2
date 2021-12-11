﻿using System;
using System.Collections.Generic;
using System.Text;
using Komponent.IO.Attributes;

namespace plugin_square_enix.Archives
{
    public class Binheader
    {
        [FixedLength(4)]
        public string magic;
        public int fileCount;
        public int totalFileSizes;
        [FixedLength(5)]
        public int[] unknowns;
    }
    public class BinTableEntry
    {
        public int offset;
        public int fileSize;
    }
    public class BinStructure
    {
        public Binheader header;
        [VariableLength("header.fileCount")]
        public BinTableEntry[] entries;
    }
}
