namespace UntamedMediaPlayer.Core.Contracts.Services;

public interface IPathService
{
    string RootPath { get; }
    string SettingsPath { get; }
    string DatabasePath { get; }
}
