using System;
using Kontract.Models.Context;

namespace Kontract.Interfaces.Plugins.Identifier
{
    /// <summary>
    /// Marker interface for plugins that support loading additional assemblies.
    /// </summary>
    /// <see cref="IPlugin"/>
    [Obsolete("Override IPlugin.RegisterAssemblies instead")]
    public interface IRegisterAssembly
    {
        /// <summary>
        /// Allows registering assemblies that are not loaded by the main executable and are needed for execution.
        /// </summary>
        /// <param name="context">The context to register the assemblies in.</param>
        void RegisterAssemblies(DomainContext context);
    }
}
