using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Kontract.FileSystem;
using Kontract.FileSystem.Exceptions;
using Kontract.FileSystem.Nodes.Abstract;
using Kontract.FileSystem.Nodes.Afi;
using Kontract.Interfaces.Archive;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace KontractUnitTests
{
    [TestClass]
    public class FsTests
    {
        [TestMethod]
        public void PhysicalStructureTests()
        {
            var dir = NodeFactory.FromDirectory("..\\..\\filesystem");

            // Are all childs correctly assigned in the tree
            Assert.AreEqual(4, dir.Children.Count);
            Assert.IsInstanceOfType(dir.Children[0], typeof(BaseReadOnlyDirectoryNode));
            Assert.IsInstanceOfType(dir.Children[1], typeof(BaseReadOnlyDirectoryNode));
            Assert.IsInstanceOfType(dir.Children[2], typeof(BaseReadOnlyDirectoryNode));
            Assert.IsInstanceOfType(dir.Children[3], typeof(BaseFileNode));

            // Is the relative path build correctly
            Assert.AreEqual("filesystem\\folder1", dir.Children[0].RelativePath);
            Assert.AreEqual("filesystem\\folder1\\Class1.cs", (dir.Children[0] as BaseReadOnlyDirectoryNode)?.Children[1].RelativePath);

            // Is the name assigned correctly
            Assert.AreEqual("filesystem", dir.Name);
            Assert.AreEqual("Class6.cs", dir.Children[3].Name);
        }

        [TestMethod]
        public void PhysicalDirectoryMethodTests()
        {
            var dir = NodeFactory.FromDirectory("..\\..\\filesystem");

            // Does containment work
            Assert.IsTrue(dir.ContainsDirectory("folder1\\subfolder1_1"));
            Assert.IsFalse(dir.ContainsDirectory("folder"));
            Assert.IsTrue(dir.ContainsFile("folder1\\Class1.cs"));
            Assert.IsFalse(dir.ContainsFile("Class5.cs"));

            // Does enumeration work
            Assert.AreEqual(3, dir.EnumerateDirectories().Count());
            Assert.AreEqual(1, dir.EnumerateFiles().Count());

            // Does directory getter work
            Assert.AreEqual("subfolder1_1", dir.GetDirectoryNode("folder1\\subfolder1_1").Name);
            Assert.ThrowsException<DirectoryNotFoundException>(() => dir.GetDirectoryNode("folder\\subfolder1_1"));
            Assert.ThrowsException<PathOutOfRangeException>(() => dir.GetDirectoryNode("..\\bin"));
            Assert.ThrowsException<PathOutOfRangeException>(() => dir.GetDirectoryNode("folder1\\..\\..\\bin"));

            // Does file getter work
            Assert.AreEqual("Class1.cs", dir.GetFileNode("folder1\\Class1.cs").Name);
            Assert.ThrowsException<FileNotFoundException>(() => dir.GetFileNode("folder1\\Class2.cs"));
            Assert.AreEqual("Class6.cs", dir.GetFileNode("folder1\\..\\Class6.cs").Name);
            Assert.ThrowsException<PathOutOfRangeException>(() => dir.GetFileNode("..\\.\\FsTests.cs"));
            Assert.ThrowsException<PathOutOfRangeException>(() => dir.GetFileNode("folder1\\..\\.\\..\\FsTests.cs"));

            // Does adding work
            dir.AddDirectory("new_folder");
            dir.AddDirectory("folder1\\..\\new_folder3");
            dir.AddFile("new_folder2\\new_file.bin");
            dir.AddFile("folder1\\..\\new_folder4\\new_file.bin");
            Assert.IsTrue(dir.ContainsDirectory("new_folder"));
            Assert.IsTrue(dir.ContainsFile("new_folder2\\new_file.bin"));

            // Does removing work
            dir.RemoveDirectory("new_folder");
            dir.RemoveDirectory("folder1\\..\\new_folder3");
            dir.RemoveFile("new_folder2\\new_file.bin");
            dir.RemoveFile("folder1\\..\\new_folder4\\new_file.bin");
            dir.RemoveDirectory("new_folder2");
            dir.RemoveDirectory("new_folder3");
            dir.RemoveDirectory("new_folder4");
            Assert.IsFalse(dir.ContainsDirectory("new_folder"));
            Assert.IsFalse(dir.ContainsDirectory("new_folder2"));
            Assert.IsFalse(dir.ContainsFile("new_folder2\\new_file.bin"));

            // Test disposal behaviour
            dir.Dispose();
            Assert.IsTrue(dir.Disposed);
            Assert.ThrowsException<ObjectDisposedException>(() => dir.ContainsDirectory("folder1"));
        }

        [TestMethod]
        public void PhysicalFileMethodTests()
        {
            var jitFile = NodeFactory.FromFile("..\\..\\filesystem\\..\\filesystem\\folder4\\Class5.cs");
            var file = NodeFactory.FromFile("..\\..\\filesystem\\Class6.cs");
            var openedFile = file.Open();
            var reader = new StreamReader(openedFile, Encoding.UTF8);

            // Is just-in-time creation correctly working
            Assert.IsFalse(Directory.Exists("..\\..\\filesystem\\folder4"));
            Assert.IsFalse(File.Exists("..\\..\\filesystem\\folder4\\Class5.cs"));
            var jitFileStream = jitFile.Open();
            Assert.IsTrue(Directory.Exists("..\\..\\filesystem\\folder4"));
            Assert.IsTrue(File.Exists("..\\..\\filesystem\\folder4\\Class5.cs"));
            jitFileStream.Close();

            // Is opening and closing of the files correctly working
            jitFileStream = jitFile.Open();
            Assert.IsTrue(jitFile.IsOpened);
            Assert.ThrowsException<FileAlreadyOpenException>(() => jitFile.Open());
            jitFileStream.Close();
            Assert.IsFalse(jitFile.IsOpened);

            // Test normal reading in stream
            Assert.AreEqual("using System;", reader.ReadLine());

            // Test disposal behaviour
            file.Dispose();
            Assert.IsTrue(file.Disposed);
            Assert.IsFalse(file.IsOpened);
            Assert.ThrowsException<ObjectDisposedException>(() => file.Open());

            // Finish and reset
            reader.Dispose();
            openedFile.Close();
            file.Dispose();
            File.Delete("..\\..\\filesystem\\folder4\\Class5.cs");
            Directory.Delete("..\\..\\filesystem\\folder4");
        }

        [TestMethod]
        public void AfiStructureTests()
        {
            var afi = new ArchiveFileInfo { FileName = "test\\mohp\\file.bin", FileData = new MemoryStream() };
            var afi2 = new ArchiveFileInfo { FileName = "test2\\file2.bin", FileData = new MemoryStream() };
            var tree = NodeFactory.FromArchiveFileInfos(new List<ArchiveFileInfo> { afi, afi2 });

            // Are contained elements correctly assigned
            Assert.AreEqual(2, tree.Children.Count);
            Assert.IsInstanceOfType(tree.Children[0], typeof(BaseReadOnlyDirectoryNode));
            Assert.IsInstanceOfType(tree.Children[1], typeof(BaseReadOnlyDirectoryNode));

            // Are names correct
            Assert.AreEqual("test", tree.Children[0].Name);
            Assert.AreEqual("test2", (tree.Children[1] as BaseReadOnlyDirectoryNode)?.Children[0].Parent.Name);

            // Are relative paths correct
            Assert.AreEqual("test\\mohp", (tree.Children[0] as BaseReadOnlyDirectoryNode)?.Children[0].RelativePath);
        }

        [TestMethod]
        public void VirtualDirectoryMethodTests()
        {
            var afi = new ArchiveFileInfo { FileName = "test\\mohp\\file.bin", FileData = new MemoryStream() };
            var afi2 = new ArchiveFileInfo { FileName = "test2\\file2.bin", FileData = new MemoryStream() };
            var afi3 = new ArchiveFileInfo { FileName = "test2\\file\\test.bin", FileData = new MemoryStream() };
            var dir = NodeFactory.FromArchiveFileInfos(new List<ArchiveFileInfo> { afi, afi2 });

            // Does containment work
            Assert.IsTrue(dir.ContainsDirectory("test\\mohp"));
            Assert.IsFalse(dir.ContainsDirectory("mohp"));
            Assert.IsTrue(dir.ContainsFile("test2\\file2.bin"));
            Assert.IsFalse(dir.ContainsFile("file.bin"));

            // Does enumeration work
            Assert.AreEqual(2, dir.EnumerateDirectories().Count());
            Assert.AreEqual(0, dir.EnumerateFiles().Count());

            // Does directory getter work
            Assert.AreEqual("mohp", dir.GetDirectoryNode("test\\mohp").Name);
            Assert.ThrowsException<DirectoryNotFoundException>(() => dir.GetDirectoryNode("mohp"));
            Assert.ThrowsException<PathOutOfRangeException>(() => dir.GetDirectoryNode("..\\bin"));
            Assert.ThrowsException<PathOutOfRangeException>(() => dir.GetDirectoryNode("test\\..\\..\\bin"));

            // Does file getter work
            Assert.AreEqual("file.bin", dir.GetFileNode("test\\mohp\\file.bin").Name);
            Assert.ThrowsException<FileNotFoundException>(() => dir.GetFileNode("test\\file2.bin"));
            Assert.AreEqual("file.bin", dir.GetFileNode("test\\..\\test\\mohp\\file.bin").Name);
            Assert.ThrowsException<PathOutOfRangeException>(() => dir.GetFileNode("..\\.\\FsTests.cs"));
            Assert.ThrowsException<PathOutOfRangeException>(() => dir.GetFileNode("test\\..\\.\\..\\FsTests.cs"));

            // Does adding work
            dir.AddDirectory(new AfiDirectoryNode("new_test"));
            Assert.ThrowsException<PathMismatchException>(() => dir.AddFile(afi3));
            var afi3Dir = NodeFactory.FromArchiveFileInfo(afi3) as AfiDirectoryNode;
            dir.AddDirectory(afi3Dir);
            Assert.IsTrue(dir.ContainsDirectory("test2\\file"));
            (dir.GetDirectoryNode(Path.GetDirectoryName(afi3.FileName)) as AfiDirectoryNode)?.AddFile(afi3);
            Assert.IsTrue(dir.ContainsDirectory("new_test"));
            Assert.IsTrue(dir.ContainsFile("test2\\file\\test.bin"));

            // Does removing work
            Assert.ThrowsException<NotImplementedException>(() =>
                dir.RemoveDirectory(new AfiDirectoryNode("new_test")));
            Assert.ThrowsException<NotImplementedException>(() =>
                dir.RemoveFile(afi3));

            // Test disposal behaviour
            dir.Dispose();
            Assert.IsTrue(dir.Disposed);
            Assert.ThrowsException<ObjectDisposedException>(() => dir.ContainsDirectory("folder1"));
        }

        [TestMethod]
        public void VirtualFileMethodTests()
        {
            var afi = new ArchiveFileInfo { FileName = "test\\mohp\\file.bin", FileData = new MemoryStream(new byte[] { 0x00, 0x01, 0x02, 0x03 }) };
            var afi2 = new ArchiveFileInfo { FileName = "test2\\file2.bin", FileData = new MemoryStream(new byte[] { 0x04, 0x05, 0x06, 0x07 }) };
            var dir = NodeFactory.FromArchiveFileInfos(new List<ArchiveFileInfo> { afi, afi2 });

            var fileNode = dir.GetFileNode("test\\mohp\\file.bin");
            var openedFile = fileNode.Open();
            var reader = new BinaryReader(openedFile);

            Assert.AreEqual(0, reader.ReadByte());
            Assert.AreEqual(1, reader.ReadByte());
            openedFile.Close();
            Assert.ThrowsException<NullReferenceException>(() => openedFile.CanRead);
        }
    }
}
