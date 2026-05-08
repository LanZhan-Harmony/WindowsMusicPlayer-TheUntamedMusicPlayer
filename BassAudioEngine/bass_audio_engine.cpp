#include "pch.h"

#include "bass_audio_engine_exports.h"

namespace
{
	// =====================================================================
	// WASAPI Exclusive Mode Audio pipeline architecture:
	//
	//   [Source decode stream]
	//       -> [BASS_FX tempo stream] (decode mode, handles speed/pitch)
	//           -> [BASSmix mixer stream] (decode mode, handles resampling + volume)
	//               -> [WASAPI exclusive output] (native device format)
	//
	// In shared mode, the mixer is skipped and fxHandle plays directly.
	//
	// Volume in exclusive mode is applied via BASS_ATTRIB_VOLDSP on the mixer stream.
	// This is a DSP-level volume that modifies PCM samples before they reach WASAPI, avoiding system endpoint volume issues.
	// =====================================================================

	struct EngineState final
	{
		HSTREAM mainHandle{};  // Source decode stream
		HSTREAM fxHandle{};	   // BASS_FX tempo stream (always decode in exclusive mode)
		HSTREAM mixerHandle{}; // BASSmix mixer stream (exclusive mode only, decode mode)
		bool bassInitialized{};
		bool wasapiInitialized{};
		bool pluginsLoaded{};
		bool exclusiveMode{};
		float currentVolume{1.0F};
		BassAudioEngineCallback playbackEndedCallback{};
		BassAudioEngineCallback playbackFailedCallback{};
	};

	EngineState g_engine{};
	std::mutex g_engineMutex{};

	// -----------------------------------------------------------------
	// Utility: application directory for plugin loading
	// -----------------------------------------------------------------

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

	// -----------------------------------------------------------------
	// BASS initialization
	// -----------------------------------------------------------------

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

	// -----------------------------------------------------------------
	// Sync callbacks
	// -----------------------------------------------------------------

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

	// -----------------------------------------------------------------
	// WASAPI output callback — pulls PCM data from the mixer stream
	// -----------------------------------------------------------------

	DWORD CALLBACK WasapiProc(void *buffer, DWORD length, void *)
	{
		const HSTREAM source = g_engine.mixerHandle;
		if (source == 0) [[unlikely]]
		{
			return 0;
		}

		const DWORD got = BASS_ChannelGetData(source, buffer, length);
		if (got == -1) [[unlikely]]
		{
			return 0;
		}

		return got;
	}

	// -----------------------------------------------------------------
	// Stream creation helpers
	// -----------------------------------------------------------------

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

	// -----------------------------------------------------------------
	// Query the WASAPI default device's native format
	// -----------------------------------------------------------------

	struct DeviceFormat
	{
		DWORD freq{};
		DWORD chans{};
	};

	[[nodiscard]] bool QueryDefaultDeviceFormat(DeviceFormat &out)
	{
		BASS_WASAPI_DEVICEINFO info{};

		for (DWORD device = 0; BASS_WASAPI_GetDeviceInfo(device, &info); ++device)
		{
			if ((info.flags & BASS_DEVICE_ENABLED) && !(info.flags & BASS_DEVICE_INPUT) && (info.flags & BASS_DEVICE_DEFAULT))
			{
				out.freq = info.mixfreq;
				out.chans = info.mixchans;
				return out.freq > 0 && out.chans > 0;
			}
		}

		return false;
	}

	// -----------------------------------------------------------------
	// Try to find a supported exclusive format for the device.
	// Prefers the source's native format, falls back to the device's default mix format.
	// -----------------------------------------------------------------

	[[nodiscard]] DeviceFormat NegotiateExclusiveFormat(DWORD sourceFreq, DWORD sourceChans)
	{
		// First, try the source's native sample rate + channels
		if (BASS_WASAPI_CheckFormat(-1, sourceFreq, sourceChans, BASS_WASAPI_EXCLUSIVE) == BASS_OK) [[likely]]
		{
			return {sourceFreq, sourceChans};
		}

		// Fall back to device's native format
		DeviceFormat deviceFmt{};
		if (QueryDefaultDeviceFormat(deviceFmt))
		{
			if (BASS_WASAPI_CheckFormat(-1, deviceFmt.freq, deviceFmt.chans, BASS_WASAPI_EXCLUSIVE) == BASS_OK)
			{
				return deviceFmt;
			}
		}

		// Try common sample rates as last resort
		constexpr DWORD commonRates[] = {192000, 96000, 48000, 44100};
		for (const DWORD rate : commonRates)
		{
			if (BASS_WASAPI_CheckFormat(-1, rate, sourceChans, BASS_WASAPI_EXCLUSIVE) == BASS_OK)
			{
				return {rate, sourceChans};
			}
		}

		// Absolute fallback — return device defaults and let WASAPI_Init report if it truly cannot work
		if (deviceFmt.freq > 0)
		{
			return deviceFmt;
		}

		return {sourceFreq, sourceChans};
	}

