using Kontract.Models.Context;

namespace Kontract.Interfaces.Plugins.Identifier
{
    public interface IRegisterAssembly
    {
        /// <summary>
        /// Allows registering assemblies that are not loaded by the main executable and are needed for execution.
        /// </summary>
        /// <param name="context">The context to register the assemblies in.</param>
        void RegisterAssemblies(DomainContext context);
    }
}
