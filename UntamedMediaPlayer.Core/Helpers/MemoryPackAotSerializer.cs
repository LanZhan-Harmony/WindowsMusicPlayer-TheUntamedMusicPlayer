using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using MemoryPack;
using MemoryPack.Formatters;

namespace UntamedMediaPlayer.Core.Helpers;

/// <summary>
/// NativeAOT-safe MemoryPack serialization wrapper.
/// </summary>
/// <remarks>
/// <para>
/// Root cause: MemoryPack's IMemoryPackFormatter&lt;T&gt;.Serialize&lt;TBufferWriter&gt;() is a Generic Virtual Method (GVM).
/// In NativeAOT, when TBufferWriter is a reference type (such as the internal ReusableLinkedArrayBufferWriter),
/// all reference-type generic instances share the same machine code (shared generics).
/// When formatter chains for multiple complex types, especially types containing [MemoryPackUnion] such as IBriefSongInfoBase, overlap,
/// the shared GVM dispatch table can conflict, resulting in a null function pointer call (0xc0000005).
/// </para>
/// <para>
/// Fix: use the value type (struct) AotSafeBufferWriter as TBufferWriter,
/// forcing NativeAOT to generate fully independent, non-shared specialized machine code for the entire formatter chain.
/// Value-type generic parameters never use shared generics in NativeAOT,
/// so each formatter's Serialize&lt;AotSafeBufferWriter&gt; has its own GVM entry, fundamentally avoiding dispatch table conflicts.
/// </para>
/// <para>
/// The default initial capacity is 8 KB to cover common 1 KB to 5 KB serialized results while minimizing buffer growth and array copies.
/// </para>
/// </remarks>
internal static class MemoryPackAotSerializer
{
    static MemoryPackAotSerializer()
    {
        RegisterFormatters();
    }

    /// <summary>
    /// NativeAOT-safe serialization.
    /// Uses a struct buffer writer to avoid GVM dispatch crashes.
    /// </summary>
    internal static byte[] Serialize<T>(in T? value)
    {
        AotSafeBufferWriter bufferWriter = new(8192);
        MemoryPackSerializer.Serialize(bufferWriter, value);
        return bufferWriter.ToArray();
    }

    internal static ValueTask SerializeToStreamAsync<T>(
        Stream stream,
        T? value,
        int initialCapacity = 8192,
        CancellationToken cancellationToken = default
    )
    {
        AotSafeBufferWriter bufferWriter = new(initialCapacity);
        MemoryPackSerializer.Serialize(bufferWriter, value);
        return bufferWriter.WriteToAsync(stream, cancellationToken);
    }

    private static void RegisterFormatters()
    {
        // Explicitly register MemoryPackableFormatter for [MemoryPackable] types to support NativeAOT.
        // This avoids reflection during MemoryPack's internal discovery, which can fail after NativeAOT trimming.
        // Example: Register<BriefLocalSongInfo>();

        // Explicitly register collection formatters to address reflection and trimming issues in NativeAOT.
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
        // Run the static constructor to activate the internal registration logic.
        RuntimeHelpers.RunClassConstructor(typeof(T).TypeHandle);
        // Also provide an explicit formatter as a fallback.
        MemoryPackFormatterProvider.Register(new MemoryPackableFormatter<T>());
    }
}

/// <summary>
/// NativeAOT-safe buffer writer.
/// Uses a value type (struct) to force NativeAOT to generate specialized machine code for
/// IMemoryPackFormatter&lt;T&gt;.Serialize&lt;TBufferWriter&gt;(),
/// avoiding GVM dispatch table conflicts and crashes caused by shared generics for reference types.
/// </summary>
internal struct AotSafeBufferWriter(int initialCapacity) : IBufferWriter<byte>
{
    private byte[] _buffer = new byte[initialCapacity];
    private int _written = 0;

    public void Advance(int count)
    {
        _written += count;
    }

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

    internal readonly byte[] ToArray()
    {
        return [.. _buffer.AsSpan(0, _written)];
    }

    internal readonly ValueTask WriteToAsync(
        Stream stream,
        CancellationToken cancellationToken = default
    )
    {
        return stream.WriteAsync(_buffer.AsMemory(0, _written), cancellationToken);
    }
}
