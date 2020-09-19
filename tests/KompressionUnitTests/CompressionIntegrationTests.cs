//using System.IO;
//using FluentAssertions;
//using Kompression.Huffman;
//using Kompression.Implementations;
//using Kompression.PatternMatch.MatchFinders;
//using Kompression.PatternMatch.MatchParser;
//using NUnit.Framework;

//namespace KompressionUnitTests
//{
//    [TestFixture]
//    public class CompressionIntegrationTests
//    {
//        private byte[] _data;

//        [SetUp]
//        public void Setup()
//        {
//            _data = new byte[]
//            {
//                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
//                0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01,
//                0x02, 0x02, 0x02, 0x02, 0x02, 0x02, 0x02, 0x02,
//                0x03, 0x03, 0x03, 0x03, 0x03, 0x03, 0x03, 0x03,
//            };
//        }

//        [Test]
//        public void Lz10_CompressionDecompression_Works()
//        {
//            // Arrange
//            var dataStream = new MemoryStream(_data);
//            var compressedStream = new MemoryStream();

//            var lz10Configuration = Compressions.Lz10;
//            lz10Configuration.WithMatchOptions(configure => configure.
//                FindMatchesWith((limits, options) => new HybridSuffixTreeMatchFinder(limits[0], options)).
//                ParseMatchesWith((finders, calculator, options) => new ForwardBackwardOptimalParser(options, calculator, finders)));
//            var compressor = lz10Configuration.Build();

//            // Act
//            compressor.Compress(dataStream, compressedStream);
//            dataStream.Position = compressedStream.Position = 0;
//            compressor.Decompress(compressedStream, dataStream);

//            // Assert
//            dataStream.ToArray().Should().BeEquivalentTo(_data);
//        }

//        [Test]
//        public void Lz11_CompressionDecompression_Works()
//        {
//            // Arrange
//            var dataStream = new MemoryStream(_data);
//            var compressedStream = new MemoryStream();

//            var lz11Configuration = Compressions.Lz11;
//            lz11Configuration.WithMatchOptions(configure => configure.
//                FindMatchesWith((limits, options) => new HybridSuffixTreeMatchFinder(limits[0], options)).
//                ParseMatchesWith((finders, calculator, options) => new ForwardBackwardOptimalParser(options, calculator, finders)));
//            var compressor = lz11Configuration.Build();

//            // Act
//            compressor.Compress(dataStream, compressedStream);
//            dataStream.Position = compressedStream.Position = 0;
//            compressor.Decompress(compressedStream, dataStream);

//            // Assert
//            dataStream.ToArray().Should().BeEquivalentTo(_data);
//        }

//        [Test]
//        public void Lz40_CompressionDecompression_Works()
//        {
//            // Arrange
//            var dataStream = new MemoryStream(_data);
//            var compressedStream = new MemoryStream();

//            var lz40Configuration = Compressions.Lz40;
//            lz40Configuration.WithMatchOptions(configure => configure.
//                FindMatchesWith((limits, options) => new HybridSuffixTreeMatchFinder(limits[0], options)).
//                ParseMatchesWith((finders, calculator, options) => new ForwardBackwardOptimalParser(options, calculator, finders)));
//            var compressor = lz40Configuration.Build();

//            // Act
//            compressor.Compress(dataStream, compressedStream);
//            dataStream.Position = compressedStream.Position = 0;
//            compressor.Decompress(compressedStream, dataStream);

//            // Assert
//            dataStream.ToArray().Should().BeEquivalentTo(_data);
//        }

//        [Test]
//        public void Lz60_CompressionDecompression_Works()
//        {
//            // Arrange
//            var dataStream = new MemoryStream(_data);
//            var compressedStream = new MemoryStream();

