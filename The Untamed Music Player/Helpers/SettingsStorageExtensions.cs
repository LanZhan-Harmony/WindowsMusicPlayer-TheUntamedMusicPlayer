using Windows.Storage;
using Windows.Storage.Streams;

namespace The_Untamed_Music_Player.Helpers;

public static class SettingsStorageExtensions
{
    private const string FileExtension = ".json";

    public static bool IsRoamingStorageAvailable(this ApplicationData appData)
    {
        return appData.RoamingStorageQuota == 0;
    }

    public static async Task SaveAsync<T>(this StorageFolder folder, string name, T content)
    {
        var file = await folder.CreateFileAsync(
            GetFileName(name),
            CreationCollisionOption.ReplaceExisting
        );
        var fileContent = await Json.StringifyAsync(content!);

        await FileIO.WriteTextAsync(file, fileContent);
    }

    public static async Task<T?> ReadAsync<T>(this StorageFolder folder, string name)
    {
        if (!File.Exists(Path.Combine(folder.Path, GetFileName(name))))
        {
            return default;
        }

        var file = await folder.GetFileAsync($"{name}.json");
        var fileContent = await FileIO.ReadTextAsync(file);

        return await Json.ToObjectAsync<T>(fileContent);
    }

    public static async Task SaveAsync<T>(
        this ApplicationDataContainer settings,
        string key,
        T value
    )
    {
        settings.SaveString(key, await Json.StringifyAsync(value!));
    }

    public static void SaveString(this ApplicationDataContainer settings, string key, string value)
    {
        settings.Values[key] = value;
    }

    public static async Task<T?> ReadAsync<T>(this ApplicationDataContainer settings, string key)
    {
        object? obj;

        if (settings.Values.TryGetValue(key, out obj))
        {
            return await Json.ToObjectAsync<T>((string)obj);
        }

        return default;
    }

    public static async Task<StorageFile> SaveFileAsync(
        this StorageFolder folder,
        byte[] content,
        string fileName,
        CreationCollisionOption options = CreationCollisionOption.ReplaceExisting
    )
    {
        ArgumentNullException.ThrowIfNull(content);

        if (string.IsNullOrEmpty(fileName))
        {
            throw new ArgumentException(
                "File name is null or empty. Specify a valid file name",
                nameof(fileName)
            );
        }

        var storageFile = await folder.CreateFileAsync(fileName, options);
        await FileIO.WriteBytesAsync(storageFile, content);
        return storageFile;
    }

    public static async Task<byte[]?> ReadFileAsync(this StorageFolder folder, string fileName)
    {
        var item = await folder.TryGetItemAsync(fileName).AsTask().ConfigureAwait(false);

        if ((item is not null) && item.IsOfType(StorageItemTypes.File))
        {
            var storageFile = await folder.GetFileAsync(fileName);
            var content = await storageFile.ReadBytesAsync();
            return content;
        }

        return null;
    }

    public static async Task<byte[]?> ReadBytesAsync(this StorageFile file)
    {
        if (file is not null)
        {
            using IRandomAccessStream stream = await file.OpenReadAsync();
            using var reader = new DataReader(stream.GetInputStreamAt(0));
            await reader.LoadAsync((uint)stream.Size);
            var bytes = new byte[stream.Size];
            reader.ReadBytes(bytes);
            return bytes;
        }

        return null;
    }

    private static string GetFileName(string name)
    {
        return string.Concat(name, FileExtension);
    }
}
