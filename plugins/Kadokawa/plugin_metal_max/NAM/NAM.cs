using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Komponent.IO;
using Kontract.Interfaces.Text;

namespace plugin_metal_max.NAM
{
    public sealed class NAM
    {
        /// <summary>
        /// The list of text entries in the file.
        /// </summary>
        public List<TextEntry> Entries { get; set; } = new List<TextEntry>();

        public NamFile NamFile { get; private set; } = NamFile.ItemList;

        private bool _hasArr = false;

        #region ARR

        private List<ItemListArrEntry> _itemListArrEntries;

        //private List<ItemListArrEntry> _something;

        #endregion

        /// <summary>
        /// Read an NAM file into memory.
        /// </summary>
        /// <param name="input">A readable stream of an NAM file.</param>
        public NAM(Stream input, Stream inputARR, string filename)
        {
            _hasArr = inputARR != null;

            switch (filename)
            {
                case "ITEMLIST.NAM":
                    NamFile = NamFile.ItemList;
                    break;
            }

            var arrOffsets = new List<short>();

            if (_hasArr)
            {
                using (var br = new BinaryReaderX(inputARR))
                {
                    switch (NamFile)
                    {
                        case NamFile.ItemList:
                            _itemListArrEntries = br.ReadMultiple<ItemListArrEntry>((int)br.BaseStream.Length / (int)NamFile);
                            arrOffsets.AddRange(_itemListArrEntries.Select(e => e.Offset));
                            break;
                    }
                }

                using (var br = new BinaryReaderX(input))
                {
                    for (var i = 0; i < arrOffsets.Count; i++)
                    {
                        br.BaseStream.Position = arrOffsets[i];

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
            else
            {
                Entries.Add(new TextEntry { Name = "Unsupported", EditedText = "This NAM file is not yet supported." });
            }
        }

        /// <summary>
        /// Write an NAM file to disk.
        /// </summary>
        /// <param name="output">A writable stream of an NAM file.</param>
        public void Save(Stream output)
        {
            using (var bw = new BinaryWriterX(output))
            {
                var pointerGroupCount = Entries.Count / 6;
                var textStartOffset = pointerGroupCount * 14 + 4;

                var pointerList = new List<short>();
                var strings = new List<(string Text, short Pointer)>();

                bw.BaseStream.Position = textStartOffset;

                foreach (var entry in Entries)
                {
                    if (entry.EditedText != string.Empty)
                    {
                        var offset = (short)bw.BaseStream.Position;

                        if (strings.All(s => s.Text != entry.EditedText))
                        {
                            strings.Add((entry.EditedText, offset));
                            pointerList.Add(offset);
                            bw.Write(Encoding.Unicode.GetBytes(entry.EditedText));
                            bw.Write((byte)0x0);
                            bw.Write((byte)0x0);
                        }
                        else
                            pointerList.Add(strings.First(s => s.Text == entry.EditedText).Pointer);
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
                    if (i != 0 && i % 6 == 0)
                        bw.Write((short)0);
                    bw.Write(pointerList[i]);
                }
            }
        }
    }
}
