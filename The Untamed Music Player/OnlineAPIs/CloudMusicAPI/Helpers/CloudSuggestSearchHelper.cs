using System.Diagnostics;
using System.Text.Json;
using The_Untamed_Music_Player.Models;

namespace The_Untamed_Music_Player.OnlineAPIs.CloudMusicAPI.Helpers;

public class CloudSuggestSearchHelper
{
    private static readonly NeteaseCloudMusicApi _api = App.GetService<NeteaseCloudMusicApi>();

    public static async Task<List<SearchResult>> GetSearchSuggestAsync(string keyWords)
    {
        var list = new List<SearchResult>();
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
                    AddResultsFromProperty(resultElement, "songs", 5, "\uE8D6", list);
                    AddResultsFromProperty(resultElement, "albums", 3, "\uE93C", list);
                    AddResultsFromProperty(resultElement, "artists", 3, "\uE77B", list);
                    AddResultsFromProperty(resultElement, "playlists", 2, "\uE728", list);
                }
            }
            catch
            {
                Debug.WriteLine("获取网易云音乐搜索建议失败");
            }
        });
        return list;
    }

    private static void AddResultsFromProperty(
        JsonElement element,
        string propertyName,
        int limit,
        string icon,
        List<SearchResult> list
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
            list.Add(new SearchResult { Icon = icon, Label = name });
        }
    }
}