//            var lz60Configuration = Compressions.Lz60;
//            lz60Configuration.WithMatchOptions(configure => configure.
//                FindMatchesWith((limits, options) => new HybridSuffixTreeMatchFinder(limits[0], options)).
//                ParseMatchesWith((finders, calculator, options) => new ForwardBackwardOptimalParser(options, calculator, finders)));
//            var compressor = lz60Configuration.Build();

//            // Act
//            compressor.Compress(dataStream, compressedStream);
//            dataStream.Position = compressedStream.Position = 0;
//            compressor.Decompress(compressedStream, dataStream);

//            // Assert
//            dataStream.ToArray().Should().BeEquivalentTo(_data);
//        }

//        [Test]
//        public void Lz77_CompressionDecompression_Works()
//        {
//            // Arrange
//            var dataStream = new MemoryStream(_data);
//            var compressedStream = new MemoryStream();

//            var lz77Configuration = Compressions.Lz77;
//            lz77Configuration.WithMatchOptions(configure => configure.
//                FindMatchesWith((limits, options) => new HybridSuffixTreeMatchFinder(limits[0], options)).
//                ParseMatchesWith((finders, calculator, options) => new ForwardBackwardOptimalParser(options, calculator, finders)));
//            var compressor = lz77Configuration.Build();

//            // Act
//            compressor.Compress(dataStream, compressedStream);
//            dataStream.Position = compressedStream.Position = 0;
//            compressor.Decompress(compressedStream, dataStream);

//            // Assert
//            dataStream.ToArray().Should().BeEquivalentTo(_data);
//        }

//        [Test]
//        public void BackwardLz77_CompressionDecompression_Works()
//        {
//            // Arrange
//            var dataStream = new MemoryStream(_data);
//            var compressedStream = new MemoryStream();

//            var backwardLz77Configuration = Compressions.BackwardLz77;
//            backwardLz77Configuration.WithMatchOptions(configure => configure.
//                FindMatchesWith((limits, options) => new HybridSuffixTreeMatchFinder(limits[0], options)).
//                ParseMatchesWith((finders, calculator, options) => new ForwardBackwardOptimalParser(options, calculator, finders)));
//            var compressor = backwardLz77Configuration.Build();

//            // Act
//            compressor.Compress(dataStream, compressedStream);
//            dataStream.Position = compressedStream.Position = 0;
//            compressor.Decompress(compressedStream, dataStream);

//            // Assert
//            dataStream.ToArray().Should().BeEquivalentTo(_data);
//        }

//        [Test]
//        public void LzEcd_CompressionDecompression_Works()
//        {
//            // Arrange
//            var dataStream = new MemoryStream(_data);
//            var compressedStream = new MemoryStream();

//            var lzEcdConfiguration = Compressions.LzEcd;
//            lzEcdConfiguration.WithMatchOptions(configure => configure.
//                FindMatchesWith((limits, options) => new HybridSuffixTreeMatchFinder(limits[0], options)).
//                ParseMatchesWith((finders, calculator, options) => new ForwardBackwardOptimalParser(options, calculator, finders)));
//            var compressor = lzEcdConfiguration.Build();

//            // Act
//            compressor.Compress(dataStream, compressedStream);
//            dataStream.Position = compressedStream.Position = 0;
//            compressor.Decompress(compressedStream, dataStream);

//            // Assert
//            dataStream.ToArray().Should().BeEquivalentTo(_data);
//        }

//        [Test]
//        public void Lze_CompressionDecompression_Works()
//        {
//            // Arrange
//            var dataStream = new MemoryStream(_data);
//            var compressedStream = new MemoryStream();

//            var lzeConfiguration = Compressions.Lze;
//            lzeConfiguration.WithMatchOptions(configure => configure.
//                FindMatchesWith((limits, options) => new HybridSuffixTreeMatchFinder(limits[0], options)).
//                FindMatchesWith((limits, options) => new HybridSuffixTreeMatchFinder(limits[1], options)).
//                ParseMatchesWith((finders, calculator, options) => new ForwardBackwardOptimalParser(options, calculator, finders)));
//            var compressor = lzeConfiguration.Build();

