using System;
using System.Threading;
using Kontract.Interfaces.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;
using Kontract.Attributes;
using Kontract.Interfaces.FileSystem;
using System.IO;
using Kontract.FileSystem;
using Kore;

namespace KoreUnitTests
{
    [TestClass]
    public class KoreTests
    {
        [TestMethod]
        public void LoadSaveClose()
        {
            var kore = new KoreManager(".");

            var filename = @"..\..\TestFiles\file.test";
            var kfi = kore.LoadFile(new KoreLoadInfo(File.Open(filename, FileMode.Open), filename) { FileSystem = new PhysicalFileSystem(Path.GetFullPath(@"..\..\TestFiles\")) });
            Assert.IsNotNull(kfi);
            Assert.IsTrue(kfi.Adapter is ITest);
            Assert.IsTrue((kfi.Adapter as ITest).Communication.Contains("string1"));
            Assert.IsTrue((kfi.Adapter as ITest).Communication.Contains("string2"));

            kore.SaveFile(new KoreSaveInfo(kfi, @"..\..\temp\"));
            Assert.IsTrue((kfi.Adapter as ITest).Communication.Contains("string3"));

            var closeResult = kore.CloseFile(kfi);

            Assert.IsTrue(closeResult);
            Assert.IsFalse(kore.OpenFiles.Contains(kfi));
        }

        [TestMethod]
        public void GetAdapters()
        {
            var kore = new KoreManager(".");

            var list = kore.GetAdapters<ILoadFiles>().Where(x => x is ITest);
            Assert.IsTrue(list.Any());
        }

        [TestMethod]
        public void FileExtensionsByType()
        {
            var kore = new KoreManager(".");

            var exts = kore.FileExtensionsByType<ILoadFiles>();

            Assert.IsTrue(exts.Contains(".test"));
        }

        [TestMethod]
        public void FileFiltersByType()
        {
            var kore = new KoreManager(".");

            var filters = kore.FileFiltersByType<ILoadFiles>();

            Assert.IsTrue(filters.Contains("TestPlugin (*.test)|*.test"));
        }
    }
}
