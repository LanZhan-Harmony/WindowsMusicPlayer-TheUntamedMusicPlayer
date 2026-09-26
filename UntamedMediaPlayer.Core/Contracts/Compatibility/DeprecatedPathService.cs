namespace UntamedMediaPlayer.Core.Contracts.Compatibility;

public interface IDeprecatedPathService
{
    bool HasDeprecatedAppData { get; }
    string? DeprecatedRootFolderPath { get; }
    string? SettingsFilePath { get; }
    string? PlaylistFolderPath { get; }
    string? PlaylistM3u8FolderPath { get; }
    string? MusicFoldersFilePath { get; }
}
