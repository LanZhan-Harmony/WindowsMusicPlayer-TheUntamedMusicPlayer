#include "pch.h"

#include "bass_audio_engine_exports.h"

namespace
{
    struct EngineState final
    {
        HSTREAM mainHandle{};
        HSTREAM fxHandle{};
        bool bassInitialized{};
        bool wasapiInitialized{};
        bool pluginsLoaded{};
        BassAudioEngineCallback playbackEndedCallback{};
        BassAudioEngineCallback playbackFailedCallback{};
    };

    EngineState g_engine{};
    std::mutex g_engineMutex{};

    [[nodiscard]] std::wstring GetAppDirectory()
    {
        std::array<wchar_t, MAX_PATH> buffer{};
        DWORD length = GetModuleFileNameW(nullptr, buffer.data(), static_cast<DWORD>(buffer.size()));
        if (length == 0) [[unlikely]]
        {
            return {};
        }

        std::filesystem::path appPath{std::wstring_view{buffer.data(), length}};
        return appPath.parent_path().wstring();
    }

    void LoadBassPlugins()
    {
        if (g_engine.pluginsLoaded) [[unlikely]]
        {
            return;
        }

        const std::wstring appDirectory = GetAppDirectory();
        if (appDirectory.empty()) [[unlikely]]
        {
            return;
        }

        constexpr std::array pluginNames{
            L"bassape.dll",
            L"basscd.dll",
            L"bassdsd.dll",
            L"bassflac.dll",
            L"basshls.dll",
            L"bassmidi.dll",
            L"bassopus.dll",
            L"basswebm.dll",
            L"basswv.dll",
        };

        const std::filesystem::path baseDir{appDirectory};
        for (const wchar_t *const name : pluginNames)
        {
            BASS_PluginLoad((baseDir / name).c_str(), 0);
        }

        g_engine.pluginsLoaded = true;
    }

    [[nodiscard]] bool EnsureBassInitialized()
    {
        if (g_engine.bassInitialized) [[likely]]
        {
            return true;
        }

        if (!BASS_Init(-1, 44100, 0, nullptr, nullptr)) [[unlikely]]
        {
            return false;
        }

        g_engine.bassInitialized = true;
        LoadBassPlugins();
        return true;
    }

    void FreeStreamsUnsafe()
    {
        if (BASS_WASAPI_IsStarted())
        {
            BASS_WASAPI_Stop(TRUE);
        }

        if (g_engine.wasapiInitialized)
        {
            BASS_WASAPI_Free();
            g_engine.wasapiInitialized = false;
        }

        if (g_engine.fxHandle != 0) [[likely]]
        {
            BASS_StreamFree(g_engine.fxHandle);
            g_engine.fxHandle = 0;
        }

        if (g_engine.mainHandle != 0) [[likely]]
        {
            BASS_StreamFree(g_engine.mainHandle);
            g_engine.mainHandle = 0;
        }
    }

    void CALLBACK OnPlaybackEndedSync(HSYNC, DWORD, DWORD, void *)
    {
        const BassAudioEngineCallback callback = g_engine.playbackEndedCallback;
        if (callback != nullptr) [[likely]]
        {
            callback();
        }
    }

    void CALLBACK OnPlaybackFailedSync(HSYNC, DWORD, DWORD, void *)
    {
        const BassAudioEngineCallback callback = g_engine.playbackFailedCallback;
        if (callback != nullptr) [[likely]]
        {
            callback();
        }
    }

    DWORD CALLBACK WasapiProc(void *buffer, DWORD length, void *)
    {
        if (g_engine.fxHandle == 0) [[unlikely]]
        {
            return 0;
        }

        return BASS_ChannelGetData(g_engine.fxHandle, buffer, length);
    }

    [[nodiscard]] HSTREAM CreateMainStream(const wchar_t *path, BOOL isOnline)
    {
        if (isOnline)
        {
            constexpr DWORD streamFlags = BASS_UNICODE | BASS_SAMPLE_FLOAT | BASS_STREAM_DECODE;
            return BASS_StreamCreateURL(reinterpret_cast<const char *>(path), 0, streamFlags, nullptr, nullptr);
        }

        constexpr DWORD streamFlags = BASS_UNICODE | BASS_SAMPLE_FLOAT | BASS_ASYNCFILE | BASS_STREAM_DECODE;
        return BASS_StreamCreateFile(FALSE, path, 0, 0, streamFlags);
    }
}

