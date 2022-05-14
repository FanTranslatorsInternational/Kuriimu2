using Komponent.IO;
using Komponent.IO.Streams;
using Kontract.Models.Archive;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace plugin_capcom.Archives
{
    public class GtPac
    {
        public IList<IArchiveFileInfo> Load(Stream input)
        {
            using var br = new BinaryReaderX(input, true);
            var _header = br.ReadType<GtPacHeader>();

            var offsets = br.ReadMultiple<int>(_header.fileCount);
            
            // Add files
            var result = new List<IArchiveFileInfo>();
            for (var i = 0; i < _header.fileCount; i++)
            {
                try
                {
                    br.BaseStream.Position = offsets[i];
                    var length = offsets[i + 1] - offsets[i];
                    var subStream = new SubStream(input, offsets[i], length);

                    result.Add(new ArchiveFileInfo(subStream, "file" + i + ".bin"));
                }
                catch
                {
                    System.Diagnostics.Debug.WriteLine("Error on" + i.ToString());
                }

            }

            return result;
        }
    }
}
