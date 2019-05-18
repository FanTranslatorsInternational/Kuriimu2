namespace Kore.Files.Models
{
    // TODO: Documentation and proper creation of the object
    public class KoreSaveInfo
    {
        public KoreSaveInfo(KoreFileInfo kfi, string tempFolder)
        {
            Kfi = kfi;
            TempFolder = tempFolder;
        }

        public KoreFileInfo Kfi { get; }
        public string TempFolder { get; }
        public string NewSaveFile { get; set; }
        public int Version { get; set; }
        public bool OverwriteExistingFiles { get; set; }
    }
}
