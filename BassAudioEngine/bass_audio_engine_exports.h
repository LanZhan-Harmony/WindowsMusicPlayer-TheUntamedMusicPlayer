#pragma once

#include <windows.h>

#ifdef BASSAUDIOENGINE_EXPORTS
#define BAE_API extern "C" __declspec(dllexport)
#else
#define BAE_API extern "C" __declspec(dllimport)
#endif

using BassAudioEngineCallback = void(CALLBACK*)();

BAE_API void WINAPI BaeSetCallbacks(BassAudioEngineCallback playbackEndedCallback, BassAudioEngineCallback playbackFailedCallback);
BAE_API BOOL WINAPI BaeInitialize();
BAE_API void WINAPI BaeShutdown();
BAE_API BOOL WINAPI BaeLoadSong(const wchar_t* path, BOOL isOnline, BOOL isExclusiveMode, double volume, double speed, double* totalSeconds);
BAE_API void WINAPI BaeStop();
BAE_API BOOL WINAPI BaePlay(BOOL isExclusiveMode);
BAE_API void WINAPI BaePause(BOOL isExclusiveMode);
BAE_API void WINAPI BaeSetSpeed(double speed);
BAE_API void WINAPI BaeSetVolume(double volume);
BAE_API double WINAPI BaeGetPositionSeconds();
BAE_API BOOL WINAPI BaeSetPositionSeconds(double targetSeconds);
BAE_API int WINAPI BaeGetLastError();
BAE_API BOOL WINAPI BaeIsLastErrorBusy();
