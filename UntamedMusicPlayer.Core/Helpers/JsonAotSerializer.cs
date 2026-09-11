using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace UntamedMusicPlayer.Core.Helpers;

[JsonSourceGenerationOptions(
    WriteIndented = true,
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    PropertyNameCaseInsensitive = true
)]
[JsonSerializable(typeof(bool))]
[JsonSerializable(typeof(char))]
[JsonSerializable(typeof(byte))]
[JsonSerializable(typeof(short))]
[JsonSerializable(typeof(ushort))]
[JsonSerializable(typeof(int))]
[JsonSerializable(typeof(uint))]
[JsonSerializable(typeof(long))]
[JsonSerializable(typeof(ulong))]
[JsonSerializable(typeof(float))]
[JsonSerializable(typeof(double))]
[JsonSerializable(typeof(string))]
internal sealed partial class SourceGenerationContext : JsonSerializerContext;

internal static class JsonAotSerializer
{
    /// <summary>
    /// 将对象序列化为 JSON 字符串。
    /// </summary>
    /// <param name="value">要序列化的对象</param>
    /// <returns>序列化后的 JSON 字符串</returns>
    internal static string Serialize<T>(T value)
    {
        if (
            SourceGenerationContext.Default.GetTypeInfo(typeof(T))
            is not JsonTypeInfo<T> jsonTypeInfo
        )
        {
            throw new ArgumentNullException(
                nameof(value),
                $"SourceGenerationContext 特性中未声明 {typeof(T)}."
            );
        }
        return JsonSerializer.Serialize(value, jsonTypeInfo);
    }

    /// <summary>
    /// 将 JSON 字符串反序列化为指定类型的对象。
    /// </summary>
    /// <typeparam name="T">目标类型</typeparam>
    /// <param name="value">JSON 字符串</param>
    /// <returns>反序列化后的对象</returns>
    internal static T? Deserialize<T>(string value)
    {
        if (
            SourceGenerationContext.Default.GetTypeInfo(typeof(T))
            is not JsonTypeInfo<T> jsonTypeInfo
        )
        {
            throw new ArgumentNullException(
                nameof(T),
                $"SourceGenerationContext 特性中未声明 {typeof(T)}."
            );
        }
        return JsonSerializer.Deserialize(value, jsonTypeInfo);
    }

    internal static JsonTypeInfo<T> GetTypeInfo<T>()
    {
        if (
            SourceGenerationContext.Default.GetTypeInfo(typeof(T))
            is not JsonTypeInfo<T> jsonTypeInfo
        )
        {
            throw new InvalidOperationException(
                $"SourceGenerationContext 特性中未声明 {typeof(T)}."
            );
        }
        return jsonTypeInfo;
    }
}
