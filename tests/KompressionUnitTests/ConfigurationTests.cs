using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Kompression.Configuration;
using Kompression.Huffman;
using Kompression.Huffman.Support;
using Kompression.PatternMatch;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace KompressionUnitTests
{
    [TestClass]
    public class ConfigurationTests
    {
        [TestMethod]
        public void Configuration_Build_Successful()
        {
            var decoder = new Moq.Mock<IDecoder>();
            var priceCal = new Mock<IPriceCalculator>();
            var matchfinder = new Mock<IMatchFinder>();
            var huffmanTree = new Mock<IHuffmanTreeBuilder>();

            var config = new KompressionConfiguration();

            // Set general factories
            config.DecodeWith(modes => decoder.Object).WithCompressionModes(1, 2, 3);

            // Set match options
            config.WithMatchOptions(options =>
                options.CalculatePricesWith(() => priceCal.Object).WithMatchFinderOptions(opt2 =>
                    opt2.FindInBackwardOrder().FindMatchesWith(() => matchfinder.Object)
                        .FindMatchesWith(() => matchfinder.Object)));

            // Set huffman options
            config.WithHuffmanOptions(options =>
                options.BuildTreeWith(() => huffmanTree.Object));

            // Assert no exceptions in building the config
            config.Build();
        }
    }
}
