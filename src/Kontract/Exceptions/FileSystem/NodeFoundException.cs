using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using Kontract.FileSystem2.Interfaces;

namespace Kontract.Exceptions.FileSystem
{
    [Serializable]
    public class NodeFoundException<T> : Exception
    {
        public INode<T> Node { get; }

        public NodeFoundException(INode<T> node) : base($"Node {node.Name} already exists.")
        {
            Node = node;
        }

        public NodeFoundException(string message) : base(message)
        {
        }

        public NodeFoundException(string message, Exception inner) : base(message, inner)
        {
        }

        protected NodeFoundException(
            SerializationInfo info,
            StreamingContext context) : base(info, context)
        {
        }

        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue(nameof(Node), Node.RelativePath);
            base.GetObjectData(info, context);
        }
    }
}