//            // Act
//            compressor.Compress(dataStream, compressedStream);
//            dataStream.Position = compressedStream.Position = 0;
//            compressor.Decompress(compressedStream, dataStream);

//            // Assert
//            dataStream.ToArray().Should().BeEquivalentTo(_data);
//        }

//        [Test]
//        public void Lzss_CompressionDecompression_Works()
//        {
//            // Arrange
//            var dataStream = new MemoryStream(_data);
//            var compressedStream = new MemoryStream();

//            var lzssConfiguration = Compressions.Lzss;
//            lzssConfiguration.WithMatchOptions(configure => configure.
//                FindMatchesWith((limits, options) => new HybridSuffixTreeMatchFinder(limits[0], options)).
//                ParseMatchesWith((finders, calculator, options) => new ForwardBackwardOptimalParser(options, calculator, finders)));
//            var compressor = lzssConfiguration.Build();

//            // Act
//            compressor.Compress(dataStream, compressedStream);
//            dataStream.Position = compressedStream.Position = 0;
//            compressor.Decompress(compressedStream, dataStream);

//            // Assert
//            dataStream.ToArray().Should().BeEquivalentTo(_data);
//        }

//        [Test]
//        public void LzssVle_CompressionDecompression_Works()
//        {
//            // Arrange
//            var dataStream = new MemoryStream(_data);
//            var compressedStream = new MemoryStream();

//            var lzssVleConfiguration = Compressions.LzssVlc;
//            lzssVleConfiguration.WithMatchOptions(configure => configure.
//                FindMatchesWith((limits, options) => new HybridSuffixTreeMatchFinder(limits[0], options)).
//                ParseMatchesWith((finders, calculator, options) => new ForwardBackwardOptimalParser(options, calculator, finders)));
//            var compressor = lzssVleConfiguration.Build();

//            // Act
//            compressor.Compress(dataStream, compressedStream);
//            dataStream.Position = compressedStream.Position = 0;
//            compressor.Decompress(compressedStream, dataStream);

//            // Assert
//            dataStream.ToArray().Should().BeEquivalentTo(_data);
//        }

//        [Test]
//        public void Mio0Le_CompressionDecompression_Works()
//        {
//            // Arrange
//            var dataStream = new MemoryStream(_data);
//            var compressedStream = new MemoryStream();

//            var mio0Configuration = Compressions.Mio0Le;
//            mio0Configuration.WithMatchOptions(configure => configure.
//                FindMatchesWith((limits, options) => new HybridSuffixTreeMatchFinder(limits[0], options)).
//                ParseMatchesWith((finders, calculator, options) => new ForwardBackwardOptimalParser(options, calculator, finders)));
//            var compressor = mio0Configuration.Build();

//            // Act
//            compressor.Compress(dataStream, compressedStream);
//            dataStream.Position = compressedStream.Position = 0;
//            compressor.Decompress(compressedStream, dataStream);

//            // Assert
//            dataStream.ToArray().Should().BeEquivalentTo(_data);
//        }

//        [Test]
//        public void Mio0Be_CompressionDecompression_Works()
//        {
//            // Arrange
//            var dataStream = new MemoryStream(_data);
//            var compressedStream = new MemoryStream();

//            var mio0Configuration = Compressions.Mio0Be;
//            mio0Configuration.WithMatchOptions(configure => configure.
//                FindMatchesWith((limits, options) => new HybridSuffixTreeMatchFinder(limits[0], options)).
//                ParseMatchesWith((finders, calculator, options) => new ForwardBackwardOptimalParser(options, calculator, finders)));
//            var compressor = mio0Configuration.Build();

//            // Act
//            compressor.Compress(dataStream, compressedStream);
//            dataStream.Position = compressedStream.Position = 0;
//            compressor.Decompress(compressedStream, dataStream);

