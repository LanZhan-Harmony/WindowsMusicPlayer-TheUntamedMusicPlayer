#pragma warning disable

using System.Numerics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace The_Untamed_Music_Player.OnlineAPIs.CloudMusicAPI.Utils;
internal static class Crypto
{
    private static readonly byte[] iv = Encoding.ASCII.GetBytes("0102030405060708");
    private static readonly byte[] presetKey = Encoding.ASCII.GetBytes("0CoJUm6Qyw8W8jud");
    private static readonly byte[] linuxapiKey = Encoding.ASCII.GetBytes("rFgB&h#%2?^eDg:Q");
    private const string base62 = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
    private const string publicKey = "-----BEGIN PUBLIC KEY-----\nMIGfMA0GCSqGSIb3DQEBAQUAA4GNADCBiQKBgQDgtQn2JZ34ZC28NWYpAUd98iZ37BUrX/aKzmFbt7clFSs6sXqHauqKWqdtLkF2KexO40H1YTX8z2lSgBBOAxLsvaklV8k4cBFK9snQXE9/DDaFt6Rr7iVZMldczhC0JNgTz+SHXT6CBHuX3e9SdB1Ua44oncaTWz7OBGLbCiK45wIDAQAB\n-----END PUBLIC KEY-----";
    private static readonly byte[] eapiKey = Encoding.ASCII.GetBytes("e82ckenh8dichen8");

    private static RSAParameters? _cachedPublicKey;

    public static Dictionary<string, string> WEApi(object @object)
    {
        string text;
        byte[] secretKey;

        text = JsonSerializer.Serialize(@object);
        secretKey = new Random().RandomBytes(16);
        secretKey = [.. secretKey.Select(n => (byte)base62[n % 62])];
        return new Dictionary<string, string> {
            { "params", AesEncrypt(AesEncrypt(text.ToByteArrayUtf8(), CipherMode.CBC, presetKey, iv).ToBase64String().ToByteArrayUtf8(), CipherMode.CBC, secretKey, iv).ToBase64String() },
            { "encSecKey", RsaEncrypt([.. secretKey.AsEnumerable().Reverse()]/*, publicKey*/).ToHexStringLower() }
        };
    }

    public static Dictionary<string, string> LinuxApi(object @object)
    {
        string text;

        text = JsonSerializer.Serialize(@object);
        return new Dictionary<string, string> {
            { "eparams", AesEncrypt(text.ToByteArrayUtf8(), CipherMode.ECB, linuxapiKey, null).ToHexStringUpper() }
        };
    }

    public static Dictionary<string, string> EApi(string url, object @object)
    {
        string text;
        string message;
        string digest;
        string data;

        text = JsonSerializer.Serialize(@object);
        message = $"nobody{url}use{text}md5forencrypt";
        digest = message.ToByteArrayUtf8().ComputeMd5().ToHexStringLower();
        data = $"{url}-36cd479b6b5-{text}-36cd479b6b5-{digest}";
        return new Dictionary<string, string> {
            { "params", AesEncrypt(data.ToByteArrayUtf8(), CipherMode.ECB, eapiKey, null).ToHexStringUpper() }
        };
    }

    public static byte[] Decrypt(byte[] cipherBuffer) => AesDecrypt(cipherBuffer, CipherMode.ECB, eapiKey, null);

    private static byte[] AesEncrypt(byte[] buffer, CipherMode mode, byte[] key, byte[] iv)
    {
        using var aes = Aes.Create();
        aes.BlockSize = 128;
        aes.Key = key;
        if (iv is not null)
        {
            aes.IV = iv;
        }

        aes.Mode = mode;
        using var cryptoTransform = aes.CreateEncryptor();
        return cryptoTransform.TransformFinalBlock(buffer, 0, buffer.Length);
    }

