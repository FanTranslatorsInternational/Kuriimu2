using Kontract.Interfaces.Managers;
using Kontract.Interfaces.Plugins.Identifier;
using Kontract.Interfaces.Plugins.State;

namespace Kore.Managers.Plugins.FileManagement
{
    class FileCreator
    {
        // TODO: Rethink creation process
        public IFileState Create(IFilePlugin entryPoint, IFileManager fileManager)
        {
            // 1. Create new state of the plugin
            var createdPlugin = entryPoint.CreatePluginState(fileManager);

            // 2. Create empty file state
            if (!TryCreateState(createdPlugin))
            {
                // TODO: Handle errors
                return null;
            }

            return null;
        }

        private bool TryCreateState(IPluginState state)
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
}
