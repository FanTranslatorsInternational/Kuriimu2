using Komponent.Contract.Enums;
using Komponent.IO;
using Kompression.Contract;
using Kompression.Contract.DataClasses.Encoder.Huffman;
using Kompression.Contract.DataClasses.Encoder.LempelZiv;
using Kompression.Contract.DataClasses.Encoder.LempelZiv.MatchFinder;
using Kompression.Contract.DataClasses.Encoder.LempelZiv.MatchParser;
using Kompression.Contract.Encoder.LempelZiv.MatchFinder;
using Kompression.Contract.Enums.Encoder.Huffman;
using Kompression.Contract.Enums.Encoder.LempelZiv;
using Kompression.DataClasses.Configuration;
using Kompression.Encoder.Huffman;
using Kompression.Encoder.LempelZiv.InputManipulation;
using Kompression.Encoder.LempelZiv.MatchFinder;
using Kompression.Encoder.LempelZiv.MatchParser;
using Kompression.Extensions;
using Kompression.InternalContract.SlimeMoriMori.Decoder;
using Kompression.InternalContract.SlimeMoriMori.Deobfuscator;
using Kompression.InternalContract.SlimeMoriMori.Encoder;
using Kompression.InternalContract.SlimeMoriMori.Obfuscator;
using Kompression.InternalContract.SlimeMoriMori.ValueReader;
using Kompression.InternalContract.SlimeMoriMori.ValueWriter;
using Kompression.Specialized.SlimeMoriMori.Decoder;
using Kompression.Specialized.SlimeMoriMori.Deobfuscator;
using Kompression.Specialized.SlimeMoriMori.Encoder;
using Kompression.Specialized.SlimeMoriMori.Obfuscator;
using Kompression.Specialized.SlimeMoriMori.ValueReader;
using Kompression.Specialized.SlimeMoriMori.ValueWriter;

namespace Kompression.Specialized.SlimeMoriMori
{
    /// <summary>
    /// The <see cref="ICompression"/> for the compression in Slime Mori Mori for the GBA.
    /// </summary>
    public class SlimeMoriMoriCompression : ICompression
    {
        private readonly int _huffmanMode;
        private readonly int _compressionMode;
        private readonly int _obfuscationMode;
        private readonly bool _isCompressable;

        public static string[] Names => ["Slime Mori Mori"];

        public SlimeMoriMoriCompression()
        {
            _isCompressable = false;
        }

        public SlimeMoriMoriCompression(int huffmanMode, int compressionMode, int obfuscationMode)
        {
            _huffmanMode = huffmanMode;
            _compressionMode = compressionMode;
            _obfuscationMode = obfuscationMode;
            _isCompressable = true;
        }

        public void Decompress(Stream input, Stream output)
        {
            var originalInputPosition = input.Position;
            if (input.ReadByte() != 0x70)
                return;

            input.Position = 7;
            var identByte = input.ReadByte();

            var valueReader = CreateValueReader((identByte >> 3) & 0x3);
            var decoder = CreateDecoder(identByte & 0x7, valueReader);

            input.Position = originalInputPosition;
            decoder.Decode(input, output);

            var deobfuscator = CreateDeobfuscator((identByte >> 5) & 0x7);
            if (deobfuscator != null)
            {
                output.Position = 0;
                deobfuscator.Deobfuscate(output);
            }
        }

        public void Compress(Stream input, Stream output)
        {
            if (!_isCompressable)
                throw new InvalidOperationException("This instance doesn't allow for compression.");

            var inputArray = input.ToArray();

            // Obfuscate the data
            var obfuscator = CreateObfuscator(_obfuscationMode);
            if (obfuscator != null)
            {
                output.Position = 0;
                obfuscator.Obfuscate(inputArray);
                output.Position = 0;
            }

            // Find all Lz matches
            var matches = FindMatches(new MemoryStream(inputArray), _compressionMode, _huffmanMode);

            // Create huffman tree and value writer based on match filtered values
            var huffmanInput = RemoveMatchesFromInput(inputArray, matches);
            var tree = CreateHuffmanTree(huffmanInput, _huffmanMode);
            ArgumentNullException.ThrowIfNull(tree);
            var valueWriter = CreateValueWriter(_huffmanMode, tree);

            using var bw = new BinaryBitWriter(output, BitOrder.MostSignificantBitFirst, 4, ByteOrder.LittleEndian);

            // Write header data
            bw.WriteBits((int)input.Length, 0x18);
            bw.WriteByte(0x70);

            var identByte = _compressionMode & 0x7;
            identByte |= (_huffmanMode & 0x3) << 3;
            identByte |= (_obfuscationMode & 0x7) << 5;
            bw.WriteByte(identByte);

            // Write huffman tree
            WriteHuffmanTree(bw, tree, _huffmanMode);

            // Encode the input data
            var encoder = CreateEncoder(_compressionMode, valueWriter);
            encoder.Encode(input, bw, matches);

            // Flush the buffer
            bw.Flush();
        }

