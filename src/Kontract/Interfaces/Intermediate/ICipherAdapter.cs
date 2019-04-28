using System;
using System.IO;
using System.Threading.Tasks;
using Kontract.Models;
using Kontract.Models.Intermediate;

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
}
