using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Kontract.Exceptions.FileSystem;
using Kontract.FileSystem2;
using Kontract.FileSystem2.Nodes.Abstract;
using Kontract.FileSystem2.Nodes.Afi;
using Kontract.FileSystem2.Nodes.Physical;
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
            var file = NodeFactory.FromFile("..\\..\\filesystem\\Class6.cs");

            Assert.AreEqual(4, dir.Children.Count);
            Assert.IsInstanceOfType(dir.Children[0], typeof(BaseDirectoryNode));
            Assert.IsInstanceOfType(dir.Children[1], typeof(BaseDirectoryNode));
            Assert.IsInstanceOfType(dir.Children[2], typeof(BaseDirectoryNode));
            Assert.IsInstanceOfType(dir.Children[3], typeof(BaseFileNode));
            Assert.AreEqual("\\filesystem\\folder1\\Class1.cs", (dir.Children[0] as BaseDirectoryNode).Children[1].Path);

            Assert.AreEqual("Class6.cs", file.Name);

            var newFolder = NodeFactory.FromDirectory("..\\..\\filesystem\\folder4");
            Assert.IsFalse(Directory.Exists("..\\..\\filesystem\\folder4"));

            var newFile = NodeFactory.FromFile("..\\..\\filesystem\\Class5.cs").Open();
            var newFile2 = NodeFactory.FromFile("..\\..\\filesystem\\folder4\\Class5.cs").Open();
            Assert.IsTrue(Directory.Exists("..\\..\\filesystem\\folder4"));
            Assert.IsTrue(File.Exists("..\\..\\filesystem\\Class5.cs"));
            newFile.Close();
            newFile2.Close();

            File.Delete("..\\..\\filesystem\\Class5.cs");
            File.Delete("..\\..\\filesystem\\folder4\\Class5.cs");
            Directory.Delete("..\\..\\filesystem\\folder4");
        }

        [TestMethod]
        public void PhysicalDirectoryMethodTests()
        {
            var dir = NodeFactory.FromDirectory("..\\..\\filesystem");

            Assert.IsTrue(dir.ContainsDirectory("folder1\\subfolder1_1"));
            Assert.IsFalse(dir.ContainsDirectory("folder"));
            Assert.IsTrue(dir.ContainsFile("folder1\\Class1.cs"));
            Assert.IsFalse(dir.ContainsFile("Class5.cs"));

            Assert.AreEqual(3, dir.EnumerateDirectories().Count());
            Assert.AreEqual(1, dir.EnumerateFiles().Count());

            Assert.AreEqual("subfolder1_1", dir.GetDirectoryNode("folder1\\subfolder1_1").Name);
            Assert.ThrowsException<DirectoryNotFoundException>(() => dir.GetDirectoryNode("folder\\subfolder1_1"));
            Assert.AreEqual("Class1.cs", dir.GetFileNode("folder1\\Class1.cs").Name);
            Assert.ThrowsException<FileNotFoundException>(() => dir.GetFileNode("folder1\\Class2.cs"));

            dir.Add(new PhysicalDirectoryNode("new_test"));
            dir.Add(new PhysicalFileNode("test.bin", Path.GetFullPath("..\\..\\filesystem\\test.bin")));
            dir.AddRange(new List<BaseNode> { new PhysicalDirectoryNode("new_test2"), new PhysicalDirectoryNode("new_test3") });
            Assert.IsTrue(dir.ContainsDirectory("new_test"));
            Assert.IsTrue(dir.ContainsDirectory("new_test2"));
            Assert.IsTrue(dir.ContainsDirectory("new_test3"));
            Assert.IsTrue(dir.ContainsFile("test.bin"));

            var file = dir.GetFileNode("Class6.cs");
            dir.Remove(file);
            Assert.IsFalse(dir.ContainsFile("Class6.cs"));

            dir.ClearChildren();
            Assert.AreEqual(0, dir.Children.Count);

            dir.Dispose();
            Assert.IsTrue(dir.Disposed);
            Assert.ThrowsException<ObjectDisposedException>(() => dir.ContainsDirectory("folder1"));
        }

        [TestMethod]
        public void PhysicalFileMethodTests()
        {
            var file = NodeFactory.FromFile("..\\..\\filesystem\\Class6.cs");
            var file2 = NodeFactory.FromFile("..\\..\\filesystem\\Class7.cs");
            var openedFile = file.Open();
            var openedFile2 = file2.Open();
            var reader = new StreamReader(openedFile, Encoding.UTF8);

            Assert.ThrowsException<FileAlreadyOpenException>(() => file.Open());
            Assert.AreEqual("using System;", reader.ReadLine());
            openedFile.Close();
            Assert.IsFalse(openedFile.CanRead);
            Assert.IsFalse(file.IsOpened);
            Assert.IsTrue(File.Exists("..\\..\\filesystem\\Class7.cs"));

            openedFile2.Close();

            File.Delete("..\\..\\filesystem\\Class7.cs");
        }

        [TestMethod]
        public void VirtualStructureTests()
        {
            var afi = new ArchiveFileInfo { FileName = "test\\mohp\\file.bin", FileData = new MemoryStream() };
            var afi2 = new ArchiveFileInfo { FileName = "test2\\file2.bin", FileData = new MemoryStream() };
            var tree = NodeFactory.FromArchiveFileInfos(new List<ArchiveFileInfo> { afi });
            tree.Add(NodeFactory.FromArchiveFileInfo(afi2));

            Assert.AreEqual(2, tree.Children.Count);
            Assert.IsInstanceOfType(tree.Children[0], typeof(BaseDirectoryNode));
            Assert.IsInstanceOfType(tree.Children[1], typeof(BaseDirectoryNode));
            Assert.AreEqual("test", tree.Children[0].Name);
            Assert.AreEqual("file2.bin", (tree.Children[1] as BaseDirectoryNode).Children[0].Name);
            var newAfiNode = NodeFactory.FromArchiveFileInfo(new ArchiveFileInfo { FileName = "test\\mohp\\file.bin", FileData = new MemoryStream() });
            Assert.ThrowsException<NodeFoundException>(() => tree.Add(newAfiNode));
        }

        [TestMethod]
        public void VirtualDirectoryMethodTests()
        {
            var afi = new ArchiveFileInfo { FileName = "test\\mohp\\file.bin", FileData = new MemoryStream() };
            var afi2 = new ArchiveFileInfo { FileName = "test2\\file2.bin", FileData = new MemoryStream() };
            var afi3 = new ArchiveFileInfo { FileName = "test2\\file\\test.bin", FileData = new MemoryStream() };
            var tree = NodeFactory.FromArchiveFileInfos(new List<ArchiveFileInfo> { afi, afi2 });

            Assert.IsTrue(tree.ContainsDirectory("test\\mohp"));
            Assert.IsFalse(tree.ContainsDirectory("mohp"));
            Assert.IsTrue(tree.ContainsFile("test2\\file2.bin"));
            Assert.IsFalse(tree.ContainsFile("file.bin"));

            Assert.AreEqual(2, tree.EnumerateDirectories().Count());
            Assert.AreEqual(0, tree.EnumerateFiles().Count());

            Assert.AreEqual("mohp", tree.GetDirectoryNode("test\\mohp").Name);
            Assert.ThrowsException<DirectoryNotFoundException>(() => tree.GetDirectoryNode("mohp"));
            Assert.AreEqual("file2.bin", tree.GetFileNode("test2\\file2.bin").Name);
            Assert.ThrowsException<FileNotFoundException>(() => tree.GetFileNode("file.bin"));

            tree.Add(NodeFactory.FromArchiveFileInfo(afi3));
            Assert.AreEqual(2, tree.Children.Count);
            Assert.IsTrue(tree.ContainsDirectory("test2\\file"));
            Assert.IsTrue(tree.ContainsFile("test2\\file\\test.bin"));
            Assert.ThrowsException<NodeFoundException>(() => tree.Add(NodeFactory.FromArchiveFileInfo(afi2)));

            var dirNode = tree.GetDirectoryNode("test2");
            var fileNode = dirNode.GetFileNode("file2.bin");
            dirNode.Remove(fileNode);
            Assert.IsFalse(tree.ContainsFile("test2\\file2.bin"));

            tree.ClearChildren();
            Assert.AreEqual(0, tree.Children.Count);

            tree.Dispose();
            Assert.IsTrue(tree.Disposed);
            Assert.ThrowsException<ObjectDisposedException>(() => tree.ContainsDirectory("test"));
        }

        [TestMethod]
        public void VirtualFileMethodTests()
        {
            var stream = new MemoryStream(new byte[] { 0x00, 0x01, 0x02, 0x03 });
            var stream2 = new MemoryStream(new byte[] { 0x04, 0x05, 0x06, 0x07 });
            var afi = new ArchiveFileInfo() { FileName = "test\\file.bin", FileData = stream };
            var afi2 = new ArchiveFileInfo() { FileName = "test\\file2.bin", FileData = stream2 };
            var file = (NodeFactory.FromArchiveFileInfo(afi) as AfiDirectoryNode).Children[0] as BaseFileNode;
            var file2 = (NodeFactory.FromArchiveFileInfo(afi2) as AfiDirectoryNode).Children[0] as BaseFileNode;
            var openedFile = file.Open();
            var openedFile2 = file2.Open();
            var reader = new BinaryReader(openedFile);

            Assert.AreEqual(0, reader.ReadByte());
            openedFile.Close();
            Assert.ThrowsException<NullReferenceException>(() => openedFile.CanRead);
            Assert.IsTrue(stream.CanRead);

            openedFile2.Close();
        }
    }
}
