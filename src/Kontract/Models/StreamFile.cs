using System.IO;
using Kontract.Models.IO;

namespace Kontract.Models
{
    /// <summary>
    /// A class combining a stream and a name to represent an in-memory file.
    /// </summary>
    public class StreamFile
    {
        /// <summary>
        /// The stream containing the file data.
        /// </summary>
        public Stream Stream { get; }

        /// <summary>
        /// The path of the file to represent.
        /// </summary>
        public UPath Path { get; }

        /// <summary>
        /// Creates a new instance of <see cref="StreamFile"/>.
        /// </summary>
        /// <param name="stream">The stream containing the file data.</param>
        /// <param name="path">The path of the file to represent.</param>
        public StreamFile(Stream stream, UPath path)
        {
            ContractAssertions.IsNotNull(stream,nameof(stream));

            Stream = stream;
            Path = path;
        }
    }
}
