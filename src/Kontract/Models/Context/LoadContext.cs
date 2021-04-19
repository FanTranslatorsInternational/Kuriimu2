using Kontract.Interfaces.Managers;
using Kontract.Interfaces.Plugins.State;
using Kontract.Interfaces.Progress;
using Kontract.Interfaces.Providers;

namespace Kontract.Models.Context
{
    /// <summary>
    /// The class containing all environment instances for a <see cref="IPluginState.Load"/> action.
    /// </summary>
    public class LoadContext
    {
        /// <summary>
        /// The provider for temporary streams.
        /// </summary>
        public ITemporaryStreamProvider TemporaryStreamManager { get; }

        /// <summary>
        /// The progress context.
        /// </summary>
        public IProgressContext ProgressContext { get; }

        /// <summary>
        /// The dialog manager.
        /// </summary>
        public IDialogManager DialogManager { get; }

        /// <summary>
        /// Creates a new instance of <see cref="LoadContext"/>.
        /// </summary>
        /// <param name="temporaryStreamProvider">The provider for temporary streams.</param>
        /// <param name="progress">The progress for this action.</param>
        /// <param name="dialogManager">The dialog manager for this action.</param>
        public LoadContext(ITemporaryStreamProvider temporaryStreamProvider, IProgressContext progress, IDialogManager dialogManager)
        {
            ContractAssertions.IsNotNull(temporaryStreamProvider, nameof(temporaryStreamProvider));
            ContractAssertions.IsNotNull(progress, nameof(progress));
            ContractAssertions.IsNotNull(dialogManager, nameof(dialogManager));

            TemporaryStreamManager = temporaryStreamProvider;
            ProgressContext = progress;
            DialogManager = dialogManager;
        }
    }
}
