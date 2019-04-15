using System;
using System.IO;

namespace Kontract.FileSystem
{
    /// <inheritdoc />
    /// <summary>
    /// 
    /// </summary>
    public class CloseStreamEventArgs : EventArgs
    {
        /// <summary>
        /// 
        /// </summary>
        public Stream BaseStream { get; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="baseStream"></param>
        public CloseStreamEventArgs(Stream baseStream)
        {
            BaseStream = baseStream;
        }
    }
}
