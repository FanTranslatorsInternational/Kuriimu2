using System;
using System.IO;
using Kontract.Interfaces.Providers;

namespace Kontract.Interfaces.Managers
{
    /// <summary>
    /// Exposes methods to manage streams in a certain scope.
    /// </summary>
    public interface IStreamManager : IDisposable
    {
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
        Stream WrapUndisposable(Stream wrap);

        /// <summary>
        /// Checks if the given stream is managed by this instance.
        /// </summary>
        /// <param name="stream">The stream to check.</param>
        /// <returns>If the stream is managed by this instance.</returns>
        bool ContainsStream(Stream stream);

        /// <summary>
        /// Registers an already opened stream to this instance.
        /// </summary>
        /// <param name="register">The stream to register.</param>
        /// <param name="parent">The parent of the stream to register. Parent has to be registered in this instance.</param>
        void Register(Stream register, Stream parent = null);

        /// <summary>
        /// Disposes the given stream and releases base streams if they are managed by this instance.
        /// </summary>
        void Release(Stream release, bool recursive = false);

        /// <summary>
        /// Disposes all streams managed by this instance.
        /// </summary>
        void ReleaseAll();
    }
}
