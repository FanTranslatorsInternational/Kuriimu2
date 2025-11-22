namespace Konnect.Contract.Plugin.File.Hex;

public interface IHexFilePluginState : IFilePluginState
{
    Stream FileStream { get; }
}