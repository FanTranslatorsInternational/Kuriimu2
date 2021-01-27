using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Kore.Utilities.Text.TextSearcher;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace KoreUnitTests
{
    [TestClass]
    class UtilitiesTests
    {
        [TestMethod]
        public void KmpSearcher_Search_Successful()
        {
            var str = "This is a string to search in.";
            var substr = "is";

            var searcher = new KmpTextSearcher(Encoding.ASCII.GetBytes(substr));
            var offset = searcher.SearchAsync(Encoding.ASCII.GetBytes(str)).Result;

            Assert.AreEqual(2, offset);
        }
    }
}
