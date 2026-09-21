# SIBYL 耳机 Windows 10/11 电脑版状态调整中心 (SIBYL Earbuds Manager)

基于 **.NET 8 (C#) / WinUI 3 (Windows App SDK)** 构建的原生 Windows 10/11 桌面耳机控制应用。通过对 Android 客户端（`SIBYL MUSIC_1.23.apk`）的逆向反编译与协议分析，完整还原了 SIBYL 系列及主流双模蓝牙耳机（杰理/瑞昱/炬力方案）的核心控制协议，并采用微软 WinUI-Gallery 设计规范落地现代化图形界面。

> 本项目在 2026 年已从 WPF 全量迁移至 **WinUI 3 / Windows App SDK（1.7）**：`Microsoft.UI.Xaml` 命名空间、`x:Bind` 编译绑定、`ThemeResource` 主题资源、`Frame` 双页导航、自定义标题栏 + Mica 材质，仪表盘式的双栏控制台布局。

---

## 🛡️ 核心保障：为什么指令发送“绝对不会和音频播放打架”？

1. **物理与协议栈完全隔离**：
   * **音频传输**：完全托管给 Windows 10/11 系统原生音频服务（Windows Audio Engine），走经典蓝牙 **A2DP** 高清立体声链路。本 App **不触碰任何音频流，绝不申请虚拟音频端点**。
   * **指令控制**：仅使用 Windows 原生 **WinRT BLE（低功耗蓝牙）GATT 特征通道** 发送轻量状态控制包。在 Windows 底层，BLE 与 A2DP 属完全独立的子系统，时隙调度互不干扰。
2. **零串口依赖（严禁 SPP/RFCOMM）**：绝不创建任何虚拟 COM 串口（SPP），绝不触发通话协议（HFP/HSP），杜绝音质降级为窄带单声道。
3. **80ms 智能防抖与射频保护（Anti-Conflict Command Dispatcher）**：连续拖动 EQ 滑块或灯效亮度时，防抖队列自动合并同类指令，单次发包限制在 10~20 字节（单 MTU 范围），发完即释放射频，确保 A2DP 音频缓冲始终饱满。
4. **Write-Without-Response（免等待写入优先）**：高频控制指令走无阻塞异步写入，不占用 UI 线程与 GATT 同步等待。

---

## ✨ WinUI 3 (Windows App SDK) 现代交互工作流

1. **WinUI-Gallery 控件规范**：`InfoBar` 扫描状态条、`ListView` 虚拟化设备列表、`RadioButtons` 分组选择、`ToggleSwitch` 功能开关、`Slider` 连续调节、`ColorPicker` 灯效取色、`ThemeResource` 语义化主题刷子（自动适配浅色/深色/高对比度），统一走默认 Fluent 控件模板，不做手搓 ControlTemplate。
2. **两页式沉浸导航（Frame）**：
   * **页面 1【周边设备发现与连接页】**：App 启动即自动开启低功耗蓝牙持续扫描。采用从官方 Android 端 `BLEScanManager` 逆向的**厂商广播精确过滤**（CompanyID `0xC912` + 广播 payload ≥ 12 字节 + VendorId 命中官方产品清单），**设备列表只会出现官方 SIBYL 耳机**，无关蓝牙设备一概不显示。
   * **页面 2【耳机深度状态调整详情页】**：连接成功平滑进入控制中心；左上角返回键随时回设备列表切换设备。
3. **两级降噪调节**：舒适降噪（50%）/ 深度降噪（100%）。
4. **智能记忆与自动回连**：首次连接成功自动记住设备，下次启动在线即静默重连直达控制台。
5. **机型自动适配（无手动覆盖）**：连接成功后依据广播中的 VendorId **自动选择对应机型适配方案**（S1/S7/S10/B1/Y1），并按其能力矩阵「按需折叠界面功能」。
6. **隐蔽式高级抽屉**：右上角齿轮唤出，收纳 BLE 驱动内核切换（WinRT BLE 真实 / 虚拟模拟器）、固件重置与实时通信帧日志。
7. **自包含绿色发布**：WinUI 3 + `SelfContained` + `WindowsAppSDKSelfContained`，目标机器**无需安装 Windows App Runtime 与 .NET 运行时**，解压即用。

---

## 🎧 核心功能清单

* **🔋 电池电量状态监控**：左耳 (L)、右耳 (R)、充电仓独立实时电量；充电状态动态提示（⚡）。
* **🛡️ 降噪控制中心 (ANC)**：降噪（深度 100%/舒适 50%）、普通关闭、通透模式；仅在有对应机型能力时展示。
* **🎛️ 10 段专业音乐均衡器 (EQ)**：`31Hz~16kHz`、`-8 ~ +8 dB`、`SnapsTo=StepValues` 步进、80ms 防抖限流；内置 7 组预设。
* **🎮 游戏低延迟模式**：38ms 超低延迟电竞模式一键开关（ToggleSwitch）。
* **🖐️ 按键与触控自定义**：左右耳 单击/双击/三击/长按 手势动作映射，显式「保存」后下发协议。
* **💡 RGB 炫彩灯效控制**：常亮/呼吸/关闭、呼吸速度 (0~7)、亮度 (0~100%)、ColorPicker 自由取色。
* **⏱️ 实用辅助工具**：触控锁定、寻找耳机、定时自动关机（15/30/60 分钟）、恢复默认/恢复出厂。
* **🔊 Hi-Res / LDAC 高清解码**：按机型能力展示（如 S11 / WF200 / WS200）。
* **🌐 双模式自由切换**：真实 WinRT BLE 模式 / 虚拟佩戴模拟器（无需硬件即可演示完整状态回路）。

---

## 🎵 内置 3 组音效处理与疗愈白噪音

* **官方 3 大经典 DSP 硬件调音预设**（写入耳机硬件寄存器）：
  * 经典原声 (Classic)：`[0,0,0,0,0,0,0,0,0,0]`
  * 澎湃摇滚 (Rock)：`[-6,+5,-3,-2,+5,+4,-4,-3,+6,+4]`
  * 温润抒情 (Lyric)：`[+6,+5,+6,+1,0,0,+1,+3,+4,0]`
* **3 大疗愈白噪音**（客户端 `Windows.Media.Playback` 即时播放、循环、可调音量）：
  * 🌸 粉红噪音 `mp3_fenhong.mp3` — 1/f 功率谱舒缓耳鸣与耳压。
  * 🐦 森林鸟鸣 `mp3_niaojiao.mp3` — 配合通透模式释放佩戴疲劳。
  * 🌧️ 淅沥细雨 `mp3_xiaoyu.mp3` — 配合 ANC 深度降噪打造极致静谧空间。

---

## 📱 各型号设备功能支持映射矩阵 (Device Capability Matrix)

| 型号系列 | 主控芯片方案 | ANC 降噪 | 10段EQ | 游戏模式 | RGB炫彩灯效 | LDAC高清 | 体感控制 | 触控防误触 | 定时关机 |
| :--- | :--- | :---: | :---: | :---: | :---: | :---: | :---: | :---: | :---: |
| **S1 (旗舰)** | 杰理 AC6973D8 | ✅ (多档深度) | ✅ | ✅ (38ms) | - | - | - | ✅ | ✅ (15/30/60m) |
| **B6 (电竞)** | 杰理 AC697N | ✅ | ✅ | ✅ | ✅ (呼吸/常亮/调速) | - | - | ✅ | - |
| **B1** | 杰理 AC6973D8 | ✅ | ✅ | ✅ | - | - | - | - | - |
| **S10** | 杰理 AC6973D8 | - (半入耳) | ✅ | ✅ | - | - | - | - | - |
| **S11 / Pro** | 瑞昱 RTL8773 / 杰理 | ✅ | ✅ | ✅ | - | ✅ (Hi-Res) | - | - | - |
| **B8** | 杰理 (内置G-Sensor) | - | ✅ | ✅ | - | - | ✅ (手势晃动) | - | - |
| **B14** | 杰理 / 炬力 | - | ✅ | ✅ | - | ✅ | - | ✅ | - |
| **Y1** | 炬力 ATS3015 | ✅ | ✅ | ✅ | - | - | - | - | - |
| **Y7 / Y7 Max** | 炬力 / 杰理 | ✅ | ✅ | ✅ | - | - | - | - | - |
| **Y8 / Y9** | 炬力 / 杰理 | ✅ | ✅ | ✅ | - | - | - | - | - |
| **CH500** | 瑞昱 BBPro (头戴) | ✅ | ✅ | ✅ | - | - | - | - | - |
| **WF/WS200** | 瑞昱 LDAC (运动挂耳)| ✅ | ✅ | ✅ | - | ✅ | - | - | - |
| **PRO (通用)**| 自动自适应 | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |

App 依据广播中的官方 VendorId **自动识别机型和能力矩阵**（如 `15377=S1`、`15889=S7`、`15895=S10`、`15921/16017=B1`、`15985/16193=Y1`），不提供手动型号覆盖；未知机型回退到「PRO 通用全能」，并按能力矩阵**按需折叠界面功能**（例如非灯效机型不显示 RGB 卡片）。

---

## 🛠️ 项目结构

```text
sibyl-bluetooth-deaktop/
├── src/
│   ├── SibylEarbuds.Core/                    # 跨平台核心协议库 (.NET 8, 平台无关)
│   │   ├── Models/                           # 状态与业务模型 (ANC, EQ, 按键, 灯光)
│   │   ├── Protocol/                         # 数据帧封包/解析/校验/UUID定义/设备判定
│   │   ├── Dispatcher/                       # 防音频打架调度器 (Debounce限流)
│   │   ├── Services/                         # 耳机状态管理服务
│   │   └── Transport/                        # 传输接口与 Mock 虚拟模拟器
│   └── SibylEarbuds.App/                     # Windows 10/11 WinUI 3 客户端
│       ├── Themes/                           # 语义化主题刷子 (Light/Dark/HighContrast)
│       ├── Views/                            # DeviceDiscoveryPage / DeviceDashboardPage
│       ├── Controls/                         # EarbudVisualControl 矢量耳机电量控件
│       ├── ViewModels/                       # MVVM (主视图模型 + 命令)
│       ├── Converters/                       # x:Bind 静态辅助 (Visibility/格式化)
│       ├── Services/                         # 内置音效播放 / 配置持久化
│       ├── Bluetooth/                        # WinRT BLE 适配层 + 传输工厂
│       ├── App.xaml / MainWindow.xaml        # 应用与窗口 (Mica + 自定义标题栏)
│       └── ShellPage.xaml                    # 顶栏 + Frame 导航 + 高级抽屉
├── tests/
│   └── SibylEarbuds.Core.Tests/              # 自动化单元测试 (xUnit, 协议帧/设备判定)
├── .github/workflows/build-and-release.yml   # CI: 单元测试 + win-x64/win-arm64 自包含发布
├── build-windows.ps1 / .bat                  # 本地一键构建脚本
└── README.md
```

---

## 🚀 快速开始

### 方式一：GitHub Actions 一键出包（推荐）

推送到 `main` 或打 tag（`v*`）后，流水线自动在 `windows-latest` 上完成：

1. `dotnet test` 运行核心协议单元测试；
2. `dotnet publish -c Release -r win-x64 / win-arm64 --self-contained ...` 产出绿色分发包；
3. 自动上传 Artifact / GitHub Release（`SibylEarbudsManager-win-x64.zip`、`-arm64.zip`）。

### 方式二：在 Windows 10/11 本地构建

1. 安装 [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)（windows-latest 内置，本地需 ≥ 8.0）。
2. 双击运行根目录 `build-windows.bat`（或 PowerShell 执行 `.\build-windows.ps1 [-Arch x64|arm64]`）。
3. 产物位于 `./dist/<rid>/SibylEarbudsManager.exe`，双击即用（自包含，无需运行时）。

### 方式三：Visual Studio 2022

打开 `SibylEarbuds.sln`，解决方案平台选择 **x64**（或 ARM64），直接 F5 运行调试。WinUI 3 工程不支持 Any CPU。

### 方式四：仅运行核心协议测试（跨平台可跑）

```bash
dotnet test tests/SibylEarbuds.Core.Tests/SibylEarbuds.Core.Tests.csproj -c Release
```

> `SibylEarbuds.Core` 为纯 `.NET 8` 平台无关库，Linux/macOS 上也可编译并执行上述测试。

---

## 🧩 技术要点速记（WinUI 3 迁移结论）

* **窗口尺寸**：WinUI 3 没有 `SizeToContent`，按技能规范在 `MainWindow` 构造时以 `GetDpiForWindow` 换算物理像素后 `AppWindow.Resize`（1200×820 DIP）。
* **数据绑定**：全面使用编译期强类型 `x:Bind`（默认 `OneTime`，读写均显式标注 `OneWay/TwoWay`）；`Visibility` 依赖 x:Bind 内置 bool→Visibility 转换；格式化走静态辅助函数（`DisplayHelper`）而非常规 `IValueConverter`。
* **主题**：所有颜色通过 `{ThemeResource}` 语义化刷子（浅色/深色/高对比度三套 `ThemeDictionaries`），不使用硬编码色值。
* **控件**：0 个手写 `ControlTemplate`，全部基于 WinUI 默认模板 + 轻量样式覆盖；移除 WPF 专用的 `CommandManager`、`MediaPlayer`、`UniformGrid`，分别替换为手动 `CanExecuteChanged`、`Windows.Media.Playback.MediaPlayer`、`Grid` 等分。

---

## 📋 APK 逆向协议细节速记

| 指令类别 | Command ID (Hex) | SubCmd | 数据格式 (Payload) | 说明 |
| :--- | :--- | :--- | :--- | :--- |
| **ANC 降噪** | `0x09` | `0x01` (Set) | `[Mode (1B)], [Depth (1B)]` | Mode: 1=ANC, 2=Normal, 3=Transparency |
| **音乐均衡器** | `0x02` | `0x01` (Set) | `[Band0..Band9 (10B signed)]` | 10 段增益 (-8 ~ +8 dB) |
| **按键手势** | `0x01` | `0x01` (Set) | `[Left1..4, Right1..4 (8B)]` | 左右耳4个手势动作映射 |
| **游戏模式** | `0x0E` | `0x01` (Set) | `[0x00 / 0x01]` | 0: 正常, 1: 低延迟游戏模式 |
| **灯效控制** | `0x0A` | `0x01` (Set) | `[Mode, Spd, Bri, R, G, B]` | 模式/速度/亮度/色彩 |
| **寻找耳机** | `0x03` | `0x01` (Set) | `[0x00 / 0x01]` | 1: 响铃, 0: 停止 |
| **防误触** | `0x07` | `0x01` (Set) | `[0x00 / 0x01]` | 1: 禁用触控, 0: 启用 |
| **定时关机** | `0x20` | `0x01` (Set) | `[Minutes (1B)]` | 关机延时 (0 为不开启) |
| **状态查询** | `0x00` | `0x02` (Get) | `[]` | 耳机回报当前电量与工作模式 |

> 完整封包/解析逻辑见 `SibylEarbuds.Core/Protocol/*.cs`，帧格式 `[0xFF][Seq][Len][CmdId][Payload...][0xAA]`，由 `PacketProtocolTests` 覆盖回归。

---

## ⚠️ 已知约束

* WinUI 3 工程仅支持 **x64 / ARM64** 平台（Any CPU 会出现 MSB 平台错误），勿以 AnyCPU 构建。
* 需要 Windows 10 1809+；Mica 材质体验依赖 Windows 11（旧系统自动回退纯色背景）。
* 真实蓝牙控制依赖 Microsoft 蓝牙适配器 GATT 支持；无硬件时请使用高级抽屉中的「虚拟设备模拟器」。