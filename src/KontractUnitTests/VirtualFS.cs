using System;
using Kontract.Interfaces.VirtualFS;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;
using System.IO;
using Kontract.Interfaces.Archive;
using System.Collections.Generic;
using Kontract.FileSystem;

namespace KontractUnitTests
{
    [TestClass]
    public class VirtualFS
    {
        [TestMethod]
        public void Physical()
        {
            var fs = new PhysicalFileSystem("..\\..\\filesystem");

            FSTest(fs);
        }

        [TestMethod]
        public void Virtual()
        {
            var files = new List<ArchiveFileInfo> {
                new ArchiveFileInfo{ FileName="Class6.cs", FileData=new MemoryStream()},
                new ArchiveFileInfo{ FileName="folder1\\Class1.cs", FileData=new MemoryStream()},
                new ArchiveFileInfo{ FileName="folder2/Class2.cs", FileData=new MemoryStream()},
                new ArchiveFileInfo{ FileName="folder2/Class3.cs", FileData=new MemoryStream()},
                new ArchiveFileInfo{ FileName="folder2\\subfolder1_2/Class4.cs", FileData=new MemoryStream()},
                new ArchiveFileInfo{ FileName="folder3\\subfolder1_3/Class5.cs", FileData=new MemoryStream()}
            };

            var fs = new VirtualFileSystem(files, "../..\\virtualTemp");

            FSTest(fs);

            Assert.ThrowsException<InvalidOperationException>(new Action(() => fs.GetDirectory("..\\..")));
            Assert.ThrowsException<InvalidOperationException>(new Action(() => fs.GetDirectory("folder2\\subfolder1_2\\../../..")));
        }

        private void FSTest(IVirtualFSRoot fs)
        {
            Assert.IsTrue(fs.EnumerateFiles().Contains(Path.Combine(fs.RootDir, "Class6.cs")));

            Assert.IsTrue(fs.EnumerateDirectories().Count() == 3);
            Assert.IsTrue(fs.EnumerateDirectories().Contains(Path.Combine(fs.RootDir, "folder1")));

            var fs2 = fs.GetDirectory("folder2\\subfolder1_2");

            Assert.IsTrue(fs2.EnumerateDirectories().Count() == 0);
            Assert.IsTrue(fs2.EnumerateFiles().Contains(Path.Combine(fs2.RootDir, "Class4.cs")));

            var file = fs2.OpenFile("Class4.cs");

            Assert.IsTrue(file.CanRead);

            file.Close();
        }
    }
}
