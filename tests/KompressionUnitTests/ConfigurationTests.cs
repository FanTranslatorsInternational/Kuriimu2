//using System;
//using System.IO;
//using Kompression.Configuration;
//using Kompression.Interfaces;
//using Microsoft.VisualStudio.TestTools.UnitTesting;
//using Moq;

//namespace KompressionUnitTests
//{
//    [TestClass]
//    public class ConfigurationTests
//    {
//        [TestMethod]
//        public void Configuration_Build_Successful()
//        {
//            // Arrange
//            var decoder = new Mock<IDecoder>();
//            decoder.Setup(x => x.Decode(It.IsAny<Stream>(), It.IsAny<Stream>()));
//            var priceCalculator = new Mock<IPriceCalculator>();
//            var matchFinder = new Mock<IMatchFinder>();
//            var huffmanTree = new Mock<IHuffmanTreeBuilder>();
//            var stream = new Mock<Stream>();

//            var config = new KompressionConfiguration();

//            // Set general factories
//            config.DecodeWith(modes => decoder.Object).WithCompressionModes(1, 2, 3);

//            // Set match options
//            config.WithMatchOptions(options =>
//                options.CalculatePricesWith(() => priceCalculator.Object).
//                    FindInBackwardOrder().
//                    FindMatchesWith((limits,findOptions) => matchFinder.Object).
//                    FindMatchesWith((limits,findOptions) => matchFinder.Object));

//            // Set huffman options
//            config.WithHuffmanOptions(options =>
//                options.BuildTreeWith(() => huffmanTree.Object));

//            // Act + Assert
//            var compressor = config.Build();

//            Assert.ThrowsException<InvalidOperationException>(() => compressor.Compress(stream.Object, stream.Object));
//            compressor.Decompress(stream.Object, stream.Object);
//        }
//    }
//}
