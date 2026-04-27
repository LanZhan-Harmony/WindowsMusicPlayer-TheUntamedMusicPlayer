<div align="center">

[English](Docs/CONTRIBUTING-en.md) | [中文](CONTRIBUTING.md)

</div>

# 开发环境配置指南

## 系统要求

- **操作系统**：Windows 10 22H2（内部版本 19041）或更高版本
- **开发工具**：Visual Studio 2026（社区版即可）

## 开发环境设置

### 1. 安装 Visual Studio 组件

安装 Visual Studio 2026 时，请勾选以下工作负载：

- **使用 C++ 的桌面开发**
- **WinUI 应用程序开发**

![Visual Studio 组件安装图](/Docs/Images/VisualStudioComponent.png)

### 2. 克隆项目源码

使用 Git 克隆本项目到本地：

```bash
git clone https://github.com/LanZhan-Harmony/WindowsMusicPlayer-TheUntamedMusicPlayer.git
```

### 3. 在 Visual Studio 中打开项目

在 Visual Studio 中打开 `UntamedMusicPlayer.slnx` 文件。

### 4. 还原 NuGet 包

等待 Visual Studio 自动还原 NuGet 依赖包。

### 5. 编译 BassAudioEngine

右击 `BassAudioEngine` 项目，选择“生成”以编译音频引擎。
![编译 BassAudioEngine 图](/Docs/Images/BuildBassAudioEngine.png)

### 6. 编译运行 Untamed Music Player

右击 `Untamed Music Player` 项目，选择“设为启动项目”。
![设为启动项目图](/Docs/Images/SetStartupProject.png)

点击工具栏中的"**▶ Untamed Music Player (Package)**"按钮开始调试和运行应用程序。
