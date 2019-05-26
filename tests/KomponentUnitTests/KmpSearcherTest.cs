using System;
using System.Text;
using Komponent.Tools;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace KomponentUnitTests
{
    [TestClass]
    public class KmpSearcherTest
    {
        [TestMethod]
        public void Search()
        {
            var str = "This is a string to search in.";
            var substr = "is";

            var searcher = new KmpSearcher(Encoding.ASCII.GetBytes(substr));
            var offset = searcher.Search(Encoding.ASCII.GetBytes(str));

            Assert.AreEqual(2, offset);
        }
    }
}
