# SIBYL 耳机 Windows 10/11 电脑版状态调整中心 (SIBYL Earbuds Manager)

基于 **.NET 8 (C#) / WPF** 构建的现代 Windows 10/11 桌面耳机控制应用。通过对 Android 客户端（`SIBYL MUSIC_1.23.apk`）的逆向反编译与协议分析，完整还原了 SIBYL 系列及主流双模蓝牙耳机（杰理/瑞昱/炬力方案）的核心控制协议，并提供了 Windows 11 Fluent 风格的现代化图形交互界面。

---

## 🛡️ 核心保障：为什么指令发送“绝对不会和音频播放打架”？

很多 Windows 下的第三方蓝牙耳机工具会导致音乐播放**卡顿、爆音甚至音质暴跌**（瞬间变成电话通话般的窄带单声道），本项目从底层机制上彻底解决了这一问题：

1. **物理与协议栈完全隔离（Physical & Protocol Decoupling）**：
   * **音频传输**：完全托管给 Windows 10/11 系统原生音频服务（Windows Audio Engine），走经典蓝牙 **A2DP（高级音频分发协议）** 高清立体声链路。本 App **不触碰任何音频流，绝不申请虚拟音频端点**。
   * **指令控制**：仅使用 Windows 原生 **WinRT BLE（低功耗蓝牙）GATT 特征通道** 发送轻量状态控制包。在 Windows 底层，BLE 与 A2DP 属于完全独立的子系统，时隙调度互不干扰。
2. **零串口依赖（严禁 SPP/RFCOMM）**：
   * 绝不创建或占用任何虚拟 COM 串口（SPP），绝不触发通话协议（HFP/HSP），杜绝系统将立体声音质降级为免提电话音。
3. **80ms 智能防抖与射频保护（Anti-Conflict Command Dispatcher）**：
   * 当用户连续拖动 10 段 EQ 滑块或亮度时，内置防抖队列会自动合并同类指令，单次发包限制在 10~20 字节（单 MTU 范围），发完即刻释放射频，确保 A2DP 音频缓冲区始终饱满。
4. **Write-Without-Response（免等待写入优先）**：
   * 优先采用无阻塞异步写入，不占用 Windows 主线程与 GATT 同步等待时间。

---

## 🎧 核心功能清单

* **🔋 电池电量状态监控**：
  * 左耳 (L)、右耳 (R)、充电仓独立实时电量显示。
  * 充电状态动态提示（⚡）。
* **🛡️ 降噪控制中心 (ANC)**：
  * 降噪 (ANC ON)：支持深度降噪 (100%) 与舒适降噪 (50%)。
  * 普通关闭 (Normal / OFF)。
  * 通透模式 (Transparency)。
* **🎛️ 10 段专业音乐均衡器 (EQ)**：
  * 频点：`31Hz, 62Hz, 125Hz, 250Hz, 500Hz, 1kHz, 2kHz, 4kHz, 8kHz, 16kHz`。
  * 增益调节：`-8dB ~ +8dB`。
  * 内置丰富预设：经典默认、摇滚 (Rock)、抒情 (Lyric)、流行 (Pop)、超重低音 (Bass)、人声 (Vocal)、自定义 (Custom)。
* **🎮 游戏低延迟模式**：
  * 一键开启 38ms 超低延迟电竞模式。
* **🖐️ 按键与触控自定义**：
  * 左右耳分别配置手势：单击、双击、三击、长按。
  * 动作映射：播放/暂停、上一曲、下一曲、唤醒语音助手、音量+、音量-、切换降噪、游戏模式切换、EQ音效循环。
* **💡 RGB 炫彩灯效控制**：
  * 模式切换：常亮、呼吸、关闭。
  * 呼吸速度 (0~7) 与 亮度 (0~100%) 连续滑块调节。
* **⏱️ 实用辅助工具**：
  * 触控锁定（防误触保护）。
  * 寻找耳机（播放定位警报提示音）。
  * 定时自动关机（15分钟、30分钟、60分钟）。
  * 恢复默认设置 / 恢复出厂设置。
* **🔄 双模式自由切换**：
  * **真实蓝牙模式 (WinRT BLE)**：直接与物理耳机握手。
  * **虚拟耳机模拟器 (Mock Mode)**：无需硬件即可即时演示全部 UI 与状态回路，适合非 Windows 或未携带耳机的开发调试场景。

---

## 🛠️ 项目结构

```text
sibyl-bluetooth-deaktop/
├── src/
│   ├── SibylEarbuds.Core/              # 跨平台核心协议库 (.NET 8)
│   │   ├── Models/                     # 状态与业务模型 (ANC, EQ, 按键, 灯光)
│   │   ├── Protocol/                   # 数据帧封包/解析/校验/UUID定义
│   │   ├── Dispatcher/                 # 防音频打架调度器 (Debounce限流)
│   │   ├── Services/                   # 耳机状态管理服务
│   │   └── Transport/                  # 传输接口与 Mock 虚拟模拟器
│   └── SibylEarbuds.App/               # Windows 10/11 WPF 客户端
│       ├── Bluetooth/                  # WinRT BLE 适配层 (Windows.Devices.Bluetooth)
│       ├── Controls/                   # 现代环形电量条等控件
│       ├── ViewModels/                 # MVVM 视图模型
│       ├── Converters/                 # XAML 数据转换器
│       ├── MainWindow.xaml             # Windows 11 Fluent 界面
│       └── App.xaml
├── tests/
│   └── SibylEarbuds.Core.Tests/        # 自动化单元测试 (xUnit)
├── build-windows.bat                   # Windows 批处理一键打包脚本
├── build-windows.ps1                   # Windows PowerShell 一键打包脚本
└── README.md
```

---

## 🚀 快速开始

### 方式一：在 Windows 10/11 上一键打包与运行

1. 确保电脑已安装 [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)。
2. 双击运行根目录下的 `build-windows.bat`（或在 PowerShell 中执行 `.\build-windows.ps1`）。
3. 编译完成后，打开 `./dist` 目录，直接双击运行生成的 `SibylEarbudsManager.exe` 即可！

### 方式二：使用 Visual Studio 打开

直接使用 **Visual Studio 2022** 打开 `SibylEarbuds.sln`，按 `F5` 即可运行与断点调试。

### 方式三：运行核心协议自动化测试

```bash
dotnet test
```

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
