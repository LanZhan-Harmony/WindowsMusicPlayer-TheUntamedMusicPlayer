#pragma warning disable
using System.Numerics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace UntamedMusicPlayer.OnlineAPIs.CloudMusicAPI.Utils;

internal static class Crypto
{
    private static readonly byte[] iv = Encoding.ASCII.GetBytes("0102030405060708");
    private static readonly byte[] presetKey = Encoding.ASCII.GetBytes("0CoJUm6Qyw8W8jud");
    private static readonly byte[] linuxapiKey = Encoding.ASCII.GetBytes("rFgB&h#%2?^eDg:Q");
    private const string base62 = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
    private const string publicKey =
        "-----BEGIN PUBLIC KEY-----\nMIGfMA0GCSqGSIb3DQEBAQUAA4GNADCBiQKBgQDgtQn2JZ34ZC28NWYpAUd98iZ37BUrX/aKzmFbt7clFSs6sXqHauqKWqdtLkF2KexO40H1YTX8z2lSgBBOAxLsvaklV8k4cBFK9snQXE9/DDaFt6Rr7iVZMldczhC0JNgTz+SHXT6CBHuX3e9SdB1Ua44oncaTWz7OBGLbCiK45wIDAQAB\n-----END PUBLIC KEY-----";
    private static readonly byte[] eapiKey = Encoding.ASCII.GetBytes("e82ckenh8dichen8");
    private static RSAParameters? _cachedPublicKey;

    public static Dictionary<string, string> WEApi(object obj)
    {
        var text = JsonSerializer.Serialize(obj);
        var secretKey = RandomNumberGenerator.GetBytes(16);
        secretKey = [.. secretKey.Select(n => (byte)base62[n % 62])];
        return new()
        {
            {
                "params",
                AesEncrypt(
                        AesEncrypt(text.ToByteArrayUtf8(), CipherMode.CBC, presetKey, iv)
                            .ToBase64String()
                            .ToByteArrayUtf8(),
                        CipherMode.CBC,
                        secretKey,
                        iv
                    )
                    .ToBase64String()
            },
            { "encSecKey", RsaEncrypt([.. secretKey.Reverse()]).ToHexStringLower() },
        };
    }

    public static Dictionary<string, string> LinuxApi(object obj)
    {
        var text = JsonSerializer.Serialize(obj);
        return new()
        {
            {
                "eparams",
                AesEncrypt(text.ToByteArrayUtf8(), CipherMode.ECB, linuxapiKey, null)
                    .ToHexStringUpper()
            },
        };
    }

    public static Dictionary<string, string> EApi(string url, object obj)
    {
        var text = JsonSerializer.Serialize(obj);
        var message = $"nobody{url}use{text}md5forencrypt";
        var digest = message.ToByteArrayUtf8().ComputeMd5().ToHexStringLower();
        var data = $"{url}-36cd479b6b5-{text}-36cd479b6b5-{digest}";
        return new()
        {
            {
                "params",
                AesEncrypt(data.ToByteArrayUtf8(), CipherMode.ECB, eapiKey, null).ToHexStringUpper()
            },
        };
    }

    public static byte[] Decrypt(byte[] cipherBuffer) =>
        AesDecrypt(cipherBuffer, CipherMode.ECB, eapiKey, null);

    private static byte[] AesEncrypt(byte[] buffer, CipherMode mode, byte[] key, byte[]? iv)
    {
        using var aes = Aes.Create();
        aes.Key = key;
        return mode switch
        {
            CipherMode.CBC => aes.EncryptCbc(buffer, iv!, PaddingMode.PKCS7),
            CipherMode.ECB => aes.EncryptEcb(buffer, PaddingMode.PKCS7),
            _ => throw new NotSupportedException($"Mode {mode} is not supported."),
        };
    }

    private static byte[] AesDecrypt(byte[] buffer, CipherMode mode, byte[] key, byte[]? iv)
    {
        using var aes = Aes.Create();
        aes.Key = key;
        return mode switch
        {
            CipherMode.CBC => aes.DecryptCbc(buffer, iv!, PaddingMode.PKCS7),
            CipherMode.ECB => aes.DecryptEcb(buffer, PaddingMode.PKCS7),
            _ => throw new NotSupportedException($"Mode {mode} is not supported."),
        };
    }

    private static byte[] RsaEncrypt(byte[] buffer)
    {
        if (_cachedPublicKey == null)
        {
            using var rsa = RSA.Create();
            rsa.ImportFromPem(publicKey);
            _cachedPublicKey = rsa.ExportParameters(false);
        }

        var rsaParameters = _cachedPublicKey.Value;
        return BigInteger
            .ModPow(
                new BigInteger(buffer, true, true),
                new BigInteger(rsaParameters.Exponent, true, true),
                new BigInteger(rsaParameters.Modulus, true, true)
            )
            .ToByteArray(true, true);
    }
}
