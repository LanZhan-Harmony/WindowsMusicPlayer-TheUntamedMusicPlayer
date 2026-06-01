# Untamed Music Player 重构执行指南（基于当前 C# 代码实扫）

本文件不是“概念建议”，而是给后续 AI 重构直接执行的任务清单。

扫描范围：

- 已扫描 `UntamedMusicPlayer`、`UntamedMusicPlayer.Core`、`UntamedMusicPlayer.OnlineAPI` 的 C# 与 XAML 绑定关系。
- 已明确忽略 C++ 项目 `BassAudioEngine`。

---

## 1. 代码库现状诊断（实锤问题）

### 1.1 `Data.cs` 成为全局单例总线（高风险）

问题：`Data` 同时持有“状态 + 服务 + View + ViewModel + 导航参数 + 常量”，已形成全局隐式依赖。

证据：

- `UntamedMusicPlayer/Models/Data.cs` 持有 `MainWindow`、`ShellPage`、`HomePage`、`LyricPage`、`RootPlayBarView`、`DesktopLyricWindow`。
- 同文件持有 `MusicLibrary`、`OnlineMusicLibrary`、`PlaylistLibrary`、`MusicPlayer`、`PlayState`。
- 大量页面与 ViewModel 直接访问 `Data.*`，并在 XAML 中直接绑定 `model:Data.*`。

影响：

- 依赖不可见，难测、难替换、难并行开发。
- 生命周期混乱，窗口对象全局挂载导致内存泄漏和悬挂事件风险。
- 重构阻力指数级上升。

### 1.2 ViewModel 直接依赖 UI 类型（高风险）

问题：大量 ViewModel 引用了 `Microsoft.UI.Xaml.*`，并接收 `RoutedEventArgs`、`SelectionChangedEventArgs`、`ItemClickEventArgs`、`DragEventArgs` 等纯 UI 参数。

证据（节选）：

- `UntamedMusicPlayer/ViewModels/HomeViewModel.cs`
- `UntamedMusicPlayer/ViewModels/LocalSongsViewModel.cs`
- `UntamedMusicPlayer/ViewModels/ShellViewModel.cs`
- `UntamedMusicPlayer/ViewModels/SettingsViewModel.cs`
- `UntamedMusicPlayer/ViewModels/LyricViewModel.cs`

影响：

- ViewModel 不可复用、不可单测。
- 事件处理混入业务，UI 改动会破坏业务逻辑。

### 1.3 `async void` 滥用（高风险）

问题：除事件处理器外，存在大量 `async void` 业务方法。

证据（节选）：

- `UntamedMusicPlayer/Playback/MusicPlayer.cs`
- `UntamedMusicPlayer/Models/MusicLibrary.cs`
- `UntamedMusicPlayer/ViewModels/*`（多处）

影响：

- 异常无法被调用方捕获。
- 并发流程不可等待，导致状态错乱。

### 1.4 Model 与 Service 职责错位（高风险）

问题：`Models` 目录中包含大量 I/O、网络、调度、副作用逻辑。

证据（节选）：

- `UntamedMusicPlayer/Models/MusicLibrary.cs`：文件系统扫描、Watcher、Dispatcher、消息发送。
- `UntamedMusicPlayer/Models/OnlineMusicLibrary.cs`：网络搜索、UI `Visibility`、`AutoSuggestBox` Loaded 处理。
- `UntamedMusicPlayer/Models/FileManager.cs`：持久化读写。

影响：

- 领域模型被污染，边界模糊。
- 后续拆分 `Core` 项目困难。

### 1.5 导航和页面控制耦合在 ViewModel（高风险）

问题：ViewModel 通过 `Data.ShellPage`、`Data.HomePage` 直接导航或获取 `Frame`。

证据：

- `UntamedMusicPlayer/ViewModels/HomeViewModel.cs`
- `UntamedMusicPlayer/ViewModels/LocalSongsViewModel.cs`
- `UntamedMusicPlayer/ViewModels/ShellViewModel.cs`

影响：

- UI 容器一旦变化，业务层大面积改动。

### 1.6 在线 API 项目重复实现且未真正接入（中高风险）

问题：`UntamedMusicPlayer` 项目中已有 `OnlineAPIs/CloudMusicAPI`，同时 `UntamedMusicPlayer.OnlineAPI` 也有同类实现，但命名空间不同且 UI 工程未引用 `UntamedMusicPlayer.OnlineAPI.*`。

证据：

- UI 工程使用 `UntamedMusicPlayer.OnlineAPIs.CloudMusicAPI`。
- 搜索结果显示 UI 工程没有 `UntamedMusicPlayer.OnlineAPI.*` 的引用。

影响：

- 代码重复、维护分叉、行为不一致。