void WINAPI BaeSetCallbacks(BassAudioEngineCallback playbackEndedCallback, BassAudioEngineCallback playbackFailedCallback)
{
    const std::lock_guard lock{g_engineMutex};
    g_engine.playbackEndedCallback = playbackEndedCallback;
    g_engine.playbackFailedCallback = playbackFailedCallback;
}

BOOL WINAPI BaeInitialize()
{
    const std::lock_guard lock{g_engineMutex};
    return EnsureBassInitialized() ? TRUE : FALSE;
}

void WINAPI BaeShutdown()
{
    const std::lock_guard lock{g_engineMutex};

    FreeStreamsUnsafe();

    if (g_engine.bassInitialized) [[likely]]
    {
        BASS_Free();
        g_engine.bassInitialized = false;
    }

    g_engine.playbackEndedCallback = nullptr;
    g_engine.playbackFailedCallback = nullptr;
}

BOOL WINAPI BaeLoadSong(const wchar_t *path, BOOL isOnline, BOOL isExclusiveMode, double volume, double speed, double *totalSeconds)
{
    const std::lock_guard lock{g_engineMutex};

    FreeStreamsUnsafe();

    if (!EnsureBassInitialized()) [[unlikely]]
    {
        return FALSE;
    }

    g_engine.mainHandle = CreateMainStream(path, isOnline);
    if (g_engine.mainHandle == 0 && BASS_ErrorGetCode() == BASS_ERROR_INIT) [[unlikely]]
    {
        g_engine.bassInitialized = false;
        if (!EnsureBassInitialized()) [[unlikely]]
        {
            return FALSE;
        }

        g_engine.mainHandle = CreateMainStream(path, isOnline);
    }

    if (g_engine.mainHandle == 0) [[unlikely]]
    {
        return FALSE;
    }

    const DWORD tempoFlags = isExclusiveMode ? BASS_STREAM_DECODE : BASS_FX_FREESOURCE;
    g_engine.fxHandle = BASS_FX_TempoCreate(g_engine.mainHandle, tempoFlags);
    if (g_engine.fxHandle == 0) [[unlikely]]
    {
        BASS_StreamFree(g_engine.mainHandle);
        g_engine.mainHandle = 0;
        return FALSE;
    }

    BASS_ChannelSetSync(g_engine.fxHandle, BASS_SYNC_END, 0, OnPlaybackEndedSync, nullptr);
    BASS_ChannelSetSync(g_engine.fxHandle, BASS_SYNC_STALL, 0, OnPlaybackFailedSync, nullptr);

    const float tempoPercent = static_cast<float>((speed - 1.0) * 100.0);
    BASS_ChannelSetAttribute(g_engine.fxHandle, BASS_ATTRIB_TEMPO, tempoPercent);
    BASS_ChannelSetAttribute(g_engine.fxHandle, BASS_ATTRIB_VOL, static_cast<float>(volume));

    if (totalSeconds != nullptr) [[likely]]
    {
        const QWORD lengthBytes = BASS_ChannelGetLength(g_engine.fxHandle, BASS_POS_BYTE);
        *totalSeconds = BASS_ChannelBytes2Seconds(g_engine.fxHandle, lengthBytes);
    }

    return TRUE;
}

void WINAPI BaeStop()
{
    const std::lock_guard lock{g_engineMutex};
    FreeStreamsUnsafe();
}

