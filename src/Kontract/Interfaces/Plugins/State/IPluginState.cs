namespace Kontract.Interfaces.Plugins.State
{
    /// <summary>
    /// A marker interface that each plugin state has to derive from.
    /// </summary>
    public interface IPluginState
    {
        #region Optional feature support checks
        
        public bool CanSave => this is ISaveFiles;
        public bool CanLoad => this is ILoadFiles;

        #endregion
    }
}
