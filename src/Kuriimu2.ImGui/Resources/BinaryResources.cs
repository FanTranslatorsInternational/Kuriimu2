using System.IO;
using System.Reflection;

namespace Kuriimu2.ImGui.Resources
{
    static class BinaryResources
    {
        #region Resource Names

        private const string ManifestResourceName_ = "Kuriimu2.ImGui.Resources.version.json";

        #endregion

        #region Resource Instances

        public static string VersionManifest => FromResource(ManifestResourceName_);

        #endregion

        private static string FromResource(string name)
        {
            var resourceStream= Assembly.GetExecutingAssembly().GetManifestResourceStream(name);
            if (resourceStream == null)
                return null;

            var reader=new StreamReader(resourceStream);
            return reader.ReadToEnd();
        }
    }
}
