using System;
using System.IO;
using System.Threading.Tasks;
using Kontract.Interfaces.Progress;

namespace Kontract.Interfaces.Plugins.State.Intermediate
{
    /// <summary>
    /// Provides methods to hash files.
    /// </summary>
    public interface IHashAdapter:IIntermediate
    {
        /// <summary>
        /// Compute a hash from a file.
        /// </summary>
        /// <param name="toHash"></param>
        /// <param name="progress"></param>
        /// <returns></returns>
        Task<HashResult> Compute(Stream toHash, IProgressContext progress);
    }
}