//            // Assert
//            dataStream.ToArray().Should().BeEquivalentTo(_data);
//        }

//        [Test]
//        public void Yay0Le_CompressionDecompression_Works()
//        {
//            // Arrange
//            var dataStream = new MemoryStream(_data);
//            var compressedStream = new MemoryStream();

//            var yay0Configuration = Compressions.Yay0Le;
//            yay0Configuration.WithMatchOptions(configure => configure.
//                FindMatchesWith((limits, options) => new HybridSuffixTreeMatchFinder(limits[0], options)).
//                ParseMatchesWith((finders, calculator, options) => new ForwardBackwardOptimalParser(options, calculator, finders)));
//            var compressor = yay0Configuration.Build();

//            // Act
//            compressor.Compress(dataStream, compressedStream);
//            dataStream.Position = compressedStream.Position = 0;
//            compressor.Decompress(compressedStream, dataStream);

//            // Assert
//            dataStream.ToArray().Should().BeEquivalentTo(_data);
//        }

//        [Test]
//        public void Yay0Be_CompressionDecompression_Works()
//        {
//            // Arrange
//            var dataStream = new MemoryStream(_data);
//            var compressedStream = new MemoryStream();

//            var yay0Configuration = Compressions.Yay0Be;
//            yay0Configuration.WithMatchOptions(configure => configure.
//                FindMatchesWith((limits, options) => new HybridSuffixTreeMatchFinder(limits[0], options)).
//                ParseMatchesWith((finders, calculator, options) => new ForwardBackwardOptimalParser(options, calculator, finders)));
//            var compressor = yay0Configuration.Build();

//            // Act
//            compressor.Compress(dataStream, compressedStream);
//            dataStream.Position = compressedStream.Position = 0;
//            compressor.Decompress(compressedStream, dataStream);

//            // Assert
//            dataStream.ToArray().Should().BeEquivalentTo(_data);
//        }

//        [Test]
//        public void Yaz0Le_CompressionDecompression_Works()
//        {
//            // Arrange
//            var dataStream = new MemoryStream(_data);
//            var compressedStream = new MemoryStream();

//            var yaz0Configuration = Compressions.Yaz0Le;
//            yaz0Configuration.WithMatchOptions(configure => configure.
//                FindMatchesWith((limits, options) => new HybridSuffixTreeMatchFinder(limits[0], options)).
//                ParseMatchesWith((finders, calculator, options) => new ForwardBackwardOptimalParser(options, calculator, finders)));
//            var compressor = yaz0Configuration.Build();

//            // Act
//            compressor.Compress(dataStream, compressedStream);
//            dataStream.Position = compressedStream.Position = 0;
//            compressor.Decompress(compressedStream, dataStream);

//            // Assert
//            dataStream.ToArray().Should().BeEquivalentTo(_data);
//        }

//        [Test]
//        public void Yaz0Be_CompressionDecompression_Works()
//        {
//            // Arrange
//            var dataStream = new MemoryStream(_data);
//            var compressedStream = new MemoryStream();

//            var yaz0Configuration = Compressions.Yaz0Be;
//            yaz0Configuration.WithMatchOptions(configure => configure.
//                FindMatchesWith((limits, options) => new HybridSuffixTreeMatchFinder(limits[0], options)).
//                ParseMatchesWith((finders, calculator, options) => new ForwardBackwardOptimalParser(options, calculator, finders)));
//            var compressor = yaz0Configuration.Build();

//            // Act
//            compressor.Compress(dataStream, compressedStream);
//            dataStream.Position = compressedStream.Position = 0;
//            compressor.Decompress(compressedStream, dataStream);

//            // Assert
//            dataStream.ToArray().Should().BeEquivalentTo(_data);
//        }