    private static byte[] AesDecrypt(byte[] buffer, CipherMode mode, byte[] key, byte[] iv)
    {
        using var aes = Aes.Create();
        aes.BlockSize = 128;
        aes.Key = key;
        if (iv is not null)
        {
            aes.IV = iv;
        }

        aes.Mode = mode;
        using var cryptoTransform = aes.CreateDecryptor();
        return cryptoTransform.TransformFinalBlock(buffer, 0, buffer.Length);
    }

    private static byte[] RsaEncrypt(byte[] buffer/*, string key*/)
    {
        RSAParameters rsaParameters;

        _cachedPublicKey ??= ParsePublicKey(publicKey);

        rsaParameters = _cachedPublicKey.Value;
        return BigInteger.ModPow(
            new BigInteger(buffer, true, true),
            new BigInteger(rsaParameters.Exponent, true, true),
            new BigInteger(rsaParameters.Modulus, true, true)
            ).ToByteArray(true, true);
    }

    private static RSAParameters ParsePublicKey(string _publicKey)
    {
        _publicKey = _publicKey.Replace("\n", string.Empty);
        _publicKey = _publicKey[26..^24];
        using var _stream = new MemoryStream(Convert.FromBase64String(_publicKey));
        using var _reader = new BinaryReader(_stream);
        ushort _i16;
        byte[] _oid;
        byte _i8;
        byte _low;
        byte _high;
        int _modulusLength;
        byte[] _modulus;
        int _exponentLength;
        byte[] _exponent;

        _i16 = _reader.ReadUInt16();
        if (_i16 == 0x8130)
        {
            _reader.ReadByte();
        }
        else if (_i16 == 0x8230)
        {
            _reader.ReadInt16();
        }
        else
        {
            throw new ArgumentException(null, nameof(_publicKey));
        }

        _oid = _reader.ReadBytes(15);
        if (!_oid.SequenceEqual(new byte[] { 0x30, 0x0D, 0x06, 0x09, 0x2A, 0x86, 0x48, 0x86, 0xF7, 0x0D, 0x01, 0x01, 0x01, 0x05, 0x00 }))
        {
            throw new ArgumentException(null, nameof(_publicKey));
        }

        _i16 = _reader.ReadUInt16();
        if (_i16 == 0x8103)
        {
            _reader.ReadByte();
        }
        else if (_i16 == 0x8203)
        {
            _reader.ReadInt16();
        }
        else
        {
            throw new ArgumentException(null, nameof(_publicKey));
        }

        _i8 = _reader.ReadByte();
        if (_i8 != 0x00)
        {
            throw new ArgumentException(null, nameof(_publicKey));
        }

        _i16 = _reader.ReadUInt16();
        if (_i16 == 0x8130)
        {
            _reader.ReadByte();
        }
        else if (_i16 == 0x8230)
        {
            _reader.ReadInt16();
        }
        else
        {
            throw new ArgumentException(null, nameof(_publicKey));
        }

        _i16 = _reader.ReadUInt16();
        if (_i16 == 0x8102)
        {
            _high = 0;
            _low = _reader.ReadByte();
        }
        else if (_i16 == 0x8202)
        {
            _high = _reader.ReadByte();
            _low = _reader.ReadByte();
        }
        else
        {
            throw new ArgumentException(null, nameof(_publicKey));
        }

        _modulusLength = BitConverter.ToInt32([_low, _high, 0x00, 0x00], 0);
        if (_reader.PeekChar() == 0x00)
        {
            _reader.ReadByte();
            _modulusLength -= 1;
        }
        _modulus = _reader.ReadBytes(_modulusLength);
        if (_reader.ReadByte() != 0x02)
        {
            throw new ArgumentException(null, nameof(_publicKey));
        }

        _exponentLength = _reader.ReadByte();
        _exponent = _reader.ReadBytes(_exponentLength);
        return new RSAParameters
        {
            Modulus = _modulus,
            Exponent = _exponent
        };
    }
}