        private static LempelZivMatch[] FindMatches(Stream input, int compressionMode, int huffmanMode)
        {
            ILempelZivMatchFinder[] matchFinders;
            switch (compressionMode)
            {
                case 1:
                    matchFinders =
                    [
                        new HistoryMatchFinder(new LempelZivMatchFinderOptions
                        {
                            Limitations = new LempelZivMatchLimitations
                            {
                                MinLength = 3,
                                MaxLength = 18,
                                MinDisplacement = 1,
                                MaxDisplacement = 0xFFFF
                            },
                            UnitSize = UnitSize.Byte
                        })
                    ];
                    break;

                case 2:
                    matchFinders =
                    [
                        new HistoryMatchFinder(new LempelZivMatchFinderOptions
                        {
                            Limitations = new LempelZivMatchLimitations
                            {
                                MinLength = 3,
                                MaxLength = -1,
                                MinDisplacement = 1,
                                MaxDisplacement = 0xFFFF
                            },
                            UnitSize = UnitSize.Byte
                        })
                    ];
                    break;

                case 3:
                    matchFinders =
                    [
                        new HistoryMatchFinder(new LempelZivMatchFinderOptions
                        {
                            Limitations = new LempelZivMatchLimitations
                            {
                                MinLength = 4,
                                MaxLength = -1,
                                MinDisplacement = 2,
                                MaxDisplacement = 0xFFFF
                            },
                            UnitSize = UnitSize.Byte
                        })
                    ];
                    break;

                case 4:
                    return [];

                case 5:
                    matchFinders =
                    [
                        new HistoryMatchFinder(new LempelZivMatchFinderOptions
                        {
                            Limitations = new LempelZivMatchLimitations
                            {
                                MinLength = 3,
                                MaxLength = 0x42,
                                MinDisplacement = 1,
                                MaxDisplacement = 0xFFFF
                            },
                            UnitSize = UnitSize.Byte
                        }),
                        new RleMatchFinder(new LempelZivMatchFinderOptions
                        {
                            Limitations = new LempelZivMatchLimitations
                            {
                                MinLength = 1,
                                MaxLength = 0x40
                            },
                            UnitSize = UnitSize.Byte
                        })
                    ];
                    break;

                default:
                    throw new InvalidOperationException($"Unknown compression mode {compressionMode}.");
            }

            // Optimal parse all LZ matches
            var parser = new OptimalLempelZivMatchParser(new LempelZivMatchParserOptions
            {
                InputManipulation = new InputManipulator(new LempelZivInputAdjustmentOptions()),
                PriceCalculator = new SlimePriceCalculator(compressionMode, huffmanMode),
                MatchFinders = matchFinders,
                UnitSize = compressionMode == 3 ? UnitSize.Short : UnitSize.Byte,
                TaskCount = 8
            });

            return [.. parser.ParseMatches(input)];
        }

        private static void WriteHuffmanTree(BinaryBitWriter bw, HuffmanTreeNode rootNode, int huffmanMode)
        {
            int bitDepth;
            switch (huffmanMode)
            {
                case 1:
                    bitDepth = 4;
                    break;
                case 2:
                    bitDepth = 8;
                    break;
                default:
                    return;
            }

            var depthList = rootNode.Children;
            if (depthList == null)
                return;

            for (var i = 0; i < 16; i++)
            {
                var valuesWithBitCount = depthList.Count(x => x.IsLeaf);
                bw.WriteByte(valuesWithBitCount);

                foreach (var value in depthList.Where(x => x.IsLeaf).Select(x => x.Code))
                    bw.WriteBits(value, bitDepth);

                depthList = [.. depthList.Where(x => !x.IsLeaf).SelectMany(x => x.Children ?? [])];
            }
        }

