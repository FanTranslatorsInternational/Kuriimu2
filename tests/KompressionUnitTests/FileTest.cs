using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using NUnit.Framework;

namespace KompressionUnitTests
{
    [TestFixture]
    public class FileTest
    {
        [Test]
        public void Decompress_File_Success()
        {
            var file = @"D:\Users\Kirito\Desktop\f016_000_-_copia.LB";
            var fileStream = File.OpenRead(file);
            var output = File.Create(file + ".out");

            var compression = Kompression.Implementations.Compressions.PsLz.Build();

            compression.Decompress(fileStream, output);

            fileStream.Close();
            output.Close();
        }

        [Test]
        public void Compress_File_Success()
        {
            var file = @"D:\Users\Kirito\Desktop\crown\EVM01.PCK";
            var fileStream = File.OpenRead(file);
            var compressed = File.Create(file + ".comp");
            var decompressed = File.Create(file + ".decomp");

            var compression = Kompression.Implementations.Compressions.Wp16.Build();

            compression.Compress(fileStream, compressed);

            compressed.Position = 0;
            compression.Decompress(compressed, decompressed);

            fileStream.Close();
            compressed.Close();
            decompressed.Close();
        }
    }
}
