#include "pch.h"

#include "bass_audio_engine_exports.h"

#include "bass.h"
#include "bass_fx.h"
#include "basswasapi.h"

#include <array>
#include <chrono>
#include <filesystem>
#include <mutex>
#include <string>
#include <string_view>
#include <thread>

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
        auto length = GetModuleFileNameW(nullptr, buffer.data(), static_cast<DWORD>(buffer.size()));
        if (length == 0) [[unlikely]]
        {
            return {};
        }

        auto appPath = std::filesystem::path{std::wstring_view{buffer.data(), length}};
        return appPath.parent_path().wstring();
    }

    void LoadBassPlugins()
    {
        if (g_engine.pluginsLoaded) [[unlikely]]
        {
            return;
        }

        const auto appDirectory = GetAppDirectory();
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

        for (const auto pluginName : pluginNames)
        {
            const auto fullPath = std::filesystem::path{appDirectory} / pluginName;
            BASS_PluginLoad(fullPath.c_str(), 0);
        }

        g_engine.pluginsLoaded = true;
    }

    [[nodiscard]] bool EnsureBassInitialized()
    {
        if (g_engine.bassInitialized) [[unlikely]]
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
        const auto callback = g_engine.playbackEndedCallback;
        if (callback != nullptr) [[likely]]
        {
            callback();
        }
    }

    void CALLBACK OnPlaybackFailedSync(HSYNC, DWORD, DWORD, void *)
    {
        const auto callback = g_engine.playbackFailedCallback;
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
            constexpr auto streamFlags = BASS_UNICODE | BASS_SAMPLE_FLOAT | BASS_STREAM_DECODE;
            return BASS_StreamCreateURL(reinterpret_cast<const char *>(path), 0, streamFlags, nullptr, nullptr);
        }

        constexpr auto streamFlags = BASS_UNICODE | BASS_SAMPLE_FLOAT | BASS_ASYNCFILE | BASS_STREAM_DECODE;
        return BASS_StreamCreateFile(FALSE, path, 0, 0, streamFlags);
    }
}

void WINAPI BaeSetCallbacks(BassAudioEngineCallback playbackEndedCallback, BassAudioEngineCallback playbackFailedCallback)
{
    const auto lock = std::lock_guard{g_engineMutex};
    g_engine.playbackEndedCallback = playbackEndedCallback;
    g_engine.playbackFailedCallback = playbackFailedCallback;
}

BOOL WINAPI BaeInitialize()
{
    const auto lock = std::lock_guard{g_engineMutex};
    return EnsureBassInitialized() ? TRUE : FALSE;
}

void WINAPI BaeShutdown()
{
    const auto lock = std::lock_guard{g_engineMutex};

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
    const auto lock = std::lock_guard{g_engineMutex};

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

    const auto tempoFlags = isExclusiveMode ? BASS_STREAM_DECODE : BASS_FX_FREESOURCE;
    g_engine.fxHandle = BASS_FX_TempoCreate(g_engine.mainHandle, tempoFlags);
    if (g_engine.fxHandle == 0) [[unlikely]]
    {
        BASS_StreamFree(g_engine.mainHandle);
        g_engine.mainHandle = 0;
        return FALSE;
    }

    BASS_ChannelSetSync(g_engine.fxHandle, BASS_SYNC_END, 0, OnPlaybackEndedSync, nullptr);
    BASS_ChannelSetSync(g_engine.fxHandle, BASS_SYNC_STALL, 0, OnPlaybackFailedSync, nullptr);

    const auto tempoPercent = static_cast<float>((speed - 1.0) * 100.0);
    BASS_ChannelSetAttribute(g_engine.fxHandle, BASS_ATTRIB_TEMPO, tempoPercent);
    BASS_ChannelSetAttribute(g_engine.fxHandle, BASS_ATTRIB_VOL, static_cast<float>(volume));

    if (totalSeconds != nullptr) [[likely]]
    {
        const auto lengthBytes = BASS_ChannelGetLength(g_engine.fxHandle, BASS_POS_BYTE);
        *totalSeconds = BASS_ChannelBytes2Seconds(g_engine.fxHandle, lengthBytes);
    }

    return TRUE;
}

void WINAPI BaeStop()
{
    const auto lock = std::lock_guard{g_engineMutex};
    FreeStreamsUnsafe();
}

BOOL WINAPI BaePlay(BOOL isExclusiveMode)
{
    const auto lock = std::lock_guard{g_engineMutex};

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

        const auto initFlags = BASS_WASAPI_EXCLUSIVE | BASS_WASAPI_EVENT;
        if (!BASS_WASAPI_Init(-1, channelInfo.freq, channelInfo.chans, initFlags, 0.1F, 0.025F, WasapiProc, nullptr)) [[unlikely]]
        {
            return FALSE;
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
    const auto lock = std::lock_guard{g_engineMutex};

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
    const auto lock = std::lock_guard{g_engineMutex};

    if (g_engine.fxHandle == 0) [[unlikely]]
    {
        return;
    }

    const auto tempoPercent = static_cast<float>((speed - 1.0) * 100.0);
    BASS_ChannelSetAttribute(g_engine.fxHandle, BASS_ATTRIB_TEMPO, tempoPercent);
}

void WINAPI BaeSetVolume(double volume)
{
    const auto lock = std::lock_guard{g_engineMutex};

    if (g_engine.fxHandle == 0) [[unlikely]]
    {
        return;
    }

    BASS_ChannelSetAttribute(g_engine.fxHandle, BASS_ATTRIB_VOL, static_cast<float>(volume));
}

double WINAPI BaeGetPositionSeconds()
{
    const auto lock = std::lock_guard{g_engineMutex};

    if (g_engine.fxHandle == 0) [[unlikely]]
    {
        return -1.0;
    }

    const auto positionBytes = BASS_ChannelGetPosition(g_engine.fxHandle, BASS_POS_BYTE);
    return BASS_ChannelBytes2Seconds(g_engine.fxHandle, positionBytes);
}

BOOL WINAPI BaeSetPositionSeconds(double targetSeconds)
{
    const auto lock = std::lock_guard{g_engineMutex};

    if (g_engine.fxHandle == 0) [[unlikely]]
    {
        return FALSE;
    }

    const auto targetBytes = BASS_ChannelSeconds2Bytes(g_engine.fxHandle, targetSeconds);
    auto result = BASS_ChannelSetPosition(g_engine.fxHandle, targetBytes, BASS_POS_BYTE);

    if (!result && BASS_ErrorGetCode() == BASS_ERROR_POSITION) [[unlikely]]
    {
        for (auto retryCount = 0; retryCount < 20 && !result; ++retryCount)
        {
            std::this_thread::sleep_for(std::chrono::milliseconds{100});
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
