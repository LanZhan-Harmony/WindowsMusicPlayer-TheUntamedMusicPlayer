using Microsoft.Extensions.Logging;
using UntamedMusicPlayer.Core.Models;
using UntamedMusicPlayer.Core.Services;
using UntamedMusicPlayer.OnlineAPI.CloudMusicAPI;
using UntamedMusicPlayer.OnlineAPI.CloudMusicAPI.Models;
using ZLogger;

namespace UntamedMusicPlayer.Core.OnlineAPIs.CloudMusicAPI.Helpers;

public sealed class CloudSuggestSearchHelper
{
    private static readonly ILogger _logger =
        CoreLoggingService.CreateLogger<CloudSuggestSearchHelper>();

    public static async Task<List<SuggestResult>> GetSuggestAsync(
        string keyWords,
        CloudMusicApiService api
    )
    {
        var list = new List<SuggestResult>();
        await Task.Run(async () =>
        {
            try
            {
                var (_, response) = await api.SearchSuggestionsAsync(keyWords);

                if (response?.Result is not { } resultElement)
                {
                    return;
                }

                AddResults(resultElement.Songs, 5, "\uE940", list);
                AddResults(resultElement.Albums, 3, "\uE93C", list);
                AddResults(resultElement.Artists, 3, "\uE77B", list);
                AddResults(resultElement.Playlists, 2, "\uE728", list);
            }
            catch (Exception ex)
            {
                _logger.ZLogInformation(ex, $"获取网易云搜索建议失败");
            }
        });
        return list;
    }

    private static void AddResults(
        IEnumerable<CloudSearchSuggestionItemDto>? items,
        int limit,
        string icon,
        List<SuggestResult> list
    )
    {
        var names = items
            ?.Select(item => item.Name)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Cast<string>()
            .Distinct()
            .Take(limit);

        if (names is null)
        {
            return;
        }

        foreach (var name in names)
        {
            list.Add(new SuggestResult { Icon = icon, Label = name });
        }
    }
}
