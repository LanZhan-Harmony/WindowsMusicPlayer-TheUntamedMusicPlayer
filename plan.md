# UntamedMusicPlayer 重构执行记录（供后续 AI 接力）

## 目标与验收口径

- 目标：按 `UntamedMusicPlayer_Refactoring_Guide.md` 推进大规模分层重构，逐步拆除 `Data.cs` 的全局耦合。
- 验收：用户已明确取消冒烟测试，当前仅要求“编译成功”。

## 已完成阶段

### 阶段 0：重构规划与拆批

- 完成全仓只读勘探（Data 依赖分布、ViewModel/UI 耦合、Core/OnlineAPI 边界）。
- 形成分阶段路线：先低风险抽象（常量/状态/导航）再逐步参数化导航，最后收敛删除 `Data.cs`。

### 阶段 1：Data 第一刀（常量与应用状态）

- 新增 `UntamedMusicPlayer.Core/Constants/AppConstants.cs`，承接原 `Data` 常量。
- 新增并接入应用状态服务：
  - `UntamedMusicPlayer.Core/Contracts/Services/IAppStateService.cs`
  - `UntamedMusicPlayer.Core/Services/AppStateService.cs`
- `App.xaml.cs` 完成 DI 注册；`Data.cs` 对状态改为兼容转发层。

### 阶段 2：导航访问解耦

- 新增导航抽象：
  - `UntamedMusicPlayer/Contracts/Services/INavigationService.cs`
  - `UntamedMusicPlayer/Services/NavigationService.cs`
- 大批量替换 ViewModel 对 `Data.ShellPage`/`Data.HomePage`/`Data.MainWindow` 的直接导航访问。
- 结果：ViewModel 中对 `Data.ShellPage/HomePage/MainWindow` 的直接引用已清零。

### 阶段 3：参数化导航（进行中）

#### 3.1 歌单链路参数化（已完成）

- 新增参数模型：`PlaylistNavigationArgs`。
- `ShellPage.Navigate` 支持对象参数并透传实体。
- `PlayListsPage` -> `PlayListDetailPage` -> `PlayListDetailViewModel` 改为显式参数初始化。
- 结果：`Data.SelectedPlaylist` 全仓引用清零。

#### 3.2 本地专辑/歌手链路参数化（本轮已完成）

- 新增参数模型（位置 record）：
  - `LocalAlbumNavigationArgs(LocalAlbumInfo Album, string FromPage)`
  - `LocalArtistNavigationArgs(LocalArtistInfo Artist, string FromPage)`
- 详情页改造：
  - `LocalAlbumDetailPage.xaml.cs`：`OnNavigatedTo` 使用 `NavigationEventArgs.Parameter` 初始化。
  - `LocalArtistDetailPage.xaml.cs`：`OnNavigatedTo` 使用 `NavigationEventArgs.Parameter` 初始化。
- 详情 ViewModel 改造：
  - `LocalAlbumDetailViewModel.cs`：新增 `Initialize(LocalAlbumInfo)`，并将“跳歌手”改为参数导航。
  - `LocalArtistDetailViewModel.cs`：新增 `Initialize(LocalArtistInfo)`，并将“跳专辑”改为参数导航。
- 列表页改造：
  - `LocalAlbumsPage.xaml.cs`、`LocalArtistsPage.xaml.cs`：点击进入详情改为参数导航。
  - 返回定位与连贯动画锚点改为页面本地字段，不再依赖 `Data.SelectedLocal*`。
- 五条入口链路全部完成参数化：
  - `RootPlayBarViewModel.cs`
  - `PlayQueueViewModel.cs`
  - `PlayListDetailViewModel.cs`
  - `LyricViewModel.cs`
  - `LocalSongsViewModel.cs`
- `LocalAlbumsViewModel.cs` 的“显示歌手”入口也已改为参数导航。
- 删除 `Data.cs` 中字段：
  - `SelectedLocalAlbum`
  - `SelectedLocalArtist`

### 阶段 4：async void 治理（进行中）

#### 4.1 非事件 async void 第一批（本轮已完成）

- 将一批“初始化/加载/保存”类非事件异步方法改为 `Task`，并在调用方显式使用 `_ = MethodAsync()` 启动，避免隐式 fire-and-forget。
- 已覆盖文件（12 个）：
  - `UntamedMusicPlayer/ViewModels/HomeViewModel.cs`
  - `UntamedMusicPlayer/ViewModels/ShellViewModel.cs`
  - `UntamedMusicPlayer/ViewModels/MusicLibraryViewModel.cs`
  - `UntamedMusicPlayer/Views/MusicLibraryPage.xaml.cs`
  - `UntamedMusicPlayer/ViewModels/LocalAlbumsViewModel.cs`
  - `UntamedMusicPlayer/ViewModels/LocalSongsViewModel.cs`
  - `UntamedMusicPlayer/ViewModels/LocalArtistsViewModel.cs`
  - `UntamedMusicPlayer/ViewModels/PlayListsViewModel.cs`
  - `UntamedMusicPlayer/ViewModels/OnlineAlbumDetailViewModel.cs`
  - `UntamedMusicPlayer/ViewModels/OnlineArtistDetailViewModel.cs`
  - `UntamedMusicPlayer/Views/OnlineAlbumDetailPage.xaml.cs`
  - `UntamedMusicPlayer/Views/OnlineArtistDetailPage.xaml.cs`