	// -----------------------------------------------------------------
	// Resource cleanup
	// -----------------------------------------------------------------

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

		if (g_engine.mixerHandle != 0)
		{
			BASS_StreamFree(g_engine.mixerHandle);
			g_engine.mixerHandle = 0;
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

		g_engine.exclusiveMode = false;
	}

	// -----------------------------------------------------------------
	// Apply volume to the appropriate handle depending on mode
	// -----------------------------------------------------------------

	void ApplyVolumeUnsafe(float volume)
	{
		g_engine.currentVolume = volume;

		if (g_engine.exclusiveMode && g_engine.mixerHandle != 0)
		{
			// In exclusive mode, use BASS_ATTRIB_VOLDSP on the mixer stream.
			// This applies volume as a DSP effect on the PCM data before it
			// reaches WASAPI, so it doesn't touch the system endpoint volume.
			BASS_ChannelSetAttribute(g_engine.mixerHandle, BASS_ATTRIB_VOLDSP, volume);
		}
		else if (g_engine.fxHandle != 0)
		{
			// In shared mode, standard BASS_ATTRIB_VOL works fine
			BASS_ChannelSetAttribute(g_engine.fxHandle, BASS_ATTRIB_VOL, volume);
		}
	}

	// -----------------------------------------------------------------
	// Initialize WASAPI exclusive output with the mixer pipeline
	// -----------------------------------------------------------------

	[[nodiscard]] bool InitWasapiExclusive()
	{
		if (g_engine.fxHandle == 0) [[unlikely]]
		{
			return false;
		}

		// Get source format info from the tempo stream
		BASS_CHANNELINFO channelInfo{};
		if (!BASS_ChannelGetInfo(g_engine.fxHandle, &channelInfo)) [[unlikely]]
		{
			return false;
		}

		// Negotiate a format the device actually supports in exclusive mode
		const DeviceFormat deviceFmt = NegotiateExclusiveFormat(channelInfo.freq, channelInfo.chans);

		// Create a BASSmix mixer stream at the device's native format.
		// The mixer runs in decode mode so WASAPI pulls from it.
		// BASS_MIXER_NONSTOP keeps the mixer running even when the source temporarily has no data (prevents glitches during seeks).
		constexpr DWORD mixerFlags = BASS_STREAM_DECODE | BASS_SAMPLE_FLOAT | BASS_MIXER_NONSTOP;
		g_engine.mixerHandle = BASS_Mixer_StreamCreate(deviceFmt.freq, deviceFmt.chans, mixerFlags);
		if (g_engine.mixerHandle == 0) [[unlikely]]
		{
			return false;
		}

		// Add the tempo stream as a source into the mixer.
		// BASSmix will automatically resample from the source rate to the mixer rate, which eliminates the pitch/speed problems.
		constexpr DWORD addFlags = BASS_MIXER_CHAN_NORAMPIN | BASS_MIXER_CHAN_BUFFER;
		if (!BASS_Mixer_StreamAddChannel(g_engine.mixerHandle, g_engine.fxHandle, addFlags)) [[unlikely]]
		{
			BASS_StreamFree(g_engine.mixerHandle);
			g_engine.mixerHandle = 0;
			return false;
		}

		// Set syncs on the source channel through the mixer
		BASS_Mixer_ChannelSetSync(g_engine.fxHandle, BASS_SYNC_END | BASS_SYNC_MIXTIME, 0, OnPlaybackEndedSync, nullptr);
		BASS_Mixer_ChannelSetSync(g_engine.fxHandle, BASS_SYNC_STALL, 0, OnPlaybackFailedSync, nullptr);

		// Initialize WASAPI in exclusive mode — NO BASS_WASAPI_AUTOFORMAT.
		// We've already negotiated a supported format, and BASSmix handles all the resampling, so WASAPI doesn't need to convert anything.
		constexpr DWORD wasapiFlags = BASS_WASAPI_EXCLUSIVE;
		if (!BASS_WASAPI_Init(-1, deviceFmt.freq, deviceFmt.chans, wasapiFlags, 0.1F, 0, WasapiProc, nullptr)) [[unlikely]]
		{
			BASS_Mixer_ChannelRemove(g_engine.fxHandle);
			BASS_StreamFree(g_engine.mixerHandle);
			g_engine.mixerHandle = 0;
			return false;
		}

		g_engine.wasapiInitialized = true;
		g_engine.exclusiveMode = true;

		// Apply saved volume via DSP volume on the mixer
		ApplyVolumeUnsafe(g_engine.currentVolume);

		return true;
	}
}

