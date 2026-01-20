#pragma warning disable
using System.Security.Cryptography;
using System.Text;

namespace UntamedMusicPlayer.OnlineAPIs.CloudMusicAPI.Utils;

internal static class Extensions
{
    extension(string value)
    {
        public byte[] ToByteArrayUtf8() => Encoding.UTF8.GetBytes(value);
    }

    extension(byte[] value)
    {
        public string ToHexStringLower() => Convert.ToHexString(value).ToLowerInvariant();

        public string ToHexStringUpper() => Convert.ToHexString(value);

        public string ToBase64String() => Convert.ToBase64String(value);

        public byte[] ComputeMd5() => MD5.HashData(value);
    }

    extension(Random random)
    {
        public byte[] RandomBytes(int length) => RandomNumberGenerator.GetBytes(length);
    }

    extension<TKey, TValue>(Dictionary<TKey, TValue> dictionary)
    {
        public TValue GetValueOrDefault(TKey key, TValue defaultValue) =>
            dictionary.TryGetValue(key, out var value) ? value : defaultValue;
    }
}