### 1.7 `UntamedMusicPlayer.Core` 基本为空壳（中风险）

问题：`UntamedMusicPlayer.Core/Class1.cs` 为空，尚未承载 UI 无关逻辑。

影响：

- 分层目标存在但未落地。

### 1.8 DI 使用了容器，但仍有 Service Locator（中风险）

问题：`App.xaml.cs` 使用 `Host.CreateDefaultBuilder` 注册服务，但大量类仍通过 `App.GetService<T>()` 主动取依赖。

影响：

- 依赖方向仍不清晰，构造函数注入优势未发挥。

---

## 2. 目标架构（重构完成态）

### 2.1 分层与项目职责

- `UntamedMusicPlayer`：仅保留 UI 层（Views、Converters、UI 资源、窗口相关服务实现）。
- `UntamedMusicPlayer.Core`：业务服务、领域模型、接口、应用用例（不引用 `Microsoft.UI.*`）。
- `UntamedMusicPlayer.OnlineAPI`：各平台 API Provider 与 DTO/映射。

### 2.2 依赖方向

- UI -> Core（接口）
- UI -> OnlineAPI（通过 Core 抽象间接调用，避免 UI 直接依赖具体 Provider）
- Core 不得依赖 UI

### 2.3 关键原则

- 所有业务入口可 `await`，避免 `async void`。
- ViewModel 仅处理状态与命令，不接收 UI 控件类型。
- 页面导航、对话框、文件选择、拖拽事件均通过接口/适配层隔离。

---

## 3. 分阶段重构计划（AI 可按批次执行）

总策略：渐进式绞杀（Strangler Pattern），每阶段必须“可编译、可运行、可回滚”。

### 阶段 0：建立基线（必须先做）

目标：防止重构过程中回归无感知。

任务：

1. 建立架构约束文档（本文件即基础）。
2. 建立最小冒烟检查清单：启动、扫描本地库、播放、切歌、搜索在线、导入导出歌单。
3. 增加“禁止新增 `Data.*` 调用”的代码审查规则。

验收：

- 冒烟流程可跑通。
- 后续 PR 不允许新增 `Data.*`。

### 阶段 1：先拆 `Data` 的“常量与状态”

目标：低风险切第一刀。

任务：

1. 新建 `Core/Constants/AppConstants`，迁移音频/封面扩展名。
2. 新建 `Core/Contracts/Services/IAppStateService` 与实现，迁移 `IsMusicProcessing`、`IsFileActivationLaunch`。
3. 保留 `Data` 兼容层（仅转发），开始替换调用点到 DI 服务。

验收：

- `Data.cs` 中不再定义常量和可变全局标志位（仅临时转发可接受）。

### 阶段 2：抽离导航与窗口访问

目标：切断 ViewModel 对 Page/Window 的直接引用。

任务：

1. 定义 `INavigationService`、`IWindowService`、`IDialogService`。
2. 替换 `Data.ShellPage`、`Data.HomePage`、`Data.MainWindow` 的使用。
3. 统一导航参数，不再使用 `Data.SelectedLocalAlbum` 等“全局临时变量”。

验收：

- ViewModel 中不出现 `Data.ShellPage`/`Data.HomePage`/`Data.MainWindow`。

### 阶段 3：ViewModel 去 UI 类型化

目标：把 ViewModel 从“事件处理器集合”改为“命令 + 状态”。

任务：

1. 将 `*_Click` 迁移为 `[RelayCommand]` 与业务命名（如 `PlaySongAsync`）。
2. 对 `SelectionChanged`、`DragOver` 等必须保留 UI 事件的场景，在 XAML.cs 中做参数提取，再调用 ViewModel 命令。
3. `Visibility`、`Thickness`、`Brush` 等 UI 类型改为纯值（bool/int/enum），在 XAML 用 Converter 或样式完成转换。

验收：

- ViewModel 不再引用 `Microsoft.UI.Xaml.*`（允许极少数过渡文件，需登记）。

### 阶段 4：`async void` 清理

目标：让异步链可追踪、可捕获、可取消。

任务：

1. 业务方法一律改 `Task`/`Task<T>`。
2. 事件处理器仅保留 UI 必需的 `async void`，内部调用 `await SomeCommandAsync()`。
3. 为长任务引入 `CancellationToken`（搜索、扫描、加载更多）。
4. 耗时同步操作统一封装在 `*Async` 内部并 `await Task.Run(...)`，调用方不直接写 `Task.Run(...)`。
5. 非 UI 线程更新 UI 时，统一通过 `DispatcherQueue.TryEnqueue(...)` 回到 UI 线程。

验收：

- 除事件签名外，无新增 `async void`。
- 业务调用点不出现“到处 `Task.Run`”的分散写法。
- UI 更新线程切换路径清晰且可审计。