BOOL WINAPI BaePlay(BOOL isExclusiveMode)
{
    const std::lock_guard lock{g_engineMutex};

    if (g_engine.fxHandle == 0) [[unlikely]]
    {
        return FALSE;
    }

    if (isExclusiveMode)
    {
        if (BASS_WASAPI_IsStarted()) [[unlikely]]
        {
            return TRUE;
        }

        if (g_engine.wasapiInitialized)
        {
            return BASS_WASAPI_Start() ? TRUE : FALSE;
        }

        BASS_CHANNELINFO channelInfo{};
        if (!BASS_ChannelGetInfo(g_engine.fxHandle, &channelInfo)) [[unlikely]]
        {
            return FALSE;
        }

        // Use AUTOFORMAT flag to let BASS handle sample rate conversion automatically
        // Try to initialize with the original frequency first
        const DWORD baseFlags = BASS_WASAPI_EXCLUSIVE | BASS_WASAPI_AUTOFORMAT;
        if (!BASS_WASAPI_Init(-1, channelInfo.freq, channelInfo.chans, baseFlags | BASS_WASAPI_EVENT, 0.1F, 0, WasapiProc, nullptr))
        {
            // If it fails, try not using the EVENT flag
            if (!BASS_WASAPI_Init(-1, channelInfo.freq, channelInfo.chans, baseFlags, 0.1F, 0.025F, WasapiProc, nullptr))
            {
                return FALSE;
            }
        }

        g_engine.wasapiInitialized = true;
        return BASS_WASAPI_Start() ? TRUE : FALSE;
    }

    if (BASS_ChannelIsActive(g_engine.fxHandle) == BASS_ACTIVE_PLAYING) [[unlikely]]
    {
        return TRUE;
    }

    if (BASS_ChannelPlay(g_engine.fxHandle, FALSE)) [[likely]]
    {
        return TRUE;
    }

    if (BASS_ErrorGetCode() == BASS_ERROR_START && BASS_Start())
    {
        return BASS_ChannelPlay(g_engine.fxHandle, FALSE) ? TRUE : FALSE;
    }

    return FALSE;
}

void WINAPI BaePause(BOOL isExclusiveMode)
{
    const std::lock_guard lock{g_engineMutex};

    if (g_engine.fxHandle == 0) [[unlikely]]
    {
        return;
    }

    if (isExclusiveMode)
    {
        if (BASS_WASAPI_IsStarted()) [[likely]]
        {
            BASS_WASAPI_Stop(FALSE);
        }

        return;
    }

    BASS_ChannelPause(g_engine.fxHandle);
}

void WINAPI BaeSetSpeed(double speed)
{
    const std::lock_guard lock{g_engineMutex};

    if (g_engine.fxHandle == 0) [[unlikely]]
    {
        return;
    }

    const float tempoPercent = static_cast<float>((speed - 1.0) * 100.0);
    BASS_ChannelSetAttribute(g_engine.fxHandle, BASS_ATTRIB_TEMPO, tempoPercent);
}

void WINAPI BaeSetVolume(double volume)
{
    const std::lock_guard lock{g_engineMutex};

    if (g_engine.fxHandle == 0) [[unlikely]]
    {
        return;
    }

    BASS_ChannelSetAttribute(g_engine.fxHandle, BASS_ATTRIB_VOL, static_cast<float>(volume));
}

double WINAPI BaeGetPositionSeconds()
{
    const std::lock_guard lock{g_engineMutex};

    if (g_engine.fxHandle == 0) [[unlikely]]
    {
        return -1.0;
    }

    const QWORD positionBytes = BASS_ChannelGetPosition(g_engine.fxHandle, BASS_POS_BYTE);
    return BASS_ChannelBytes2Seconds(g_engine.fxHandle, positionBytes);
}

BOOL WINAPI BaeSetPositionSeconds(double targetSeconds)
{
    const std::lock_guard lock{g_engineMutex};

    if (g_engine.fxHandle == 0) [[unlikely]]
    {
        return FALSE;
    }

    const QWORD targetBytes = BASS_ChannelSeconds2Bytes(g_engine.fxHandle, targetSeconds);
    BOOL result = BASS_ChannelSetPosition(g_engine.fxHandle, targetBytes, BASS_POS_BYTE);

    if (!result && BASS_ErrorGetCode() == BASS_ERROR_POSITION) [[unlikely]]
    {
        for (int retryCount = 0; retryCount < 5 && !result; ++retryCount)
        {
            std::this_thread::sleep_for(std::chrono::milliseconds{200});
            result = BASS_ChannelSetPosition(g_engine.fxHandle, targetBytes, BASS_POS_BYTE);
        }
    }

    return result ? TRUE : FALSE;
}

int WINAPI BaeGetLastError()
{
    return BASS_ErrorGetCode();
}

BOOL WINAPI BaeIsLastErrorBusy()
{
    return BASS_ErrorGetCode() == BASS_ERROR_BUSY ? TRUE : FALSE;
}
