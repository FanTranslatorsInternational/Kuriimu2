using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Komponent.IO;
using Komponent.IO.Streams;
using Kompression.Implementations;
using Kontract.Models.Archive;

namespace plugin_ganbarion.Archives
{
    class Jcmp
    {
        private static readonly int HeaderSize = Tools.MeasureType(typeof(JcmpHeader));

        private Jarc _jarc;
        private JcmpHeader _header;

        public IList<IArchiveFileInfo> Load(Stream input)
        {
            using var br = new BinaryReaderX(input);

            // Read header
            _header = br.ReadType<JcmpHeader>();

            // Decompress data
            var jarcStream = new MemoryStream();
            Compressions.ZLib.Build().Decompress(new SubStream(input, 0x14, _header.compSize), jarcStream);
            jarcStream.Position = 0;

            _jarc = new Jarc();
            return _jarc.Load(jarcStream);
        }

        public void Save(Stream output, IList<IArchiveFileInfo> files)
        {
            using var bw = new BinaryWriterX(output);

            // Calculate offsets
            var dataOffset = HeaderSize;

            // Save and compress jarc
            var jarcStream = new MemoryStream();
            _jarc.Save(jarcStream, files);
            jarcStream.Position = 0;

            output.Position = dataOffset;
            Compressions.ZLib.Build().Compress(jarcStream, output);

            // Write header
            _header.decompSize = (int)jarcStream.Length;
            _header.compSize = (int)(output.Length - HeaderSize);
            _header.fileSize = (int)output.Length;

            output.Position = 0;
            bw.WriteType(_header);
        }
    }
}
