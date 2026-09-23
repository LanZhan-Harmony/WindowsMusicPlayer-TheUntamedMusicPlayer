namespace UntamedMediaPlayer.Core.Contracts.Services;

public interface ILocalizationService
{
    string GetString(string resourceKey);
    string GetString(string resourceKey, params object[] args);
}
