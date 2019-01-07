using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Komponent.IO;
using Kontract.Interfaces.Text;

namespace plugin_space_channel_5.TEXT
{
    public sealed class TEXT
    {
        /// <summary>
        /// The size in bytes of the MTPA Header.
        /// </summary>
        private const int TextHeaderSize = 0x10;

        /// <summary>
        /// The size in bytes of each group metadata entry.
        /// </summary>
        private const int GroupMetadataEntrySize = 0x10;

        /// <summary>
        /// The list of text entries in the file.
        /// </summary>
        public List<TextEntry> Entries { get; set; } = new List<TextEntry>();

        #region InstanceData

        private readonly TEXTHeader _textHeader;

        #endregion
        
        /// <summary>
        /// Creates a new blank TEXT file.
        /// </summary>
        public TEXT()
        {
            _textHeader = new TEXTHeader();
        }

        /// <summary>
        /// Read a TEXT file into memory.
        /// </summary>
        /// <param name="input">A readable stream of a TEXT file.</param>
        public TEXT(Stream input)
        {
            using (var br = new BinaryReaderX(input))
            {
                // TEXT Header
                _textHeader = br.ReadStruct<TEXTHeader>();

                // Text Group Metadata
                var groupMetadata = br.ReadMultiple<TextGroupMetadata>(_textHeader.EntryCount);

                // Read Strings
                foreach (var group in groupMetadata)
                {
                    br.BaseStream.Position = group.NameOffset;
                    var name = br.ReadCStringASCII();

                    br.BaseStream.Position = group.EntryOffset;
                    var offsets = br.ReadMultiple<int>(group.EntryCount);

                    var lines = new List<string>();
                    foreach (var offset in offsets)
                    {
                        br.BaseStream.Position = offset;
                        var length = br.ReadCStringASCII().Length;
                        br.BaseStream.Position = offset;
                        lines.Add(br.ReadString(length, Encoding.GetEncoding(1252)));
                    }

                    Entries.Add(new TextEntry
                    {
                        Name = name,
                        EditedText = string.Join("\n", lines)
                    });
                }
            }
        }

        /// <summary>
        /// Write a TEXT file to disk.
        /// </summary>
        /// <param name="output">A writable stream of a TEXT file.</param>
        public void Save(Stream output)
        {
            using (var bw = new BinaryWriterX(output))
            {
                // Figure out how long the metadata is
                var lineCount = 0;
                foreach (var entry in Entries)
                    lineCount += entry.EditedText.Split(new[] { "\n" }, StringSplitOptions.RemoveEmptyEntries).Length;

                var offsetStart = TextHeaderSize + Entries.Count * GroupMetadataEntrySize;
                var textStart = offsetStart + lineCount * sizeof(int);

                var groupMetadata = new List<TextGroupMetadata>();
                var offsets = new List<int>();

                // Write out our entries
                bw.BaseStream.Position = textStart;
                var offset = offsetStart;
                foreach (var entry in Entries)
                {
                    var meta = new TextGroupMetadata();
                    var lines = entry.EditedText.Split(new[] { "\n" }, StringSplitOptions.RemoveEmptyEntries).ToList();

                    // Metadata
                    meta.NameOffset = (int)bw.BaseStream.Position;
                    meta.EntryCount = lines.Count;
                    meta.EntryOffset = offset;
                    groupMetadata.Add(meta);

                    // Name
                    bw.Write(Encoding.GetEncoding(1252).GetBytes(entry.Name));
                    bw.Write((byte)0x0);

                    // Lines
                    if (lines.Count > 0)
                        foreach (var line in lines)
                        {
                            offsets.Add((int)bw.BaseStream.Position);
                            bw.Write(Encoding.GetEncoding(1252).GetBytes(line));
                            bw.Write((byte)0x0);
                        }

                    offset += lines.Count * sizeof(int);
                }

                // Update the header
                _textHeader.FileSize = (int)bw.BaseStream.Position;
                _textHeader.EntryCount = Entries.Count;

                // Write the header
                bw.BaseStream.Position = 0;
                bw.WriteStruct(_textHeader);

                // Write the group metadata
                bw.WriteMultiple(groupMetadata);

                // Write the line offsets
                bw.WriteMultiple(offsets);
            }
        }
    }
}
