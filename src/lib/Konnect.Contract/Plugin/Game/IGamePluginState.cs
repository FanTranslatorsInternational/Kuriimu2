namespace Konnect.Contract.Plugin.Game;

public interface IGamePluginState
{
    #region Optional feature checks

    bool CanProcessTexts => this is ITextProcessingState;

    #endregion

    #region Optional feature casting defaults

    ITextProcessingState? TextProcessing => this as ITextProcessingState;

    #endregion
}