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
        private string _testFile = @"..\..\TestFiles\file.test";
        private string _metaFile = @"..\..\TestFiles\meta.bin";

        private void RestoreTestFile()
        {
            if (File.Exists(_testFile))
                File.Delete(_testFile);
            if (File.Exists(_metaFile))
                File.Delete(_metaFile);

            var file = File.Create(_testFile);
            new BinaryWriter(file).Write(new byte[] { 0x16, 0x16, 0x16, 0x16 });
            file.Close();

            file = File.Create(_metaFile);
            new BinaryWriter(file).Write(new byte[] { 0x11, 0x22, 0x33, 0x44 });
            file.Close();
        }

        [TestMethod]
        public void LoadSaveClose()
        {
            var kore = new KoreManager(".");

            var kfi = kore.LoadFile(new KoreLoadInfo(File.Open(_testFile, FileMode.Open), _testFile) { FileSystem = new PhysicalFileSystem(Path.GetFullPath(@"..\..\TestFiles\")) });
            Assert.IsNotNull(kfi);
            Assert.IsTrue(kfi.Adapter is ITest);
            Assert.IsTrue((kfi.Adapter as ITest).Communication.Contains("string1"));
            Assert.IsTrue((kfi.Adapter as ITest).Communication.Contains("string2"));

            var savedKfi = kore.SaveFile(new KoreSaveInfo(kfi, @"..\..\temp\"));
            Assert.IsFalse((savedKfi.Adapter as ITest).Communication.Contains("string3"));

            var closeResult = kore.CloseFile(savedKfi);

            Assert.IsTrue(closeResult);
            Assert.IsFalse(kore.OpenFiles.Contains(kfi));
            Assert.IsFalse(kore.OpenFiles.Contains(savedKfi));
            Assert.AreEqual(6, new FileInfo(_testFile).Length);
            Assert.AreEqual(6, new FileInfo(_metaFile).Length);

            var t = File.OpenRead(_testFile);
            var m = File.OpenRead(_metaFile);
            Assert.IsTrue(new BinaryReader(t).ReadBytes(6).SequenceEqual(new byte[] { 0x16, 0x16, 0x16, 0x16, 0x32, 0x32 }));
            Assert.IsTrue(new BinaryReader(m).ReadBytes(6).SequenceEqual(new byte[] { 0x11, 0x22, 0x33, 0x44, 0x55, 0x66 }));
            t.Close();
            m.Close();

            RestoreTestFile();
        }

        [TestMethod]
        public void SaveAsOtherDirectory()
        {
            var kore = new KoreManager(".");

            var kfi = kore.LoadFile(new KoreLoadInfo(File.Open(_testFile, FileMode.Open), _testFile) { FileSystem = new PhysicalFileSystem(Path.GetFullPath(@"..\..\TestFiles\")) });
            Assert.IsNotNull(kfi);
            Assert.IsTrue(kfi.Adapter is ITest);
            Assert.IsTrue((kfi.Adapter as ITest).Communication.Contains("string1"));
            Assert.IsTrue((kfi.Adapter as ITest).Communication.Contains("string2"));

            var savedKfi = kore.SaveFile(new KoreSaveInfo(kfi, @"..\..\temp\") { NewSaveFile = @"..\..\TestFiles\SaveAsLocation\newmain.test" });
            Assert.IsFalse((savedKfi.Adapter as ITest).Communication.Contains("string3"));

            var closeResult = kore.CloseFile(savedKfi);

            Assert.IsTrue(closeResult);
            Assert.IsFalse(kore.OpenFiles.Contains(kfi));
            Assert.IsFalse(kore.OpenFiles.Contains(savedKfi));
            Assert.AreEqual(4, new FileInfo(_testFile).Length);
            Assert.AreEqual(4, new FileInfo(_metaFile).Length);
        }

        [TestMethod]
        public void SaveAsSameDirectory()
        {
            var kore = new KoreManager(".");

            var kfi = kore.LoadFile(new KoreLoadInfo(File.Open(_testFile, FileMode.Open), _testFile) { FileSystem = new PhysicalFileSystem(Path.GetFullPath(@"..\..\TestFiles\")) });
            Assert.IsNotNull(kfi);
            Assert.IsTrue(kfi.Adapter is ITest);
            Assert.IsTrue((kfi.Adapter as ITest).Communication.Contains("string1"));
            Assert.IsTrue((kfi.Adapter as ITest).Communication.Contains("string2"));

            var savedKfi = kore.SaveFile(new KoreSaveInfo(kfi, @"..\..\temp\") { NewSaveFile = _testFile });
            Assert.IsFalse((savedKfi.Adapter as ITest).Communication.Contains("string3"));

            var closeResult = kore.CloseFile(savedKfi);

            Assert.IsTrue(closeResult);
            Assert.IsFalse(kore.OpenFiles.Contains(kfi));
            Assert.IsFalse(kore.OpenFiles.Contains(savedKfi));
            Assert.AreEqual(6, new FileInfo(_testFile).Length);
            Assert.AreEqual(6, new FileInfo(_metaFile).Length);

            var t = File.OpenRead(_testFile);
            var m = File.OpenRead(_metaFile);
            Assert.IsTrue(new BinaryReader(t).ReadBytes(6).SequenceEqual(new byte[] { 0x16, 0x16, 0x16, 0x16, 0x32, 0x32 }));
            Assert.IsTrue(new BinaryReader(m).ReadBytes(6).SequenceEqual(new byte[] { 0x11, 0x22, 0x33, 0x44, 0x55, 0x66 }));
            t.Close();
            m.Close();

            RestoreTestFile();
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
