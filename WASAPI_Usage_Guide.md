# WASAPI独占模式使用指南

## 功能概述

MusicPlayer类现已支持WASAPI独占模式播放，提供更低延迟和更高质量的音频输出。

## 新增功能

### 1. 独占模式属性
- `IsExclusiveMode`: bool类型属性，控制是否启用WASAPI独占模式

### 2. 模式切换方法
- `SwitchExclusiveMode()`: 在播放过程中动态切换独占模式和共享模式

## 使用方法

### 启用独占模式
```csharp
// 设置独占模式
musicPlayer.IsExclusiveMode = true;

// 或者动态切换
musicPlayer.SwitchExclusiveMode();
```

### 检查当前模式
```csharp
if (musicPlayer.IsExclusiveMode)
{
    // 当前处于独占模式
    Console.WriteLine("正在使用WASAPI独占模式");
}
else
{
    // 当前处于共享模式
    Console.WriteLine("正在使用标准共享模式");
}
```

## 技术细节

### 独占模式特点
1. **更低延迟**: 绕过Windows音频混合器，直接与音频驱动通信
2. **更高音质**: 避免系统重采样和音频处理
3. **独占访问**: 应用程序独占音频设备，其他应用无法同时播放音频

### 实现原理
1. 在独占模式下，使用WASAPI接口直接控制音频设备
2. 音频数据流通过WASAPI回调函数`WasapiProc`传输
3. 音量控制使用`BassWasapi.SetVolume`而非Bass的ChannelSetAttribute
4. 支持播放中动态切换模式，保持播放位置连续性

### 兼容性处理
- 如果独占模式初始化失败，自动回退到共享模式
- 所有现有功能（播放、暂停、音量调节、变速等）在两种模式下都完全兼容
- 无缝支持在线音频流和本地文件

## 注意事项

1. **独占访问**: 启用独占模式时，其他应用程序将无法播放音频
2. **设备兼容性**: 某些音频设备可能不支持独占模式
3. **驱动要求**: 需要支持WASAPI的音频驱动程序
4. **自动回退**: 如果独占模式不可用，系统会自动回退到共享模式

## 错误处理

系统会自动处理以下情况：
- WASAPI设备不可用时自动回退到Bass默认播放
- 独占模式初始化失败时的错误恢复
- 模式切换时的状态保持和位置恢复

## 示例代码

```csharp
// 创建播放器实例
var player = new MusicPlayer();

// 设置独占模式
player.IsExclusiveMode = true;

// 播放音乐（将自动使用独占模式）
player.PlaySongByInfo(songInfo);

// 运行时切换模式
player.SwitchExclusiveMode(); // 切换到共享模式
player.SwitchExclusiveMode(); // 切换回独占模式
```

## 日志监控

系统会记录以下关键事件：
- WASAPI初始化成功/失败
- 独占模式启动成功/失败
- 模式切换操作
- 设备信息和错误状态

查看日志以了解WASAPI运行状态和故障排除信息。