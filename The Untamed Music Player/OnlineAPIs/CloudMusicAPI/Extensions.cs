#pragma warning disable

using System.Security.Cryptography;
using System.Text;

namespace The_Untamed_Music_Player.OnlineAPIs.CloudMusicAPI;
internal static class Extensions
{
    private static readonly MD5 _md5 = MD5.Create();

    public static byte[] ToByteArrayUtf8(this string value) => Encoding.UTF8.GetBytes(value);

    public static string ToHexStringLower(this byte[] value)
    {
        StringBuilder sb;

        sb = new StringBuilder();
        for (var i = 0; i < value.Length; i++)
        {
            sb.Append(value[i].ToString("x2"));
        }

        return sb.ToString();
    }

    public static string ToHexStringUpper(this byte[] value)
    {
        StringBuilder sb;

        sb = new StringBuilder();
        for (var i = 0; i < value.Length; i++)
        {
            sb.Append(value[i].ToString("X2"));
        }

        return sb.ToString();
    }

    public static string ToBase64String(this byte[] value) => Convert.ToBase64String(value);

    public static byte[] ComputeMd5(this byte[] value) => _md5.ComputeHash(value);

    public static byte[] RandomBytes(this Random random, int length)
    {
        byte[] buffer;

        buffer = new byte[length];
        random.NextBytes(buffer);
        return buffer;
    }

    public static TValue GetValueOrDefault<TKey, TValue>(this Dictionary<TKey, TValue> dictionary, TKey key, TValue defaultValue) => dictionary.TryGetValue(key, out var value) ? value : defaultValue;
}
