using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Komponent.IO;

namespace plugin_sony_images.GIM
{
    public sealed class GIMHeader
    {
        [FixedLength(16)]
        public string Magic;
        public int Count;
        public int FileSize; // Minus Magic (0x10)
    }

    public sealed class MIGChunk
    {
        public int ID;
        public int ChildChunkOffset;
        // They seem to point to the end of the MIG, except for the image data chunk;
        // Image data chunk points to the chunk with the palette, which I would call a child of the image data
        public int ChunkSize; // With header
        public int ChunkHeaderSize;
	
        public byte[] Data;
    }
}
