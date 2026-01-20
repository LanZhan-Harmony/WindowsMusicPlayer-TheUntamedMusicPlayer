using Windows.Storage;
using Windows.Storage.Streams;

namespace UntamedMusicPlayer.Helpers;

public static class SettingsStorageExtensions
{
    private const string FileExtension = ".json";

    extension(ApplicationData appData)
    {
        public bool IsRoamingStorageAvailable()
        {
            return appData.RoamingStorageQuota == 0;
        }
    }

    extension(StorageFolder folder)
    {
        public async Task<T?> ReadAsync<T>(string name)
        {
            if (!File.Exists(Path.Combine(folder.Path, GetFileName(name))))
            {
                return default;
            }

            var file = await folder.GetFileAsync($"{name}.json");
            var fileContent = await FileIO.ReadTextAsync(file);

            return await Json.ToObjectAsync<T>(fileContent);
        }

        public async Task SaveAsync<T>(string name, T content)
        {
            var file = await folder.CreateFileAsync(
                GetFileName(name),
                CreationCollisionOption.ReplaceExisting
            );
            var fileContent = await Json.StringifyAsync(content!);

            await FileIO.WriteTextAsync(file, fileContent);
        }

        public async Task<byte[]?> ReadFileAsync(string fileName)
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

        public async Task<StorageFile> SaveFileAsync(
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
    }

    extension(StorageFile file)
    {
        public async Task<byte[]?> ReadBytesAsync()
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
    }

    extension(ApplicationDataContainer settings)
    {
        public async Task<T?> ReadAsync<T>(string key)
        {
            object? obj;

            if (settings.Values.TryGetValue(key, out obj))
            {
                return await Json.ToObjectAsync<T>((string)obj);
            }

            return default;
        }

        public async Task SaveAsync<T>(string key, T value)
        {
            settings.SaveString(key, await Json.StringifyAsync(value!));
        }

        public void SaveString(string key, string value)
        {
            settings.Values[key] = value;
        }
    }

    private static string GetFileName(string name)
    {
        return string.Concat(name, FileExtension);
    }
}
