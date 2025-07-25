using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using The_Untamed_Music_Player.Models;
using The_Untamed_Music_Player.OnlineAPIs.CloudMusicAPI;
using Windows.UI;

namespace The_Untamed_Music_Player.Helpers;

[JsonSerializable(typeof(bool))]
[JsonSerializable(typeof(byte))]
[JsonSerializable(typeof(short))]
[JsonSerializable(typeof(int))]
[JsonSerializable(typeof(double))]
[JsonSerializable(typeof(string))]
[JsonSerializable(typeof(Color))]
[JsonSerializable(typeof(List<string>))]
[JsonSerializable(typeof(BriefLocalSongInfo))]
[JsonSerializable(typeof(BriefCloudOnlineSongInfo))]
public partial class JsonContext : JsonSerializerContext { }

public static class Json
{
    public static async Task<T?> ToObjectAsync<T>(string value)
    {
        return await Task.Run(() =>
        {
            if (JsonContext.Default.GetTypeInfo(typeof(T)) is not JsonTypeInfo<T> jsonTypeInfo)
            {
                throw new ArgumentNullException(
                    nameof(T),
                    $"JsonSerializable特性中未声明 {typeof(T)}."
                );
            }
            return JsonSerializer.Deserialize(value, jsonTypeInfo);
        });
    }

    public static async Task<string> StringifyAsync(object value)
    {
        return await Task.Run(() =>
        {
            var type = value.GetType();
            var jsonTypeInfo = JsonContext.Default.GetTypeInfo(type);
            return jsonTypeInfo is null
                ? throw new ArgumentNullException(
                    nameof(value),
                    $"JsonSerializable特性中未声明 {type}."
                )
                : JsonSerializer.Serialize(value, jsonTypeInfo);
        });
    }
}