// =====================================================================
// Exported API
// =====================================================================

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

	// In exclusive mode, the tempo stream must be in decode mode so it can be fed into the BASSmix mixer.
	// In shared mode, use BASS_FX_FREESOURCE for normal playback.
	const DWORD tempoFlags = isExclusiveMode ? BASS_STREAM_DECODE : BASS_FX_FREESOURCE;
	g_engine.fxHandle = BASS_FX_TempoCreate(g_engine.mainHandle, tempoFlags);
	if (g_engine.fxHandle == 0) [[unlikely]]
	{
		BASS_StreamFree(g_engine.mainHandle);
		g_engine.mainHandle = 0;
		return FALSE;
	}

	// Set tempo (speed change as percentage)
	const float tempoPercent = static_cast<float>((speed - 1.0) * 100.0);
	BASS_ChannelSetAttribute(g_engine.fxHandle, BASS_ATTRIB_TEMPO, tempoPercent);

	// Save the requested volume; it will be applied when the pipeline is fully constructed (in BaePlay for exclusive, or here for shared).
	g_engine.currentVolume = static_cast<float>(volume);

	if (!isExclusiveMode)
	{
		// For shared mode, set up syncs on the fxHandle directly
		BASS_ChannelSetSync(g_engine.fxHandle, BASS_SYNC_END, 0, OnPlaybackEndedSync, nullptr);
		BASS_ChannelSetSync(g_engine.fxHandle, BASS_SYNC_STALL, 0, OnPlaybackFailedSync, nullptr);

		// Apply volume directly for shared mode
		BASS_ChannelSetAttribute(g_engine.fxHandle, BASS_ATTRIB_VOL, g_engine.currentVolume);
	}

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
		// If WASAPI is already running, nothing to do
		if (BASS_WASAPI_IsStarted()) [[unlikely]]
		{
			return TRUE;
		}

		// If WASAPI was initialized (paused state), just restart it
		if (g_engine.wasapiInitialized)
		{
			return BASS_WASAPI_Start() ? TRUE : FALSE;
		}

		// First-time setup: build the full mixer pipeline and init WASAPI
		if (!InitWasapiExclusive()) [[unlikely]]
		{
			return FALSE;
		}

		return BASS_WASAPI_Start() ? TRUE : FALSE;
	}

	// Shared mode playback
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
	ApplyVolumeUnsafe(static_cast<float>(volume));
}

double WINAPI BaeGetPositionSeconds()
{
	const std::lock_guard lock{g_engineMutex};

	if (g_engine.fxHandle == 0) [[unlikely]]
	{
		return -1.0;
	}

	if (g_engine.exclusiveMode && g_engine.mixerHandle != 0)
	{
		// When using a mixer, get the position of the source channel through the mixer for accurate timing
		const QWORD positionBytes = BASS_Mixer_ChannelGetPosition(g_engine.fxHandle, BASS_POS_BYTE);
		return BASS_ChannelBytes2Seconds(g_engine.fxHandle, positionBytes);
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

	BOOL result{};
	if (g_engine.exclusiveMode && g_engine.mixerHandle != 0)
	{
		// Seek through the mixer for proper buffer management
		result = BASS_Mixer_ChannelSetPosition(g_engine.fxHandle, targetBytes, BASS_POS_BYTE | BASS_POS_MIXER_RESET);
	}
	else
	{
		result = BASS_ChannelSetPosition(g_engine.fxHandle, targetBytes, BASS_POS_BYTE);
	}

	if (!result && BASS_ErrorGetCode() == BASS_ERROR_POSITION) [[unlikely]]
	{
		for (int retryCount = 0; retryCount < 5 && !result; ++retryCount)
		{
			std::this_thread::sleep_for(std::chrono::milliseconds{200});

			if (g_engine.exclusiveMode && g_engine.mixerHandle != 0)
			{
				result = BASS_Mixer_ChannelSetPosition(g_engine.fxHandle, targetBytes, BASS_POS_BYTE | BASS_POS_MIXER_RESET);
			}
			else
			{
				result = BASS_ChannelSetPosition(g_engine.fxHandle, targetBytes, BASS_POS_BYTE);
			}
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
