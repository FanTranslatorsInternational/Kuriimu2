namespace Konnect.Contract.Plugin.File.Image;

public interface IImageFilePluginState : IFilePluginState
{
    IReadOnlyList<IImageFile> Images { get; }
}