//        [Test]
//        public void TaikoLz80_CompressionDecompression_Works()
//        {
//            // Arrange
//            var dataStream = new MemoryStream(_data);
//            var compressedStream = new MemoryStream();

//            var taikoConfiguration = Compressions.TaikoLz80;
//            taikoConfiguration.WithMatchOptions(configure => configure.
//                FindMatchesWith((limits, options) => new HybridSuffixTreeMatchFinder(limits[0], options)).
//                FindMatchesWith((limits, options) => new HybridSuffixTreeMatchFinder(limits[1], options)).
//                FindMatchesWith((limits, options) => new HybridSuffixTreeMatchFinder(limits[2], options)).
//                ParseMatchesWith((finders, calculator, options) => new ForwardBackwardOptimalParser(options, calculator, finders)));
//            var compressor = taikoConfiguration.Build();

//            // Act
//            compressor.Compress(dataStream, compressedStream);
//            dataStream.Position = compressedStream.Position = 0;
//            compressor.Decompress(compressedStream, dataStream);

//            // Assert
//            dataStream.ToArray().Should().BeEquivalentTo(_data);
//        }

//        [Test]
//        public void TaikoLz81_CompressionDecompression_Works()
//        {
//            // Arrange
//            var dataStream = new MemoryStream(_data);
//            var compressedStream = new MemoryStream();

//            var taikoConfiguration = Compressions.TaikoLz81;
//            taikoConfiguration.WithMatchOptions(configure => configure.
//                FindMatchesWith((limits, options) => new HybridSuffixTreeMatchFinder(limits[0], options)).
//                ParseMatchesWith((finders, calculator, options) => new ForwardBackwardOptimalParser(options, calculator, finders))).
//                WithHuffmanOptions(configure => configure.BuildTreeWith(() => new HuffmanTreeBuilder()));
//            var compressor = taikoConfiguration.Build();

//            // Act
//            compressor.Compress(dataStream, compressedStream);
//            dataStream.Position = compressedStream.Position = 0;
//            compressor.Decompress(compressedStream, dataStream);

//            // Assert
//            dataStream.ToArray().Should().BeEquivalentTo(_data);
//        }

//        [Test]
//        public void Wp16_CompressionDecompression_Works()
//        {
//            // Arrange
//            var dataStream = new MemoryStream(_data);
//            var compressedStream = new MemoryStream();

//            var wpConfiguration = Compressions.Wp16;
//            wpConfiguration.WithMatchOptions(configure => configure.
//                    FindMatchesWith((limits, options) => new HybridSuffixTreeMatchFinder(limits[0], options)).
//                    ParseMatchesWith((finders, calculator, options) => new ForwardBackwardOptimalParser(options, calculator, finders)));
//            var compressor = wpConfiguration.Build();

//            // Act
//            compressor.Compress(dataStream, compressedStream);
//            dataStream.Position = compressedStream.Position = 0;
//            compressor.Decompress(compressedStream, dataStream);

//            // Assert
//            dataStream.ToArray().Should().BeEquivalentTo(_data);
//        }

//        [Test]
//        public void TalesOf01_CompressionDecompression_Works()
//        {
//            // Arrange
//            var dataStream = new MemoryStream(_data);
//            var compressedStream = new MemoryStream();

//            var talesConfiguration = Compressions.TalesOf01;
//            talesConfiguration.WithMatchOptions(configure => configure.
//                FindMatchesWith((limits, options) => new HybridSuffixTreeMatchFinder(limits[0], options)).
//                ParseMatchesWith((finders, calculator, options) => new ForwardBackwardOptimalParser(options, calculator, finders)));
//            var compressor = talesConfiguration.Build();

//            // Act
//            compressor.Compress(dataStream, compressedStream);
//            dataStream.Position = compressedStream.Position = 0;
//            compressor.Decompress(compressedStream, dataStream);

//            // Assert
//            dataStream.ToArray().Should().BeEquivalentTo(_data);
//        }
//    }
//}
