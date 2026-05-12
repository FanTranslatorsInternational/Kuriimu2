using System;
using System.IO;
using System.Reflection;

namespace Kuriimu2.ImGui.Resources
{
    internal static class BinaryResources
    {
        #region Resource Names

        private const string ManifestResourceName_ = "Kuriimu2.ImGui.Resources.version.json";

        #endregion

        #region Resource Instances

        public static string VersionManifest => FromResource(ManifestResourceName_);

        #endregion

        private static string FromResource(string name)
        {
            var resourceStream = Assembly.GetExecutingAssembly().GetManifestResourceStream(name) 
                                 ?? throw new InvalidOperationException($"Could not load resource stream with name '{name}'.");

            var reader = new StreamReader(resourceStream);
            return reader.ReadToEnd();
        }
    }
}
