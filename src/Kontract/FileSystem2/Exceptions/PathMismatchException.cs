using System;
using System.Runtime.Serialization;
using Kontract.FileSystem2.Nodes.Abstract;
using Kontract.Interfaces.Archive;

namespace Kontract.FileSystem2.Exceptions
{
    [Serializable]
    public class PathMismatchException : Exception
    {
        public BaseReadOnlyDirectoryNode DirectoryNode { get; }
        public ArchiveFileInfo Afi { get; }

        public PathMismatchException(BaseReadOnlyDirectoryNode dirNode, ArchiveFileInfo afi) : base($"{nameof(ArchiveFileInfo)} doesn't match with the directory node \"{dirNode.RelativePath}\".")
        {
            DirectoryNode = dirNode;
            Afi = afi;
        }

        public PathMismatchException(string message) : base(message)
        {
        }

        public PathMismatchException(string message, Exception inner) : base(message, inner)
        {
        }

        protected PathMismatchException(
            SerializationInfo info,
            StreamingContext context) : base(info, context)
        {
        }

        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue(nameof(DirectoryNode), DirectoryNode);
            info.AddValue(nameof(Afi), Afi);
            base.GetObjectData(info, context);
        }
    }
}
