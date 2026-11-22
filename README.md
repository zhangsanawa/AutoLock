# AutoLock

> 空闲 N 分钟后自动锁屏，**不关屏幕、不断蓝牙**。
> Windows 10 / 11 小工具，单文件、无驱动、无后台服务、不联网。

![平台](https://img.shields.io/badge/平台-Windows%2010%20%7C%2011-0078D4)
![运行时](https://img.shields.io/badge/运行时-.NET%20Framework%204.8-512BD4)
![许可](https://img.shields.io/badge/许可-MIT-green)
![大小](https://img.shields.io/badge/安装包-2.4%20MB-blue)
![仓库](https://img.shields.io/badge/GitHub-zhangsanawa%2FAutoLock-181717?logo=github)

---

## 📥 下载安装

**推荐：下载安装包，双击即可**

👉 **[AutoLock-Setup-1.0.0.exe](https://github.com/zhangsanawa/AutoLock/releases/download/v1.0.0/AutoLock-Setup-1.0.0.exe)** （2.4 MB）

仓库地址：https://github.com/zhangsanawa/AutoLock

安装向导会询问安装位置、是否开机自启、是否创建桌面快捷方式，
并在系统「应用和功能」里注册卸载项，可随时干净卸载。

> 安装包没有代码签名证书，Windows SmartScreen 可能提示「已保护你的电脑」。
> 点 **更多信息 → 仍要运行** 即可。源码在仓库里，可自行审查或编译。

**或者：绿色版**

直接下载 [`AutoLock.exe`](AutoLock.exe) 双击运行，不写系统目录。

---

## 这个软件解决什么问题

如果你符合下面这段描述，这个工具就是为你写的：

> 我的电脑**不会自动锁屏**（电源计划里"关闭显示"和"睡眠"都是"从不"），
> 但只要一进入睡眠，**屏幕和蓝牙设备就会断联**——因为蓝牙是 USB 适配器，一睡就断电。
> 我想要的只是：**离开电脑 N 分钟后屏幕锁上，但屏幕别黑、蓝牙别断。**

### 为什么系统自带方案不行

| 你想要的 | Windows 自带方案 | 为什么不行 |
|---|---|---|
| 离开 N 分钟自动锁屏 | 屏幕保护程序 +"在恢复时显示登录屏幕" | 要手动勾选；"空白"屏保往往同时关屏 |
| 锁屏但屏幕保持点亮 | 没有原生选项 | 锁屏后电源计划的关屏定时器照样生效 |
| 别让电脑睡眠 | 电源计划设"从不" | 设成"从不"后，**连自动锁屏也没了** |
| 蓝牙别断 | 换有线音箱 | 治标不治本 |

### AutoLock 的做法

只在"空闲时间到"这一个条件上挂一个锁屏动作，同时持续调用
`SetThreadExecutionState` 告诉系统"有人需要这块屏幕"，所以锁定之后
屏幕不会黑、电脑不会睡、USB 蓝牙适配器不会断电。

```
你现在的状态：
  空闲 10 分钟 → 睡眠(S3) → 整机断电 → 蓝牙适配器断电 → 蓝牙设备断联 + 屏幕黑

用了 AutoLock 之后：
  空闲 10 分钟 → 锁屏(Win+L) → 系统仍在运行 → 蓝牙保持连接 + 屏幕保持点亮
```

> **注意**：AutoLock 不改你的电源计划。手动点"睡眠"或合盖，蓝牙该断还是断。
> 它管的是"**空闲**导致的自动行为"。

---

## ✨ 功能

- 空闲 **1 / 2 / 3 / 5 / 10 / 15 / 20 / 30 / 45 / 60 分钟** 可调，点一下立即生效
- **锁定期间保持屏幕常亮**：锁定后屏幕不黑、电脑不睡，蓝牙不断
- **全屏播放时不锁定**：看电影、打游戏时不会突然被锁
- **状态一眼可见**：每个开关前面有 `●` / `○`，顶部状态栏实时显示当前配置
- **双击托盘图标 = 立即锁定**，等效 Win+L
- **开机自启**：带 `--delay` 参数等资源管理器就绪后再注册图标
- **高 DPI 支持**：125% / 150% / 200% 缩放下图标和菜单都不糊
- **资源管理器崩溃后自动恢复图标**：监听 `TaskbarCreated` 消息
- 单文件、无安装依赖、无服务、无驱动、不联网

---

## 系统要求

| 项目 | 要求 |
|---|---|
| 操作系统 | Windows 10 1607+ / Windows 11 |
| 运行时 | **.NET Framework 4.8**（Windows 10 1903+ 与 Windows 11 系统自带） |
| 权限 | 首次运行弹出 UAC 保护，点"是" |
| 磁盘 | 安装后约 1 MB |

### 如果没有 .NET Framework 4.8

只有 Windows 10 1809 及更早、或精简版系统才需要手动装：

- 在线安装：https://dotnet.microsoft.com/download/dotnet-framework/net48
- 离线包：https://go.microsoft.com/fwlink/?LinkId=2088631

装完重启一次。检查版本：`Win+R` → `regedit` → 查看
`HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full` 的 `Release`：

| Release | 版本 |
|---|---|
| 533320 | 4.8.1 ✅ |
| ≥ 528040 | 4.8 ✅ |
| 461808–528039 | 4.7.2（需升级） |

---

## 为什么首次运行需要管理员权限

不是程序想要更多权限，而是 Windows 的**用户界面特权隔离（UIPI）**：

> 未提权的进程无法把图标注册进已提权的 explorer.exe 通知区域。
> 如果系统里的 explorer 以管理员身份运行，普通权限的托盘程序图标就都不会显示。

AutoLock 检测到会自动请求提权（弹 UAC），点"是"即可。

**不想每次弹 UAC？** 用计划任务以最高权限静默启动（配一次即可）：

```powershell
schtasks /create /tn "AutoLock" /tr "\"C:\Program Files\AutoLock\AutoLock.exe\" --delay" /sc onlogon /rl HIGHEST /f
```

---

## 使用方法

安装后从开始菜单或桌面启动，任务栏右下角出现**蓝色小锁**（可能在 `^` 折叠区里）。右键调整设置。

### 托盘菜单

```
● 运行中 · 空闲 10 分钟锁定 · 屏幕常亮
─────────────────────────────
  立即锁定（等效 Win+L）
✓ ●  启用自动锁定
  ✓ 空闲时间：10 分钟  ▸
✓ ●  锁定期间保持屏幕常亮（不休眠）
✓ ●  全屏播放时不锁定
─────────────────────────────
○ 开机自动启动
─────────────────────────────
  退出
```

`●` = 已开启，`○` = 已关闭。双击托盘图标 = 立即锁定。

### 配置文件

程序目录下的 `config.ini`（不可写时自动退到 `%LOCALAPPDATA%\AutoLock\`）：

```ini
Minutes=10          # 空闲多少分钟锁定
Enabled=1           # 1 启用 / 0 暂停
KeepScreenOn=1      # 1 锁定后保持屏幕常亮（同时阻止睡眠）
SkipFullscreen=1    # 1 全屏应用时不锁定
```

### 命令行参数

```
AutoLock.exe                      正常启动
AutoLock.exe --minutes 30         本次以 30 分钟阈值启动
AutoLock.exe --elevate            未提权时自动请求管理员权限
AutoLock.exe --delay              等通知区域就绪后再注册图标（开机自启用）
AutoLock.exe --dry-run            只写日志不真的锁定（测试用）
AutoLock.exe --selftest           打印配置、DPI、空闲时间后退出
AutoLock.exe --scan --seconds 60  实时打印空闲秒数和前台窗口信息，不锁定
```

### 日志

程序目录下的 `autolock.log`：

```
2026-11-22 18:30:03  启动  阈值=10min  Enabled=True  KeepScreenOn=True  SkipFullscreen=True
2026-11-22 18:40:15  锁定  空闲 11 分钟
```

---

## 🔧 从源码编译

### 编译主程序（无需 Visual Studio）

双击 `build.bat`，或手动执行：

```bat
set FW=%SystemRoot%\Microsoft.NET\Framework64\v4.0.30319
%FW%\csc.exe /nologo /target:winexe /platform:anycpu /optimize+ ^
  /out:AutoLock.exe /win32icon:app.ico /win32manifest:app.manifest ^
  /resource:app.ico,app.ico ^
  /r:%FW%\System.dll /r:%FW%\System.Drawing.dll ^
  /r:%FW%\System.Windows.Forms.dll /r:%FW%\System.Core.dll AutoLock.cs
```

### 编译安装包

需要 [Inno Setup 7](https://jrsoftware.org/isdl.php)：

```bat
"%LOCALAPPDATA%\Programs\Inno Setup 7\ISCC.exe" installer\AutoLock.iss
```

产物在 `installer\dist\`。

---

## ⚠️ 已知限制

- **不改电源计划**：手动点"睡眠"蓝牙仍会断，请把"在此时间后睡眠"设为"从不"
- **不阻止你手动关显示器**：`KeepScreenOn=1` 只拦系统的自动关屏定时器
- **只对当前用户会话生效**：不处理其他用户、锁屏界面、UAC 安全桌面
- **全屏判定基于窗口矩形**：极少数无边框全屏窗口可能误判，可用 `SkipFullscreen=0` 关闭

---

## 需要相反功能？

如果你想让电脑**别睡**：

- [PowerToys Awake](https://learn.microsoft.com/windows/powertoys/awake) — 微软官方
- [Caffeine](https://www.zhornsoftware.co.uk/caffeine/) — 经典免费小工具
- [Don't Sleep](https://www.nirsoft.net/utils/dont_sleep.html) — NirSoft

---

## 项目结构

```
AutoLock/
├── AutoLock.cs            主程序源码（单文件，约 900 行）
├── AutoLock.exe           编译好的绿色版
├── AutoLock.exe.config    高 DPI 配置
├── app.manifest           DPI 感知清单
├── app.ico                图标
├── icon.svg               图标矢量源文件
├── build.bat              一键编译主程序
├── installer/
│   ├── AutoLock.iss       Inno Setup 安装脚本
│   └── default.ini        默认配置
├── dist/
│   └── AutoLock-Setup-1.0.0.exe   安装包
├── README.md
└── LICENSE
```

---

## 许可

MIT License，见 [LICENSE](LICENSE)。

---

## 🔍 关键词

**中文**：自动锁屏 / 空闲锁屏 / 无操作锁屏 / 挂机锁屏 / 不黑屏锁屏 / 蓝牙不断 /
USB 蓝牙适配器 / 睡眠蓝牙断联 / 蓝牙设备断联 / 自动睡眠关屏 / 屏保替代 / 挂机不断网

**English**: auto lock screen / idle lock / lock workstation after idle /
keep screen on while locked / prevent sleep but lock / bluetooth disconnect on sleep /
USB bluetooth dongle sleep / windows 10 auto lock / screensaver alternative