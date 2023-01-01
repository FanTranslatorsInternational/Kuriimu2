using Kontract.Interfaces.Managers.Streams;
using Kontract.Interfaces.Plugins.Entry;

namespace Kontract.Models.Plugins.Entry
{
    /// <summary>
    /// The class containing all environment instances for a <see cref="IIdentifyFiles.IdentifyAsync"/> action.
    /// </summary>
    public class IdentifyContext
    {
        /// <summary>
        /// The provider for temporary streams.
        /// </summary>
        public ITemporaryStreamProvider TemporaryStreamManager { get; }

        /// <summary>
        /// Creates a new instance of <see cref="IdentifyContext"/>.
        /// </summary>
        /// <param name="temporaryStreamProvider">The provider for temporary streams.</param>
        public IdentifyContext(ITemporaryStreamProvider temporaryStreamProvider)
        {
            ContractAssertions.IsNotNull(temporaryStreamProvider, nameof(temporaryStreamProvider));

            TemporaryStreamManager = temporaryStreamProvider;
        }
    }
}
