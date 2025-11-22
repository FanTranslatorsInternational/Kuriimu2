namespace Konnect.Contract.Management.Assembly;

public interface IAssemblyManager
{
    void FromPath(string path);
    void FromResource(string resource);
    void FromStream(Stream stream);
}