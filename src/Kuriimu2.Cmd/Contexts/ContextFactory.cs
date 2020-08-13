using System;
using Kontract.Interfaces.Managers;
using Kontract.Interfaces.Plugins.State;

namespace Kuriimu2.Cmd.Contexts
{
    static class ContextFactory
    {
        public static IContext CreateFileContext(IStateInfo stateInfo, IContext parentContext, IMainContext mainContext)
        {
            switch (stateInfo.PluginState)
            {
                case ITextState _:
                    return new TextContext(stateInfo, parentContext, mainContext);

                case IImageState _:
                    return new ImageContext(stateInfo, parentContext, mainContext);

                case IArchiveState _:
                    return new ArchiveContext(stateInfo, parentContext, mainContext);

                default:
                    Console.WriteLine($"State '{stateInfo.PluginState.GetType()}' is not supported.");
                    return null;
            }
        }
    }
}
