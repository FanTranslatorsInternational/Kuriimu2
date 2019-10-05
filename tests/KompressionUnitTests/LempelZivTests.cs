using System.Diagnostics;
using System.IO;
using System.Linq;
using Kompression.Huffman;
using Kompression.Implementations;
using Kompression.Implementations.PriceCalculators;
using Kompression.PatternMatch.MatchFinders;
using Kompression.PatternMatch.MatchParser;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace KompressionUnitTests
{
    [TestClass]
    public class LempelZivTests
    {
        [TestMethod]
        public void Stub_Compress()
        {
            var file = @"D:\Users\Kirito\Desktop\masterFile.decomp";
            var str = File.OpenRead(file);
            var save = File.Create(file + ".comp");

            var watch = new Stopwatch();
            watch.Start();

            var config = Compressions.LzssVle.WithMatchOptions(options =>
                options
                    .CalculatePricesWith(() => new LzssVlcPriceCalculator())
                    .FindMatchesWith((limits, findOptions) => new HybridSuffixTreeMatchFinder(limits[0], findOptions))
                    .ParseMatchesWith((finders, calculator, findOptions) =>
                        new ForwardBackwardOptimalParser(findOptions, calculator, finders.ToArray())));
            config.Build().Compress(str, save);

            watch.Stop();

            save.Close();
        }

        [TestMethod]
        public void Stub_Decompress()
        {
            var file = @"D:\Users\Kirito\Desktop\masterFile.decomp.comp";
            var str = File.OpenRead(file);
            var save = File.Create(file + ".decomp");

            var watch = new Stopwatch();
            watch.Start();

            var config = Compressions.LzssVle;
            config.Build().Decompress(str, save);

            watch.Stop();

            save.Close();
        }
    }
}
