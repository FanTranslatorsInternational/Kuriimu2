namespace Kontract.Interfaces.Archive
{
    /// <summary>
    /// 
    /// </summary>
    public interface IArchiveRenameFiles
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="afi"></param>
        /// <param name="newFilename"></param>
        void RenameFile(ArchiveFileInfo afi, string newFilename);
    }
}
