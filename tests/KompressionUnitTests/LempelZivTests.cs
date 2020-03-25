//using System;
//using System.Diagnostics;
//using System.IO;
//using Kompression.Implementations;
//using Kompression.PatternMatch.MatchFinders;
//using Kompression.PatternMatch.MatchParser;
//using NUnit.Framework.Internal;
//using NUnit.Framework;

//namespace KompressionUnitTests
//{
//    [TestFixture]
//    public class LempelZivTests
//    {
//        [Test]
//        public void Stub_Compress()
//        {
//            var file = @"D:\Users\Kirito\Desktop\spike_chun_master.decomp";
//            var str = File.OpenRead(file);
//            var save = File.Create(file + ".comp");

//            var watch = new Stopwatch();
//            watch.Start();

//            var config = Compressions.LzssVlc.WithMatchOptions(options =>
//                options
//                    .FindMatchesWith((limits, findOptions) => new HybridSuffixTreeMatchFinder(limits[0], findOptions))
//                    //.FindMatchesWith((limits, findOptions) => new RleMatchFinder(limits[1], findOptions))
//                    .ParseMatchesWith((finders, calculator, findOptions) =>
//                        new ForwardBackwardOptimalParser(findOptions, calculator, finders)));
//            config.Build().Compress(str, save);

//            watch.Stop();

//            save.Close();
//        }

//        [Test]
//        public void Stub_Decompress()
//        {
//            var file = @"D:\Users\Kirito\Desktop\spike_chun_master.decomp.comp";
//            var str = File.OpenRead(file);
//            var save = File.Create(file + ".decomp");

//            var watch = new Stopwatch();
//            watch.Start();

//            var config = Compressions.LzssVlc;
//            try
//            {
//                config.Build().Decompress(str, save);
//            }
//            catch (Exception e)
//            {
//            }

//            watch.Stop();

//            save.Close();
//        }
//    }
//}
