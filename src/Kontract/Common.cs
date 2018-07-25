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
        /// <param name="container">The container to load plugins into.</param>
        public static void ComposePlugins(object parent, CompositionContainer container)
        {
            try
            {
                // An aggregate catalog that combines multiple catalogs.
                var catalog = new AggregateCatalog();

                if (Directory.Exists("plugins") && Directory.GetFiles("plugins", "*.dll").Length > 0)
                    catalog.Catalogs.Add(new DirectoryCatalog("plugins"));

                // Create the CompositionContainer with the parts in the catalog.
                container?.Dispose();
                container = new CompositionContainer(catalog);

                // Fill the imports of this object.
                container.ComposeParts(parent);
            }
            catch (Exception ex)
            {

            }
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
        /// Simple check for whether or not there is a message.
        /// </summary>
        public bool HasMessage => !string.IsNullOrWhiteSpace(Message);
    }


}
