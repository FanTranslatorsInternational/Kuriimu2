using System;
using Serilog;

namespace Kontract.Interfaces.Managers.Streams
{
    /// <summary>
    /// Exposes methods to manage streams in a certain scope.
    /// </summary>
    public interface IStreamManager : IDisposable
    {
        /// <summary>
        /// The logger of this stream manager.
        /// </summary>
        ILogger Logger { get; set; }

        /// <summary>
        /// The amount of stream registered.
        /// </summary>
        int Count { get; }

        /// <summary>
        /// Creates an <see cref="ITemporaryStreamProvider"/> in the scope of this instance.
        /// </summary>
        /// <returns>The created <see cref="ITemporaryStreamProvider"/>.</returns>
        ITemporaryStreamProvider CreateTemporaryStreamProvider();

        /// <summary>
        /// Wrap any stream in an undisposable container.
        /// </summary>
        /// <param name="wrap">The stream to wrap.</param>
        /// <returns>The undisposable container.</returns>
        System.IO.Stream WrapUndisposable(System.IO.Stream wrap);

        /// <summary>
        /// Checks if the given stream is managed by this instance.
        /// </summary>
        /// <param name="stream">The stream to check.</param>
        /// <returns>If the stream is managed by this instance.</returns>
        bool ContainsStream(System.IO.Stream stream);

        /// <summary>
        /// Registers an already opened stream to this instance.
        /// </summary>
        /// <param name="register">The stream to register.</param>
        /// <param name="parent">The parent of the stream to register. Parent has to be registered in this instance.</param>
        void Register(System.IO.Stream register, System.IO.Stream parent = null);

        /// <summary>
        /// Disposes the given stream and releases base streams if they are managed by this instance.
        /// </summary>
        void Release(System.IO.Stream release, bool recursive = false);

        /// <summary>
        /// Disposes all streams managed by this instance.
        /// </summary>
        void ReleaseAll();
    }
}
