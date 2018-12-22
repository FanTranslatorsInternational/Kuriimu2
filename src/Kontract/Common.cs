using System;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.IO;

namespace Kontract
{
    /// <summary>
    /// Generic plugin handling code for all plugins.
    /// </summary>
    public static class Plugins
    {
        /// <summary>
        /// Re/Loads a plugin container for a given parent object.
        /// </summary>
        /// <param name="parent">The parent object to load plugins for.</param>
        /// <param name="pluginDirectory">The directory to load plugins from</param>
        public static void ComposePlugins(object parent, string pluginDirectory)
        {
            // An aggregate catalog that combines multiple catalogs.
            var catalog = new AggregateCatalog();

            if (Directory.Exists(pluginDirectory) && Directory.GetFiles(pluginDirectory, "*.dll").Length > 0)
                catalog.Catalogs.Add(new DirectoryCatalog(pluginDirectory));

            // Create the CompositionContainer with the parts in the catalog.
            var container = new CompositionContainer(catalog);

            // Fill the imports of this object.
            container.ComposeParts(parent);
        }

        /// <summary>
        /// Re/Loads a plugin container for a given parent object.
        /// </summary>
        /// <param name="parent">The parent object to load plugins for.</param>
        /// <param name="pluginDirectory">The directory to load plugins from</param>
        public static void ComposePlugins(object parent, CompositionContainer container, string pluginDirectory)
        {
            // An aggregate catalog that combines multiple catalogs.
            //var catalog = new AggregateCatalog();

            //if (Directory.Exists(pluginDirectory) && Directory.GetFiles(pluginDirectory, "*.dll").Length > 0)
            //    catalog.Catalogs.Add(new DirectoryCatalog(pluginDirectory));

            // Create the CompositionContainer with the parts in the catalog.
            //container = new CompositionContainer(catalog);

            // Fill the imports of this object.
            container.ComposeParts(parent);
        }
    }

    /// <summary>
    /// The ProgressReport class passes completion percentage and messages to the UI.
    /// </summary>
    public class ProgressReport
    {
        /// <summary>
        /// The current progress percentage being reported between 0 and 100.
        /// </summary>
        public double Percentage { get; set; } = 0.0;

        /// <summary>
        /// The current status message for this progress report.
        /// </summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// Extra data that may be provided by the async task.
        /// </summary>
        public object Data { get; set; } = null;

        /// <summary>
        /// Simple check for whether or not there is a message.
        /// </summary>
        public bool HasMessage => !string.IsNullOrWhiteSpace(Message);
    }


}
