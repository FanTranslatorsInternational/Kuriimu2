//using System;
//using System.Collections.Generic;
//using System.IO;
//using System.Linq;
//using System.Text;
//using Komponent.IO;
//using Kontract.Interfaces.Text;

//namespace plugin_metal_max.ARR
//{
//    public sealed class ARR
//    {
//        /// <summary>
//        /// The list of text entries in the file.
//        /// </summary>
//        public List<TextEntry> Entries { get; set; } = new List<TextEntry>();

//        /// <summary>
//        /// Read an ARR file into memory.
//        /// </summary>
//        /// <param name="input">A readable stream of an ARR file.</param>
//        public ARR(Stream input)
//        {
//            using (var br = new BinaryReaderX(input))
//            {
//                var count = br.ReadInt32();

//                var pointers = new List<short>();

//                for (var i = 0; i < count; i++)
//                {
//                    for (var j = 0; j < 7; j++)
//                    {
//                        var p = br.ReadInt16();
//                        if (p == 0) continue;
//                        pointers.Add(p);
//                    }
//                }

//                for (var i = 0; i < pointers.Count; i++)
//                {
//                    br.BaseStream.Position = pointers[i];

//                    short b = 0;
//                    var chars = new List<byte>();
//                    do
//                    {
//                        b = br.ReadInt16();
//                        chars.AddRange(BitConverter.GetBytes(b));
//                    }
//                    while (b != 0);

//                    var str = Encoding.Unicode.GetString(chars.ToArray());

//                    Entries.Add(new TextEntry { Name = (i + 1).ToString(), EditedText = str.TrimEnd('\0') });
//                }
//            }
//        }

//        /// <summary>
//        /// Write an ARR file to disk.
//        /// </summary>
//        /// <param name="output">A writable stream of an ARR file.</param>
//        public void Save(Stream output)
//        {
//            using (var bw = new BinaryWriterX(output))
//            {
//                const int pointerGroupSize = 6;
//                var missingPointers = pointerGroupSize - (Entries.Count % pointerGroupSize == 0 ? pointerGroupSize : Entries.Count % pointerGroupSize);

//                for (var i = 0; i < missingPointers; i++)
//                    Entries.Add(new TextEntry { Name = (Entries.Count + 1).ToString() });

//                var pointerGroupCount = Entries.Count / pointerGroupSize;
//                var textStartOffset = pointerGroupCount * 14 + 4;

//                var pointerList = new List<short>();
//                var strings = new List<(string Text, short Pointer)>();

//                bw.BaseStream.Position = textStartOffset;

//                foreach (var entry in Entries)
//                {
//                    if (entry.EditedText != string.Empty)
//                    {
//                        var offset = (short)bw.BaseStream.Position;

//                        if (strings.All(s => s.Text != entry.EditedText))
//                        {
//                            strings.Add((entry.EditedText, offset));
//                            pointerList.Add(offset);
//                            bw.Write(Encoding.Unicode.GetBytes(entry.EditedText));
//                            bw.Write((byte)0x0);
//                            bw.Write((byte)0x0);
//                        }
//                        else
//                            pointerList.Add(strings.First(s => s.Text == entry.EditedText).Pointer);
//                    }
//                    else
//                        pointerList.Add((short)(textStartOffset - 2));
//                }

//                bw.BaseStream.Position = 0;

//                bw.Write(pointerGroupCount);

//                for (var i = 0; i < pointerList.Count; i++)
//                {
//                    if (i != 0 && i % pointerGroupSize == 0)
//                        bw.Write((short)0);
//                    bw.Write(pointerList[i]);
//                }
//            }
//        }
//    }
//}
