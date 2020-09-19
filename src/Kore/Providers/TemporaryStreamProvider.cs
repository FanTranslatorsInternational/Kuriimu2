using System;
using System.IO;
using Kontract;
using Kontract.Interfaces.Managers;
using Kontract.Interfaces.Providers;
using Kore.Streams;

namespace Kore.Providers
{
    /// <summary>
    /// Creates temporary streams on disk.
    /// </summary>
    class TemporaryStreamProvider : ITemporaryStreamProvider
    {
        private readonly string _temporaryDirectory;
        private readonly IStreamManager _streamManager;

        /// <summary>
        /// Creates a new instance of <see cref="TemporaryStreamProvider"/>.
        /// </summary>
        /// <param name="temporaryDirectory"></param>
        /// <param name="streamManager"></param>
        public TemporaryStreamProvider(string temporaryDirectory, IStreamManager streamManager)
        {
            ContractAssertions.IsNotNull(temporaryDirectory, nameof(temporaryDirectory));
            ContractAssertions.IsNotNull(streamManager, nameof(streamManager));

            _temporaryDirectory = temporaryDirectory;
            _streamManager = streamManager;
        }

        /// <inheritdoc />
        public Stream CreateTemporaryStream()
        {
            EnsureTemporaryDirectory(_temporaryDirectory);

            var fileName = GetTemporaryName();
            var file = File.Create(Path.Combine(_temporaryDirectory, fileName));

            var temporaryStream = new TemporaryStream(file);
            _streamManager.Register(temporaryStream);

            return temporaryStream;
        }

        /// <summary>
        /// Ensure that the given directory exists on disk.
        /// </summary>
        /// <param name="tempDirectory">The directory to ensure.</param>
        private void EnsureTemporaryDirectory(string tempDirectory)
        {
            if (!Directory.Exists(tempDirectory))
                Directory.CreateDirectory(tempDirectory);
        }

        /// <summary>
        /// Retrieve a temporary name.
        /// </summary>
        /// <returns>The temporary name.</returns>
        private string GetTemporaryName()
        {
            return Guid.NewGuid().ToString("D");
        }
    }
}
