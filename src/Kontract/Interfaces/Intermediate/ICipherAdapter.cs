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
    public interface ICipherAdapter : IIntermediate
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
        /// Eventhandler for requesting data
        /// </summary>
        event EventHandler<RequestDataEventArgs> RequestData;
    }

    /// <summary>
    /// The event arguments for requesting key material
    /// </summary>
    public class RequestDataEventArgs : EventArgs
    {
        public RequestDataEventArgs(string requestMessage, int dataLength, bool isRequestFile)
        {
            RequestMessage = requestMessage;
            DataSize = dataLength;
            IsRequestFile = isRequestFile;
        }

        /// <summary>
        /// The message displayed in an input field
        /// </summary>
        public string RequestMessage { get; }

        /// <summary>
        /// The length of the data
        /// </summary>
        public int DataSize { get; }

        /// <summary>
        /// Defines if event should request a file
        /// </summary>
        public bool IsRequestFile { get; }

        /// <summary>
        /// The data returned by the event
        /// </summary>
        public string Data { get; set; }
    }
}
