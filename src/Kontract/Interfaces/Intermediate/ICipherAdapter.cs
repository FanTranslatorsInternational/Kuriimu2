using Kontract.Interfaces.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kontract.Interfaces.Intermediate
{
    /// <summary>
    /// Provides methods to decrypt or encrypt files
    /// </summary>
    public interface ICipherAdapter : IPlugin
    {
        /// <summary>
        /// Encrypts a file
        /// </summary>
        /// <param name="toEncrypt"></param>
        /// <param name="encryptInto"></param>
        /// <param name="progress"></param>
        Task<bool> Encrypt(Stream toEncrypt, Stream encryptInto, IProgress<ProgressReport> progress);

        /// <summary>
        /// Decrypts a file
        /// </summary>
        /// <param name="toDecrypt"></param>
        /// <param name="decryptInto"></param>
        /// <param name="progress"></param>
        Task<bool> Decrypt(Stream toDecrypt, Stream decryptInto, IProgress<ProgressReport> progress);

        /// <summary>
        /// Eventhandler for requesting key material
        /// </summary>
        EventHandler<RequestKeyEventArgs> RequestKey { get; set; }
    }

    /// <summary>
    /// The event arguments for requesting key material
    /// </summary>
    public class RequestKeyEventArgs : EventArgs
    {
        public RequestKeyEventArgs(string requestMessage, int dataLength)
        {
            RequestMessage = requestMessage;
            DataSize = dataLength;
        }

        /// <summary>
        /// The message displayed in an input field
        /// </summary>
        public string RequestMessage { get; }

        /// <summary>
        /// The length of the key
        /// </summary>
        public int DataSize { get; }

        /// <summary>
        /// The key returned by the event
        /// </summary>
        public byte[] Data { get; set; }
    }
}