        private static byte[] RemoveMatchesFromInput(byte[] input, LempelZivMatch[] matches)
        {
            var huffmanInput = new byte[input.Length - matches.Sum(x => x.Length)];

            var huffmanInputPosition = 0;
            var inputArrayPosition = 0;
            foreach (var match in matches)
            {
                for (var i = inputArrayPosition; i < match.Position; i++)
                    huffmanInput[huffmanInputPosition++] = input[i];

                inputArrayPosition += match.Position - inputArrayPosition;
                inputArrayPosition += match.Length;
            }

            for (var i = inputArrayPosition; i < input.Length; i++)
                huffmanInput[huffmanInputPosition++] = input[i];

            return huffmanInput;
        }

        #region Create methods

        private static IValueReader CreateValueReader(int huffmanMode)
        {
            return huffmanMode switch
            {
                1 => new HuffmanReader(4),
                2 => new HuffmanReader(8),
                _ => new DefaultValueReader()
            };
        }

        private static ISlimeDecoder CreateDecoder(int decompMode, IValueReader huffmanReader)
        {
            return decompMode switch
            {
                1 => new SlimeMode1Decoder(huffmanReader),
                2 => new SlimeMode2Decoder(huffmanReader),
                3 => new SlimeMode3Decoder(huffmanReader),
                4 => new SlimeMode4Decoder(huffmanReader),
                _ => new SlimeMode5Decoder(huffmanReader)
            };
        }

        private static ISlimeDeobfuscator? CreateDeobfuscator(int deobfuscateMode)
        {
            return deobfuscateMode switch
            {
                1 => new SlimeMode1Deobfuscator(),
                2 => new SlimeMode2Deobfuscator(),
                3 => new SlimeMode3Deobfuscator(),
                4 => new SlimeMode4Deobfuscator(),
                _ => null
            };
        }

        private static HuffmanTreeNode? CreateHuffmanTree(byte[] input, int huffmanMode)
        {
            switch (huffmanMode)
            {
                case 1:
                    var tree = new HuffmanTreeBuilder();
                    var rootNode = tree.Build(input, 4, NibbleOrder.LowNibbleFirst);
                    SortHuffmanTree(rootNode);
                    return rootNode;

                case 2:
                    tree = new HuffmanTreeBuilder();
                    rootNode = tree.Build(input, 8, NibbleOrder.LowNibbleFirst);
                    SortHuffmanTree(rootNode);
                    return rootNode;

                default:
                    return null;
            }
        }

        private static IValueWriter CreateValueWriter(int huffmanMode, HuffmanTreeNode rootNode)
        {
            return huffmanMode switch
            {
                1 or 2 => new HuffmanWriter(rootNode),
                _ => new DefaultValueWriter()
            };
        }

        private static void SortHuffmanTree(HuffmanTreeNode? rootNode)
        {
            if (rootNode == null)
                return;

            var treeDepth = rootNode.GetDepth();

            IList<HuffmanTreeNode> previousDepthList = [rootNode];
            IList<HuffmanTreeNode>? depthList = rootNode.Children;
            if (depthList == null)
                return;

            for (int i = 0; i < treeDepth; i++)
            {
                if (depthList.All(x => !x.IsLeaf))
                {
                    previousDepthList = depthList;
                    depthList = [.. previousDepthList.SelectMany(x => x.Children ?? [])];
                    continue;
                }

                var ordered = depthList.OrderBy(x => !x.IsLeaf).ToList();
                for (var j = 0; j < ordered.Count; j++)
                {
                    previousDepthList[j / 2].Frequency -= previousDepthList[j / 2].Children![j % 2].Frequency;
                    previousDepthList[j / 2].Children![j % 2] = ordered[j];
                    previousDepthList[j / 2].Frequency += ordered[j].Frequency;
                }

                previousDepthList = [.. ordered.Where(x => !x.IsLeaf)];
                depthList = [.. previousDepthList.SelectMany(x => x.Children ?? [])];
            }
        }

        private static ISlimeEncoder CreateEncoder(int compressionMode, IValueWriter valueWriter)
        {
            return compressionMode switch
            {
                1 => new SlimeMode1Encoder(valueWriter),
                2 => new SlimeMode2Encoder(valueWriter),
                3 => new SlimeMode3Encoder(valueWriter),
                4 => new SlimeMode4Encoder(valueWriter),
                _ => new SlimeMode5Encoder(valueWriter)
            };
        }

        private static ISlimeObfuscator? CreateObfuscator(int obfuscationMode)
        {
            return obfuscationMode switch
            {
                1 => new SlimeMode1Obfuscator(),
                2 => new SlimeMode2Obfuscator(),
                3 => new SlimeMode3Obfuscator(),
                4 => new SlimeMode4Obfuscator(),
                _ => null
            };
        }

        #endregion

        #region Dispose

        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }

        #endregion
    }
}
