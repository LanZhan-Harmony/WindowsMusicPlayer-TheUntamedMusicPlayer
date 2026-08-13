using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using MemoryPack;
using MemoryPack.Formatters;
using UntamedMusicPlayer.Contracts.Models;
using UntamedMusicPlayer.Models;
using UntamedMusicPlayer.OnlineAPIs.CloudMusicAPI;

namespace UntamedMusicPlayer.Helpers;

public sealed class MemoryPackAotHelper
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

/// <summary>
/// NativeAOT 安全的缓冲写入器
/// 使用值类型（struct）迫使 NativeAOT 为 IMemoryPackFormatter<T>.Serialize<TBufferWriter>()
/// 这一 GVM（Generic Virtual Method）生成专门的机器码，
/// 避免引用类型共享泛型（shared generics）导致的 GVM 分派表冲突/崩溃。
/// </summary>
public struct AotSafeBufferWriter(int initialCapacity) : IBufferWriter<byte>
{
    private byte[] _buffer = new byte[initialCapacity];
    private int _written = 0;

    public void Advance(int count) => _written += count;

    public Memory<byte> GetMemory(int sizeHint = 0)
    {
        EnsureCapacity(sizeHint);
        return _buffer.AsMemory(_written);
    }

    public Span<byte> GetSpan(int sizeHint = 0)
    {
        EnsureCapacity(sizeHint);
        return _buffer.AsSpan(_written);
    }

    private void EnsureCapacity(int sizeHint)
    {
        if (sizeHint <= 0)
        {
            sizeHint = 256;
        }
        if (_written + sizeHint > _buffer.Length)
        {
            Array.Resize(ref _buffer, Math.Max(_buffer.Length * 2, _written + sizeHint));
        }
    }

    public readonly byte[] ToArray() => _buffer.AsSpan(0, _written).ToArray();

    public readonly ValueTask WriteToAsync(
        Stream stream,
        CancellationToken cancellationToken = default
    )
    {
        return stream.WriteAsync(_buffer.AsMemory(0, _written), cancellationToken);
    }
}

/// <summary>
/// NativeAOT 安全的 MemoryPack 序列化包装器
/// </summary>
/// <remarks>
/// <para>
/// 问题根因: MemoryPack 的 IMemoryPackFormatter<T>.Serialize<TBufferWriter>()是一个 Generic Virtual Method (GVM)。
/// 在 NativeAOT 中，当 TBufferWriter 是引用类型（如内部的 ReusableLinkedArrayBufferWriter）时，
/// 所有引用类型泛型实例共享同一份机器码（shared generics）。
/// 当多个复杂类型（特别是包含 [MemoryPackUnion] 的类型如 IBriefSongInfoBase）的 formatter 链交叉执行时，
/// 共享的 GVM 分派表会发生冲突，导致空函数指针调用 (0xc0000005)。
/// </para>
/// <para>
/// 修复原理: 使用值类型 (struct) 的 AotSafeBufferWriter 作为 TBufferWriter，
/// 迫使 NativeAOT 为整个 formatter 链生成完全独立的、非共享的专用机器码。
/// 值类型泛型参数在 NativeAOT 中永远不会使用 shared generics，
/// 因此每个 formatter 的 Serialize<AotSafeBufferWriter> 都有自己独立的 GVM 表项，从根本上避免了分派表冲突。
/// </para>
/// <para>
/// 当前默认初始容量设为 8KB，用于覆盖常见的 1KB~5KB 序列化结果，并尽量减少扩容和数组拷贝次数。
/// </para>
/// </remarks>
public static class MemoryPackAotSerializer
{
    /// <summary>
    /// NativeAOT 安全的序列化
    /// 使用 struct BufferWriter 避免 GVM 分派崩溃
    /// </summary>
    public static byte[] Serialize<T>(in T? value)
    {
        var bufferWriter = new AotSafeBufferWriter(8192);
        MemoryPackSerializer.Serialize(bufferWriter, value);
        return bufferWriter.ToArray();
    }

    public static ValueTask SerializeToStreamAsync<T>(
        Stream stream,
        T? value,
        int initialCapacity = 8192,
        CancellationToken cancellationToken = default
    )
    {
        var bufferWriter = new AotSafeBufferWriter(initialCapacity);
        MemoryPackSerializer.Serialize(bufferWriter, value);
        return bufferWriter.WriteToAsync(stream, cancellationToken);
    }
}
