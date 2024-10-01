using System.Text.Json;

namespace The_Untamed_Music_Player.Helpers;

public static class Json
{
    public static async Task<T?> ToObjectAsync<T>(string value)
    {
        return await Task.Run<T?>(() =>
        {
            return JsonSerializer.Deserialize<T>(value);
        });
    }

    public static async Task<string> StringifyAsync(object value)
    {
        return await Task.Run<string>(() =>
        {
            return JsonSerializer.Serialize(value);
        });
    }
}
