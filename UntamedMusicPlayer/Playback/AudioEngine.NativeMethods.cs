using System.Runtime.InteropServices;

namespace UntamedMusicPlayer.Playback;

public sealed partial class AudioEngine
{
    private const string NativeLibraryName = "BassAudioEngine";

    [UnmanagedFunctionPointer(CallingConvention.Winapi)]
    private delegate void NativePlaybackCallback();

    private static partial class NativeMethods
    {
        [LibraryImport(NativeLibraryName, EntryPoint = "BaeSetCallbacks")]
        internal static partial void SetCallbacks(
            NativePlaybackCallback? playbackEndedCallback,
            NativePlaybackCallback? playbackFailedCallback
        );

        [LibraryImport(NativeLibraryName, EntryPoint = "BaeInitialize")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static partial bool Initialize();

        [LibraryImport(NativeLibraryName, EntryPoint = "BaeShutdown")]
        internal static partial void Shutdown();

        [LibraryImport(
            NativeLibraryName,
            EntryPoint = "BaeLoadSong",
            StringMarshalling = StringMarshalling.Utf16
        )]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static partial bool LoadSong(
            string path,
            [MarshalAs(UnmanagedType.Bool)] bool isOnline,
            [MarshalAs(UnmanagedType.Bool)] bool isExclusiveMode,
            double volume,
            double speed,
            out double totalSeconds
        );

        [LibraryImport(NativeLibraryName, EntryPoint = "BaeStop")]
        internal static partial void Stop();

        [LibraryImport(NativeLibraryName, EntryPoint = "BaePlay")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static partial bool Play([MarshalAs(UnmanagedType.Bool)] bool isExclusiveMode);

        [LibraryImport(NativeLibraryName, EntryPoint = "BaePause")]
        internal static partial void Pause([MarshalAs(UnmanagedType.Bool)] bool isExclusiveMode);

        [LibraryImport(NativeLibraryName, EntryPoint = "BaeSetSpeed")]
        internal static partial void SetSpeed(double speed);

        [LibraryImport(NativeLibraryName, EntryPoint = "BaeSetVolume")]
        internal static partial void SetVolume(double volume);

        [LibraryImport(NativeLibraryName, EntryPoint = "BaeGetPositionSeconds")]
        internal static partial double GetPositionSeconds();

        [LibraryImport(NativeLibraryName, EntryPoint = "BaeSetPositionSeconds")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static partial bool SetPositionSeconds(double targetSeconds);

        [LibraryImport(NativeLibraryName, EntryPoint = "BaeGetLastError")]
        internal static partial int GetLastError();

        [LibraryImport(NativeLibraryName, EntryPoint = "BaeIsLastErrorBusy")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static partial bool IsLastErrorBusy();
    }
}
