using Wiper.Core.Models;

namespace Wiper.Core.Interfaces;

public interface ISettingsService
{
    WiperSettings Settings { get; }
    Task LoadAsync();
    Task SaveAsync();
    void AddGhostProcess(string processName);
    void RemoveGhostProcess(string processName);
    void AddTargetFolder(string folderName);
    void RemoveTargetFolder(string folderName);
}
