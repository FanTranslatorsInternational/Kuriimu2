using Kontract.Interfaces.Plugins.State;
using Kontract.Interfaces.Progress;

namespace Kontract.Models.Context
{
    /// <summary>
    /// The class containing all environment instances for a <see cref="ISaveFiles.Save"/> action.
    /// </summary>
    public class SaveContext
    {
        /// <summary>
        /// The progress context.
        /// </summary>
        public IProgressContext ProgressContext { get; }

        /// <summary>
        /// Creates a new instance of <see cref="SaveContext"/>.
        /// </summary>
        /// <param name="progress">The progress for this action.</param>
        public SaveContext(IProgressContext progress)
        {
            ContractAssertions.IsNotNull(progress, nameof(progress));

            ProgressContext = progress;
        }
    }
}
