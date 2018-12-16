using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

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
    }
}
