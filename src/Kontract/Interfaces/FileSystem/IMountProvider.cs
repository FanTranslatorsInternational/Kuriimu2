namespace Kontract.Interfaces.FileSystem
{
    public interface IMountProvider
    {
        void Mount(IFileSystem fileSystem, string fileSystemName);

        void Unmount(IFileSystem fileSystem);
    }
}
