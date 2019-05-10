using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Kontract.Exceptions.FileSystem
{
    [Serializable]
    public class FileAlreadyOpenException : Exception
    {
        public FileAlreadyOpenException()
        {
        }

        public FileAlreadyOpenException(string message) : base(message)
        {
        }

        public FileAlreadyOpenException(string message, Exception inner) : base(message, inner)
        {
        }

        protected FileAlreadyOpenException(
            SerializationInfo info,
            StreamingContext context) : base(info, context)
        {
        }
    }
}
