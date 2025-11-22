using Konnect.Contract.DataClasses.Management.Font;

namespace Konnect.Contract.Management.Font;

public interface IFontProfileManager
{
    FontProfile? Load(string filePath);

    void Save(string filePath, FontProfile profile);
}