### 阶段 5：服务边界重命名与迁移

目标：让目录与职责一致。

任务：

1. `FileManager` -> `IFileService/FileService`。
2. `MusicLibrary` -> `IMusicLibraryService/MusicLibraryService`。
3. `OnlineMusicLibrary` 拆成：
   - `IOnlineSearchService`（搜索逻辑）
   - `HomeSearchStateViewModel`（纯 UI 状态）
4. `PlaylistLibrary`、`CoverManager` 统一迁入服务层。

验收：

- `Models` 目录主要保留 DTO/实体，不再存放重 I/O 逻辑。

### 阶段 6：在线 API 统一来源

目标：消除双份 API 代码。

任务：

1. 选定唯一实现来源：推荐以 `UntamedMusicPlayer.OnlineAPI` 为唯一 Provider 项目。
2. 将 `UntamedMusicPlayer/OnlineAPIs/*` 渐进迁移或删除重复部分。
3. 在 Core 定义 `IOnlineMusicProvider` 抽象，UI 不直接依赖具体平台 API。

验收：

- UI 项目不再包含重复 API 实现目录。
- 同一功能仅一处代码源。

### 阶段 7：推进 Core 真正承载业务

目标：落地三项目分离。

任务：

1. 将播放队列规则、搜索聚合、歌单导入导出等 UI 无关逻辑迁入 `Core`。
2. `Core` 禁止引用 `Microsoft.UI.*`。
3. UI 工程只保留页面行为编排与绑定。

验收：

- `UntamedMusicPlayer.Core` 不再是空壳。

### 阶段 8：删除 `Data.cs`

目标：去除最终架构债务入口。

任务：

1. 所有调用点完成替换。
2. 删除 `Data.cs` 和残留 `model:Data.*` 绑定。

验收：

- 全仓 C#、XAML 中 `Data.` 为 0（注释除外）。

---

## 4. 重点改造清单（建议优先级）

优先级 P0（先做）：

1. `Models/Data.cs`
2. `ViewModels/HomeViewModel.cs`
3. `ViewModels/LocalSongsViewModel.cs`
4. `ViewModels/ShellViewModel.cs`
5. `Models/MusicLibrary.cs`
6. `Models/OnlineMusicLibrary.cs`

优先级 P1（随后）：

1. 其余依赖 `Data.*` 的 ViewModel。
2. 所有 `x:Bind model:Data.*` 的页面。
3. `Playback` 下 `async void` 流程。

优先级 P2（收尾）：

1. `OnlineAPIs` 重复目录清理。
2. `Core` 迁移与接口完善。

---

## 5. 规范补充（保留并强化）

### 5.1 ObservableProperty AOT 写法

统一使用：

```csharp
[ObservableProperty]
public partial string Name { get; set; }
```

### 5.2 Command 命名与边界

- 不使用 `Button_Click`、`SelectionChanged` 作为业务方法名。
- 使用 `PlaySongAsync`、`AddToPlaylistAsync` 等业务语义命名。
- ViewModel 不接受 `sender/e`。

### 5.3 Messenger 生命周期

- 统一 `StrongReferenceMessenger`。
- 注册后必须配套注销（`IDisposable` 或页面 `Loaded/Unloaded`）。
- ViewModel 推荐 `ObservableRecipient + IRecipient<T>`。

### 5.4 Converter 代替 ViewModel UI 类型

- `Visibility` 用 `bool + Converter`。
- `Thickness` 尽量改为布局状态枚举 + Converter。

### 5.5 异步代码粒度与线程模型

原则：

1. 调用方只关心“这是异步能力”，不关心内部是否用了 `Task.Run`。
2. 若存在耗时同步逻辑（CPU 密集、阻塞 I/O API、第三方同步 SDK），必须在 `*Async` 内部 `await Task.Run(...)`，避免阻塞 UI 线程。
3. UI 相关对象只能在 UI 线程访问；后台线程需要通过 `DispatcherQueue.TryEnqueue(...)` 切回 UI。

推荐写法（B，封装细节）：

```csharp
public async Task RefreshLibraryAsync(CancellationToken ct)
{
   // 将同步耗时逻辑封装在服务内部，调用者只 await 本方法
   var result = await Task.Run(() => _scanner.Scan(ct), ct);

   // 回到 UI 线程更新绑定状态
   _dispatcherQueue.TryEnqueue(() =>
   {
      Songs = result.Songs;
      IsLoading = false;
   });
}
```

不推荐写法（A，泄漏实现细节）：

```csharp
await Task.Run(() => service.Function());
```

补充约束：

- 不要把纯异步 I/O（本身已有 `await`）再包一层 `Task.Run`。
- 不要在 View/XAML.cs 中直接写复杂 `Task.Run` 业务；统一下沉到 Service 或 UseCase。
- `TryEnqueue` 内只放 UI 赋值和轻量操作，重活仍在后台线程做。

