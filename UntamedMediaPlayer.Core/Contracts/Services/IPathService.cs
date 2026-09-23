namespace UntamedMediaPlayer.Core.Contracts.Services;

public interface IPathService
{
    string RootFolderPath { get; }
    string SettingsFolderPath { get; }
    string DatabaseFolderPath { get; }
    string LogFolderPath { get; }
}