- 结果：本批清理非事件 `async void` 共 13 处（改为 `Task`）。

#### 4.2 非事件 async void 第二批（本轮已完成）

- 继续将在线链路中“可等待业务方法”从 `async void` 改为 `Task`，并将页面调用点统一改为 `_ = ...` fire-and-forget 调用。
- 已覆盖文件（10 个）：
  - `UntamedMusicPlayer/ViewModels/OnlineAlbumDetailViewModel.cs`
  - `UntamedMusicPlayer/Views/OnlineAlbumDetailPage.xaml.cs`
  - `UntamedMusicPlayer/ViewModels/OnlineArtistDetailViewModel.cs`
  - `UntamedMusicPlayer/Views/OnlineArtistDetailPage.xaml.cs`
  - `UntamedMusicPlayer/ViewModels/OnlineSongsViewModel.cs`
  - `UntamedMusicPlayer/Views/OnlineSongsPage.xaml.cs`
  - `UntamedMusicPlayer/ViewModels/OnlineAlbumsViewModel.cs`
  - `UntamedMusicPlayer/Views/OnlineAlbumsPage.xaml.cs`
  - `UntamedMusicPlayer/ViewModels/OnlineArtistsViewModel.cs`
  - `UntamedMusicPlayer/Views/OnlineArtistsPage.xaml.cs`
- 低风险补充：
  - `UntamedMusicPlayer/ViewModels/SettingsViewModel.cs` 中两个私有非事件方法 `LoadSongDownloadLocationAsync` / `SaveSongDownloadLocationAsync` 改为 `Task`，并修正触发点为 `_ = ...`。
- 结果：本批新增清理非事件 `async void` 共 21 处（改为 `Task`）。

#### 4.3 未等待异步调用（CS4014）清理（本轮已完成）

- 目标：将编译告警中的未等待异步调用补齐，保证异步语义明确，避免隐式并发。
- 清理策略：
  - 对事件处理器中的 `ExecuteAsync(...)` 调用统一改为 `await`。
  - 对属性 `set` 中无法直接 `await` 的持久化调用，改为显式 `_ = ...` fire-and-forget，避免编译器 CS4014。
  - 对 `OnNavigatedTo` 中已改为 `Task` 的初始化方法调用，改为 `async void` override + `await`。
- 已修复文件（7 个）：
  - `UntamedMusicPlayer/Views/MusicLibraryPage.xaml.cs`
  - `UntamedMusicPlayer/Views/OnlineAlbumDetailPage.xaml.cs`
  - `UntamedMusicPlayer/Views/OnlineArtistDetailPage.xaml.cs`
  - `UntamedMusicPlayer/Views/OnlinePlayListDetailPage.xaml.cs`
  - `UntamedMusicPlayer/Views/OnlinePlayListsPage.xaml.cs`
  - `UntamedMusicPlayer/Views/PlayQueuePage.xaml.cs`
  - `UntamedMusicPlayer/Views/PlayListDetailPage.xaml.cs`
- 结果：`CS4014` 从 10 条降为 0 条。

## 当前验证结果

- 关键引用统计：`SelectedLocalAlbum|SelectedLocalArtist` 全仓检索为 0（仅项目有效路径范围）。
- 构建命令：`dotnet build UntamedMusicPlayer/UntamedMusicPlayer.csproj -v minimal`
- 构建结果：成功（当前仅保留 4 条 `Icon` 过时警告 `CS0612`，非本次重构引入）。
- 现状指标：
  - `async void`（C#）总量：172
  - `Data.`（C#）总量：906

## 本轮涉及文件（增量）

- `UntamedMusicPlayer/ViewModels/RootPlayBarViewModel.cs`
- `UntamedMusicPlayer/ViewModels/PlayQueueViewModel.cs`
- `UntamedMusicPlayer/ViewModels/PlayListDetailViewModel.cs`
- `UntamedMusicPlayer/ViewModels/LyricViewModel.cs`
- `UntamedMusicPlayer/ViewModels/LocalSongsViewModel.cs`
- `UntamedMusicPlayer/Views/LocalAlbumDetailPage.xaml.cs`
- `UntamedMusicPlayer/Views/LocalArtistDetailPage.xaml.cs`
- `UntamedMusicPlayer/ViewModels/LocalAlbumsViewModel.cs`
- `UntamedMusicPlayer/ViewModels/LocalAlbumDetailViewModel.cs`
- `UntamedMusicPlayer/ViewModels/LocalArtistDetailViewModel.cs`
- `UntamedMusicPlayer/Views/LocalAlbumsPage.xaml.cs`
- `UntamedMusicPlayer/Views/LocalArtistsPage.xaml.cs`
- `UntamedMusicPlayer/Models/NavigationParameters.cs`
- `UntamedMusicPlayer/Models/Data.cs`

## 尚未完成（接续建议）

1. async void 与可测试性治理

- 对高风险 `async void`（非事件处理）改为 `Task`，补充异常路径与调用方等待策略。

2. ViewModel 去 UI 类型依赖

- 继续减少 `ViewModel` 中对 `FrameworkElement/Page/Window` 的直接接触，收敛到服务接口。

3. Data.cs 最终收敛

- 在在线链路与残余入口完成参数化后，继续删除 `Data.cs` 中导航临时字段与视图实例暴露。

## 实施约束（继续执行时请保持）

- 不回滚用户已有改动；仅处理当前任务相关文件。
- 验收口径仍为“编译成功”。
