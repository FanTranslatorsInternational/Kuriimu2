using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Kore.Utilities.Text.TextSearcher
{
    public interface ITextSearcher
    {
        /// <summary>
        /// SearchAsync a pattern in a given string.
        /// </summary>
        /// <param name="input">The data as a string to search for.</param>
        /// <param name="encoding">The encoding the string is in.</param>
        /// <returns>The index of the first occurrence of the pattern.</returns>
        Task<int> SearchAsync(string input, Encoding encoding);

        /// <summary>
        /// SearchAsync a pattern in byte data.
        /// </summary>
        /// <param name="input">The data as a byte array.</param>
        /// <returns>The index of the first occurrence of the pattern.</returns>
        Task<int> SearchAsync(byte[] input);

        /// <summary>
        /// SearchAsync a pattern in a <see cref="BinaryReader"/>.
        /// </summary>
        /// <param name="br">The data as a <see cref="BinaryReader"/>.</param>
        /// <returns>The index of the first occurrence of the pattern.</returns>
        Task<int> SearchAsync(BinaryReader br);

        /// <summary>
        /// Cancel the current search operation.
        /// </summary>
        void Cancel();
    }
}
