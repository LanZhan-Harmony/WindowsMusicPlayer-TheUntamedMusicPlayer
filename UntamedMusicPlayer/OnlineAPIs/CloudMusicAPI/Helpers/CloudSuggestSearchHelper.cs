using System.Text.Json;
using Microsoft.Extensions.Logging;
using UntamedMusicPlayer.Models;
using UntamedMusicPlayer.Services;
using ZLogger;

namespace UntamedMusicPlayer.OnlineAPIs.CloudMusicAPI.Helpers;

public class CloudSuggestSearchHelper
{
    private static readonly ILogger _logger =
        LoggingService.CreateLogger<CloudSuggestSearchHelper>();
    private static readonly NeteaseCloudMusicApi _api = NeteaseCloudMusicApi.Instance;

    public static async Task<List<SuggestResult>> GetSuggestAsync(string keyWords)
    {
        var list = new List<SuggestResult>();
        await Task.Run(async () =>
        {
            try
            {
                var (_, result) = await _api.RequestAsync(
                    CloudMusicApiProviders.SearchSuggest,
                    new Dictionary<string, string> { { "keywords", $"{keyWords}" } }
                );

                using var document = JsonDocument.Parse(result.ToJsonString());
                var root = document.RootElement;

                if (root.TryGetProperty("result", out var resultElement))
                {
                    AddResultsFromProperty(resultElement, "songs", 5, "\uE940", list);
                    AddResultsFromProperty(resultElement, "albums", 3, "\uE93C", list);
                    AddResultsFromProperty(resultElement, "artists", 3, "\uE77B", list);
                    AddResultsFromProperty(resultElement, "playlists", 2, "\uE728", list);
                }
            }
            catch (Exception ex)
            {
                _logger.ZLogInformation(ex, $"获取网易云搜索建议失败");
            }
        });
        return list;
    }

    private static void AddResultsFromProperty(
        JsonElement element,
        string propertyName,
        int limit,
        string icon,
        List<SuggestResult> list
    )
    {
        if (!element.TryGetProperty(propertyName, out var arrayElement))
        {
            return;
        }

        var names = arrayElement
            .EnumerateArray()
            .Select(item => item.GetProperty("name").GetString()!)
            .Distinct()
            .Take(limit);

        foreach (var name in names)
        {
            list.Add(new SuggestResult { Icon = icon, Label = name });
        }
    }
}