### 5.6 异常处理粒度与日志边界

核心规则：

1. 内部方法只捕获“能真正处理”的异常（重试、降级、兜底、转换）。
2. 无法处理的异常直接抛出，让上层决定展示与记录。
3. 禁止“内部吞异常 + 外部无感知”。
4. 禁止同一异常在多层重复记录并重复弹窗。

`async void`（事件入口）规则：

- 第一行进入 `try`，末尾 `catch (Exception ex)` 兜底，确保不会把异常抛到同步上下文导致进程不稳定。

示例：

```csharp
private async void RefreshButton_Click(object sender, RoutedEventArgs e)
{
   try
   {
      await ViewModel.RefreshLibraryAsync();
   }
   catch (Exception ex)
   {
      _logger.LogError(ex, "刷新失败");
      await _dialogService.ShowErrorAsync("刷新失败，请稍后重试。");
   }
}
```

`async Task`（业务层）规则：

- 只捕获可恢复异常，其余继续抛出。

```csharp
public async Task<IReadOnlyList<Song>> LoadSongsAsync(CancellationToken ct)
{
   try
   {
      return await _repository.LoadAsync(ct);
   }
   catch (TimeoutException ex)
   {
      _logger.LogWarning(ex, "读取超时，执行降级策略");
      return [];
   }
   // 其他异常不捕获，交给上层统一处理
}
```

日志落点建议：

- 业务层记录“诊断日志”（Warning/Error）。
- 表示层（页面或顶层协调器）负责“用户可见提示”。
- 同一异常链只定义一个“最终用户提示出口”。

### 5.7 必须遵守的优秀设计模式（本项目适用）

1. 分层架构（Layered Architecture）
   - UI 层不包含核心业务规则。
   - Core 层不依赖 UI 框架。
2. MVVM + Command Pattern
   - 业务动作以 Command 暴露，不让 ViewModel 持有 UI 控件。
3. Dependency Injection + Constructor Injection
   - 禁止新增 Service Locator 风格依赖获取。
4. Strategy Pattern（多源在线音乐）
   - 不同平台 API（云音乐/QQ/酷我等）通过统一接口切换。
5. Adapter Pattern（UI 事件适配）
   - 在 XAML.cs 把 `DragEventArgs`、`SelectionChangedEventArgs` 转成纯数据后再调用 ViewModel。
6. Facade/Application Service Pattern
   - 将扫描、搜索、播放队列编排聚合为用例服务，避免页面直接串联多个底层对象。
7. Repository Pattern（可选，针对持久化）
   - 文件/缓存读写通过仓储接口隔离，便于测试与替换实现。
8. Observer/Messenger Pattern（已在用）
   - 严格遵守注册/注销生命周期，避免悬挂订阅。

反模式黑名单：

- God Object（`Data.cs` 这类全局总线）。
- Anemic Boundary（ViewModel 直接操纵页面对象）。
- Exception Swallowing（`catch {}` 静默吞异常）。
- Shotgun `Task.Run`（调用点到处包线程池）。

---

## 6. AI 执行策略（你后续可直接下发）

每次只做一个小批次，推荐粒度：

1. 一个领域（例如“本地歌曲页”）
2. 最多 8~12 个文件
3. 必须包含：代码修改 + 编译通过 + 冒烟记录 + 待办清单更新

推荐提交模板：

1. `refactor(core): introduce IAppStateService and AppConstants`
2. `refactor(vm): remove Data usage from HomeViewModel`
3. `refactor(nav): replace ShellPage direct navigation with INavigationService`

每批次完成后输出：

1. 已替换的 `Data.*` 调用数
2. 剩余 `Data.*` 调用数
3. 新增/清理的 `async void` 数
4. 冒烟结果

---

## 7. 风险与回滚

高风险点：

1. 导航参数改造（最易出现页面状态丢失）
2. 播放队列与播放状态同步
3. 文件监控与重扫并发

回滚策略：

1. 每阶段独立分支与独立提交。
2. 保留兼容适配层，不做“一步到位删除”。
3. 任一阶段出现核心功能回归，先回滚该阶段，不连带后续阶段。

---

## 8. 完成定义（Definition of Done）

同时满足以下条件才算重构完成：

1. 无 `Data.cs`。
2. ViewModel 中无 `Microsoft.UI.Xaml.*` 依赖（过渡白名单除外）。
3. 业务层无 `async void`。
4. `UntamedMusicPlayer.Core` 承担实际业务逻辑并可独立测试。
5. 在线 API 只有一套实现来源。
6. 冒烟流程全绿：启动、扫描、播放、切歌、歌单、在线搜索、导入导出。
