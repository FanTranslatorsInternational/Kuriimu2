using Kontract.Interfaces.Managers.Streams;
using Zio;

namespace Kore.FileSystems
{
    public interface IKoreFileSystem : IFileSystem
    {
        /// <summary>
        /// Clones this <see cref="IKoreFileSystem"/> with the given <see cref="IStreamManager"/>.
        /// </summary>
        /// <param name="streamManager">The <see cref="IStreamManager"/> to place in the cloned <see cref="IKoreFileSystem"/>.</param>
        /// <returns>The cloned <see cref="IKoreFileSystem"/>.</returns>
        IKoreFileSystem Clone(IStreamManager streamManager);
    }
}
