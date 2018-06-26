using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Komponent.IO;

namespace Kore.XFont.Compression
{
    public class Huffman
    {
        //Huffman 4bit/8bit
        public static byte[] Decompress(Stream input, int num_bits, long decompressedLength, ByteOrder byteOrder = ByteOrder.LittleEndian)
        {
            using (var br = new BinaryReaderX(input, true))
            {
                var result = new List<byte>();

                var tree_size = br.ReadByte();
                var tree_root = br.ReadByte();
                var tree_buffer = br.ReadBytes(tree_size * 2);

                for (int i = 0, code = 0, next = 0, pos = tree_root; ; i++)
                {
                    if (i % 32 == 0)
                    {
                        code = br.ReadInt32();
                    }
                    next += (pos & 0x3F) * 2 + 2;
                    int direction = (code >> (31 - i)) % 2 == 0 ? 2 : 1;
                    var leaf = (pos >> 5 >> direction) % 2 != 0;

                    pos = tree_buffer[next - direction];
                    if (leaf)
                    {
                        result.Add((byte)pos);
                        pos = tree_root;
                        next = 0;
                    }

                    if (result.Count == decompressedLength * 8 / num_bits)
                    {
                        if (num_bits == 8)
                            return result.ToArray();
                        else
                        {
                            if (byteOrder == ByteOrder.LittleEndian)
                            {
                                return Enumerable.Range(0, (int)decompressedLength).Select(j => (byte)(result[2 * j + 1] * 16 + result[2 * j])).ToArray();
                            }
                            else
                            {
                                return Enumerable.Range(0, (int)decompressedLength).Select(j => (byte)(result[2 * j] * 16 + result[2 * j + 1])).ToArray();
                            }
                        }
                    }
                }
            }
        }

        public static byte[] Compress(Stream input, int num_bits, ByteOrder byteOrder = ByteOrder.LittleEndian)
        {
            if (num_bits != 8 && num_bits != 4)
                throw new ArgumentException($"{num_bits} Bits aren't supported!", nameof(num_bits));

            var inData = new byte[input.Length];
            input.Read(inData, 0, (int)input.Length);

            // swap the nibble order depending on compression needs
            //        - Nintendo (0x24 header) order -> most significant nibble first
            //        - Level5 (3 least significant bits == 2) order -> least significant nibble first
            if (num_bits == 4)
                if (byteOrder == ByteOrder.LittleEndian)
                    inData = inData.SelectMany(b => new[] { (byte)(b % 16), (byte)(b / 16) }).ToArray();
                else
                    inData = inData.SelectMany(b => new[] { (byte)(b / 16), (byte)(b % 16) }).ToArray();

            // Get frequencies
            var freq = inData.GroupBy(b => b).Select(g => new Node { freqCount = g.Count(), code = g.Key }).ToList();

            // Add a stub entry in the special case that there's only one item
            if (freq.Count == 1) freq.Add(new Node { code = (byte)(inData[0] + 1) });

            //Sort and create the tree
            while (freq.Count() > 1)
            {
                freq = freq.OrderBy(n => n.freqCount).ToList();
                freq = freq.Skip(2).Concat(new[] { new Node {
                    children = freq.Take(2).ToArray(),
                    freqCount = freq[0].freqCount + freq[1].freqCount
                } }).ToList();
            }

            // Label nodes to keep bandwidth small
            var lst = new List<Node>();
            while (freq.Any())
            {
                var node = freq.Select((p, i) => new { p, Score = p.code - i }).OrderBy(p => p.Score).First().p;
                freq.Remove(node);
                node.code = (byte)(lst.Count - node.code);
                lst.Add(node);
                foreach (var child in node.children.Reverse().Where(child => child.children != null))
                {
                    child.code = (byte)lst.Count;
                    freq.Add(child);
                }
            }

            // Convert our list of nodes to a dictionary of bytes -> huffman codes
            var codes = lst[0].GetHuffCodes("").ToDictionary(p => p.Item1, p => p.Item2);

            // Write header + tree
            using (var bw = new BinaryWriterX(new MemoryStream()))
            {
                // Write header
                bw.Write((byte)lst.Count);

                // Write Huffman tree
                foreach (var node in lst.Take(1).Concat(lst.SelectMany(node => node.children)))
                {
                    if (node.children != null)
                        node.code |= (byte)node.children.Select((child, i) => child.children == null ? (byte)(0x80 >> i) : 0).Sum();
                    bw.Write(node.code);
                }

                // Write bits to stream
                int data = 0, setbits = 0;
                foreach (var bit in inData.SelectMany(b => codes[b]))
                {
                    data = data * 2 + bit - '0';
                    if (++setbits % 32 == 0) bw.Write(data);
                }
                if (setbits % 32 != 0) bw.Write(data << -setbits);

                return ((MemoryStream)bw.BaseStream).ToArray();
            }
        }

        public class Node
        {
            public Node[] children;
            public int freqCount;
            public byte code;

            public IEnumerable<Tuple<byte, string>> GetHuffCodes(string seed) =>
                children?.SelectMany((child, i) => child.GetHuffCodes(seed + i)) ?? new[] { Tuple.Create(code, seed) };
        }
    }
}
