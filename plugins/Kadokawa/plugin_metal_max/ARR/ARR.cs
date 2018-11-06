using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Komponent.IO;
using Kontract.Interfaces;

namespace plugin_metal_max.ARR
{
    public sealed class ARR
    {
        /// <summary>
        /// The list of text entries in the file.
        /// </summary>
        public List<TextEntry> Entries { get; set; } = new List<TextEntry>();

        /// <summary>
        /// Read an ARR file into memory.
        /// </summary>
        /// <param name="input">A readable stream of an ARR file.</param>
        public ARR(Stream input)
        {
            using (var br = new BinaryReaderX(input))
            {
                var count = br.ReadInt32();

                var pointers = new List<short>();

                for (var i = 0; i < count; i++)
                {
                    for (var j = 0; j < 7; j++)
                    {
                        var p = br.ReadInt16();
                        if (p == 0) continue;
                        pointers.Add(p);
                    }
                }

                for (var i = 0; i < pointers.Count; i++)
                {
                    br.BaseStream.Position = pointers[i];

                    short b = 0;
                    var chars = new List<byte>();
                    do
                    {
                        b = br.ReadInt16();
                        chars.AddRange(BitConverter.GetBytes(b));
                    }
                    while (b != 0);

                    var str = Encoding.Unicode.GetString(chars.ToArray());

                    Entries.Add(new TextEntry { Name = i.ToString(), EditedText = str.TrimEnd('\0') });

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
                // Counts how many entires in each group
                var pointerGroupCount = Entries.Count / 6;

                // Gets the starting pointer
                var textStartOffset = pointerGroupCount * 14 + 4;

                var pointerList = new List<short>();

                bw.BaseStream.Position = textStartOffset;

                foreach (var entry in Entries)
                {
                    if (entry.EditedText != string.Empty)
                    {
                        pointerList.Add((short)bw.BaseStream.Position);
                        bw.Write(Encoding.Unicode.GetBytes(entry.EditedText));
                        bw.Write((byte)0x0);
                        bw.Write((byte)0x0);
                    }
                    else
                    {
                        pointerList.Add((short)(textStartOffset - 2));
                    }


                }
                bw.BaseStream.Position = 0;
                bw.Write(pointerGroupCount);

                for (var i = 0; i < pointerList.Count; i++)
                {
                    if (i % 7 == 0)
                        bw.Write((short)0);
                    bw.Write(pointerList[i]);
                }
            }
        }

    }
}
