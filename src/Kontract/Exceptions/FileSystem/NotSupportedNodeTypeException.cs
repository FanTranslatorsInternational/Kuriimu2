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
    public class NotSupportedNodeTypeException<T> : Exception
    {
        public INode<T> Node { get; }

        public NotSupportedNodeTypeException(INode<T> node) : base($"Node type {node.GetType().FullName} is invalid.")
        {
            Node = node;
        }

        public NotSupportedNodeTypeException(string message) : base(message)
        {
        }

        public NotSupportedNodeTypeException(string message, Exception inner) : base(message, inner)
        {
        }

        protected NotSupportedNodeTypeException(
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
