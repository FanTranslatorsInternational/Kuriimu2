using System;
using System.Threading;
using Kontract.Interfaces.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace KoreUnitTests
{
    [TestClass]
    public class KoreTests
    {
        [TestMethod]
        public void LoadSaveClose()
        {
            var kore = new Kore.Kore(".");

            var kfi = kore.LoadFile(@"..\..\TestFiles\file.test");
            Assert.IsNotNull(kfi);
            Assert.IsTrue(kfi.Adapter is ITest);
            Assert.IsTrue((kfi.Adapter as ITest).Communication.Contains("string1"));
            Assert.IsTrue((kfi.Adapter as ITest).Communication.Contains("string2"));

            kore.SaveFile(kfi);
            Assert.IsTrue((kfi.Adapter as ITest).Communication.Contains("string3"));

            var closeResult = kore.CloseFile(kfi);

            Assert.IsTrue(closeResult);
            Assert.IsFalse(kore.OpenFiles.Contains(kfi));
        }

        [TestMethod]
        public void GetAdapters()
        {
            var kore = new Kore.Kore(".");

            var list = kore.GetAdapters<ILoadFiles>().Where(x => x is ITest);
            Assert.IsTrue(list.Any());
        }

        [TestMethod]
        public void FileExtensionsByType()
        {
            var kore = new Kore.Kore(".");

            var exts = kore.FileExtensionsByType<ILoadFiles>();

            Assert.IsTrue(exts.Contains(".test"));
        }

        [TestMethod]
        public void FileFiltersByType()
        {
            var kore = new Kore.Kore(".");

            var filters = kore.FileFiltersByType<ILoadFiles>();

            Assert.IsTrue(filters.Contains("TestPlugin (*.test)|*.test"));
        }
    }
}
