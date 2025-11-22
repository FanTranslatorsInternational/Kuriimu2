using Konnect.Contract.Management.Files;
using Konnect.Contract.Plugin.File;

namespace Konnect.Management.Files;

class FileCreator
{
    // TODO: Rethink creation process
    public IFileState Create(IFilePlugin entryPoint, IPluginFileManager baseFileManager)
    {
        // 1. Create new state of the plugin
        var createdPlugin = entryPoint.CreatePluginState(baseFileManager);

        // 2. Create empty file state
        if (!TryCreateState(createdPlugin))
        {
            // TODO: Handle errors
            return null;
        }

        return null;
    }

    private bool TryCreateState(IFilePluginState state)
    {
        // 1. Check if state implements ICreateFile
        //if (!(state is ICreateFile creatableState))
        //{
        //    return false;
        //}

        // 2. Create empty state
        //creatableState.CreateNew();
        return false;
    }
}