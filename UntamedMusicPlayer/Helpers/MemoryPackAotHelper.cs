using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using MemoryPack;
using MemoryPack.Formatters;
using UntamedMusicPlayer.Contracts.Models;
using UntamedMusicPlayer.Models;
using UntamedMusicPlayer.OnlineAPIs.CloudMusicAPI;
using UntamedMusicPlayer.Playback;

namespace UntamedMusicPlayer.Helpers;

public class MemoryPackAotHelper
{
    public static void RegisterFormatters()
    {
        // 为 [MemoryPackable] 类型显式注册 MemoryPackableFormatter 以支持 NativeAOT
        // 这样可以避开 MemoryPack 内部探测时使用的反射（容易因 NativeAOT 裁剪而失败）
        Register<BriefLocalSongInfo>();
        Register<BriefUnknownSongInfo>();
        Register<BriefCloudOnlineSongInfo>();
        Register<LocalAlbumInfo>();
        Register<LocalArtistInfo>();
        Register<PlaylistInfo>();
        Register<IndexedPlaylistSong>();
        Register<IndexedPlayQueueSong>();

        // 接口类型特殊处理
        RuntimeHelpers.RunClassConstructor(typeof(IBriefSongInfoBase).TypeHandle);

        // 显式注册集合类型格式化器，以解决 NativeAOT 中的反射和修剪问题
        MemoryPackFormatterProvider.Register(new ConcurrentBagFormatter<BriefLocalSongInfo>());
        MemoryPackFormatterProvider.Register(
            new ConcurrentDictionaryFormatter<string, LocalAlbumInfo>()
        );
        MemoryPackFormatterProvider.Register(
            new ConcurrentDictionaryFormatter<string, LocalArtistInfo>()
        );
        MemoryPackFormatterProvider.Register(new ConcurrentDictionaryFormatter<string, byte>());
        MemoryPackFormatterProvider.Register(
            new ObservableCollectionFormatter<IndexedPlayQueueSong>()
        );
        MemoryPackFormatterProvider.Register(
            new ObservableCollectionFormatter<IndexedPlaylistSong>()
        );
        MemoryPackFormatterProvider.Register(new ListFormatter<PlaylistInfo>());
        MemoryPackFormatterProvider.Register(new ListFormatter<string>());
        MemoryPackFormatterProvider.Register(new DictionaryFormatter<string, string>());
        MemoryPackFormatterProvider.Register(new HashSetFormatter<string>());
    }

    [UnconditionalSuppressMessage(
        "Trimming",
        "IL2059",
        Justification = "T is annotated with DynamicallyAccessedMembers(All) which preserves the static constructor."
    )]
    private static void Register<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T
    >()
        where T : class, IMemoryPackable<T>
    {
        // 运行静态构造函数以激活内部注册逻辑
        RuntimeHelpers.RunClassConstructor(typeof(T).TypeHandle);
        // 同时提供显式格式化器以防万一
        MemoryPackFormatterProvider.Register(new MemoryPackableFormatter<T>());
    }
}
