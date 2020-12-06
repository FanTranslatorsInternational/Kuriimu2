using System.Collections.Generic;
using System.IO;
using System.Linq;
using Komponent.IO;
using Komponent.IO.Streams;
using Kontract.Models.Archive;

namespace plugin_sony.Archives
{
    class Ps2Disc
    {
        private const int SectorSize_ = 0x800;
        private const int DescriptorStart_ = 0x10 * SectorSize_;

        public IList<IArchiveFileInfo> Load(Stream input)
        {
            //using var reader = new CDReader(input, false);
            //return EnumerateFiles(reader.Root).ToArray();

            using var br = new BinaryReaderX(input, true);

            // Read volume descriptor
            input.Position = DescriptorStart_;
            var descriptor = br.ReadType<IsoVolumeDescriptor>();

            // Read directory entries
            return ReadDirectories(br, descriptor.descriptorPrimary.rootDir).ToArray();
        }

        private IEnumerable<IArchiveFileInfo> ReadDirectories(BinaryReaderX br, IsoDirEntry directory, string currentPath = "")
        {
            var directoryOffset = directory.body.lbaExtent.Value * SectorSize_;
            var internalOffset = 0x60;  // Skip self entry and parent entry directly

            while (PeekByte(br, directoryOffset + internalOffset) != 0)
            {
                br.BaseStream.Position = directoryOffset + internalOffset;

                var entry = br.ReadType<IsoDirEntry>();
                br.BaseStream.Position += 0xE;
                internalOffset = (int)(br.BaseStream.Position - directoryOffset);

                // Continue reading if the entry points to the current one
                if (entry.body.lbaExtent.Value == directory.body.lbaExtent.Value)
                    continue;

                // Read all sub directories and files of this directory
                if (entry.IsDirectory)
                {
                    foreach (var afi in ReadDirectories(br, entry, Path.Combine(currentPath, entry.body.fileName)))
                        yield return afi;
                    continue;
                }

                // Otherwise return file
                var subStream = new SubStream(br.BaseStream, entry.body.lbaExtent.Value * SectorSize_, entry.body.sizeExtent.Value);
                yield return new Ps2DiscArchiveFileInfo(subStream, Path.Combine(currentPath, entry.body.fileName), entry);
            }
        }

        // TODO: Move this functionality into BinaryReaderX
        private byte PeekByte(BinaryReaderX br, long offset)
        {
            var bkPos = br.BaseStream.Position;

            br.BaseStream.Position = offset;
            var value = br.ReadByte();

            br.BaseStream.Position = bkPos;

            return value;
        }
    }
}
