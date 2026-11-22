using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using Microsoft.Win32;

namespace AutoLockApp
{
    internal static class Native
    {
        [StructLayout(LayoutKind.Sequential)]
        public struct LASTINPUTINFO { public uint cbSize; public uint dwTime; }

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT { public int Left, Top, Right, Bottom; }

        [DllImport("user32.dll")]
        public static extern bool GetLastInputInfo(ref LASTINPUTINFO plii);

        [DllImport("user32.dll")]
        public static extern bool LockWorkStation();

        [DllImport("user32.dll")]
        public static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        public static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("user32.dll")]
        public static extern IntPtr GetShellWindow();

        [DllImport("user32.dll")]
        public static extern IntPtr GetDesktopWindow();

        [StructLayout(LayoutKind.Sequential)]
        public struct MONITORINFO
        {
            public int cbSize;
            public RECT rcMonitor;
            public RECT rcWork;
            public uint dwFlags;
        }

        [DllImport("user32.dll")]
        public static extern IntPtr MonitorFromWindow(IntPtr hwnd, uint dwFlags);

        [DllImport("user32.dll")]
        public static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFO lpmi);

        [DllImport("user32.dll")]
        public static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("kernel32.dll")]
        public static extern uint SetThreadExecutionState(uint esFlags);

        public const uint ES_SYSTEM_REQUIRED = 0x00000001;
        public const uint ES_DISPLAY_REQUIRED = 0x00000002;
        public const uint ES_CONTINUOUS = 0x80000000;

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr FindWindow(string cls, string win);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr FindWindowEx(IntPtr parent, IntPtr child, string cls, string win);

        // 通知区域图标栏是否已建好
        [DllImport("user32.dll")]
        private static extern bool SetProcessDPIAware();

        public static void SetProcessDpiAware()
        {
            try { SetProcessDPIAware(); } catch { }
        }

        public static bool IsTrayReady()
        {
            IntPtr shell = FindWindow("Shell_TrayWnd", null);
            if (shell == IntPtr.Zero) return false;
            IntPtr notify = FindWindowEx(shell, IntPtr.Zero, "TrayNotifyWnd", null);
            if (notify == IntPtr.Zero) return false;
            IntPtr pager = FindWindowEx(notify, IntPtr.Zero, "SysPager", null);
            if (pager == IntPtr.Zero) return false;
            IntPtr bar = FindWindowEx(pager, IntPtr.Zero, "ToolbarWindow32", null);
            return bar != IntPtr.Zero;
        }

        public static double IdleSeconds()
        {
            LASTINPUTINFO li = new LASTINPUTINFO();
            li.cbSize = (uint)Marshal.SizeOf(typeof(LASTINPUTINFO));
            if (!GetLastInputInfo(ref li)) return 0;
            uint now = unchecked((uint)Environment.TickCount);
            uint diff = unchecked(now - li.dwTime);
            return diff / 1000.0;
        }

        public static bool IsFullscreenForeground()
        {
            string ignored;
            return IsFullscreenForeground(out ignored);
        }

        // 真正的全屏 = 窗口完全覆盖它所在的显示器，且没有标题栏（最大化窗口不算）
        public static bool IsFullscreenForeground(out string info)
        {
            info = "";
            IntPtr h = GetForegroundWindow();
            if (h == IntPtr.Zero) { info = "无前台窗口"; return false; }
            if (h == GetShellWindow() || h == GetDesktopWindow()) { info = "桌面"; return false; }

            RECT r;
            if (!GetWindowRect(h, out r)) { info = "取窗口矩形失败"; return false; }

            MONITORINFO mi = new MONITORINFO();
            mi.cbSize = Marshal.SizeOf(typeof(MONITORINFO));
            IntPtr mon = MonitorFromWindow(h, 2); // MONITOR_DEFAULTTONEAREST
            if (!GetMonitorInfo(mon, ref mi)) { info = "取显示器信息失败"; return false; }

            bool covers = r.Left <= mi.rcMonitor.Left && r.Top <= mi.rcMonitor.Top
                       && r.Right >= mi.rcMonitor.Right && r.Bottom >= mi.rcMonitor.Bottom;
            int style = GetWindowLong(h, -16); // GWL_STYLE
            bool hasCaption = (style & 0x00C00000) != 0; // WS_CAPTION

            info = "窗口(" + r.Left + "," + r.Top + "," + r.Right + "," + r.Bottom + ")"
                 + " 显示器(" + mi.rcMonitor.Left + "," + mi.rcMonitor.Top + ","
                 + mi.rcMonitor.Right + "," + mi.rcMonitor.Bottom + ")"
                 + " 有标题栏=" + hasCaption;
            return covers && !hasCaption;
        }
    }

    internal sealed class Config
    {
        public int Minutes = 10;
        public bool Enabled = true;
        public bool KeepScreenOn = true;
        public bool SkipFullscreen = true;

        private static string dirCache;

        public static string Dir
        {
            get
            {
                if (dirCache != null) return dirCache;

                // 便携模式：优先把配置和日志放在程序所在目录
                try
                {
                    string local = Path.GetDirectoryName(Application.ExecutablePath);
                    string probe = Path.Combine(local, ".writable");
                    File.WriteAllText(probe, "x");
                    File.Delete(probe);
                    dirCache = local;
                    return dirCache;
                }
                catch { }

                string d = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "AutoLock");
                try { Directory.CreateDirectory(d); } catch { }
                dirCache = d;
                return dirCache;
            }
        }

        public static string FilePath { get { return Path.Combine(Dir, "config.ini"); } }
        public static string LogPath { get { return Path.Combine(Dir, "autolock.log"); } }

        public static Config Load()
        {
            Config c = new Config();
            try
            {
                if (File.Exists(FilePath))
                {
                    foreach (string line in File.ReadAllLines(FilePath))
                    {
                        string s = line.Trim();
                        if (s.Length == 0 || s.StartsWith("#")) continue;
                        int eq = s.IndexOf('=');
                        if (eq < 0) continue;
                        string k = s.Substring(0, eq).Trim().ToLowerInvariant();
                        string v = s.Substring(eq + 1).Trim();
                        bool b = v == "1" || v.Equals("true", StringComparison.OrdinalIgnoreCase);
                        int n;
                        switch (k)
                        {
                            case "minutes": if (int.TryParse(v, out n) && n > 0) c.Minutes = n; break;
                            case "enabled": c.Enabled = b; break;
                            case "keepscreenon": c.KeepScreenOn = b; break;
                            case "skipfullscreen": c.SkipFullscreen = b; break;
                        }
                    }
                }
            }
            catch { }
            if (c.Minutes < 1) c.Minutes = 1;
            return c;
        }

        public void Save()
        {
            try
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendLine("# AutoLock 配置文件，改完保存后重启程序生效");
                sb.AppendLine("Minutes=" + Minutes);
                sb.AppendLine("Enabled=" + (Enabled ? "1" : "0"));
                sb.AppendLine("KeepScreenOn=" + (KeepScreenOn ? "1" : "0"));
                sb.AppendLine("SkipFullscreen=" + (SkipFullscreen ? "1" : "0"));
                File.WriteAllText(FilePath, sb.ToString(), Encoding.UTF8);
            }
            catch { }
        }
    }

    internal static class Log
    {
        private static readonly object Gate = new object();

        public static void Write(string msg)
        {
            lock (Gate)
            {
                try
                {
                    string line = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "  " + msg
                        + Environment.NewLine;
                    File.AppendAllText(Config.LogPath, line, Encoding.UTF8);
                }
                catch { }
            }
        }
    }

    internal static class IconLoader
    {
        private static Icon cached;
        public static string Source = "(未加载)";

        public static Icon ForTray()
        {
            if (cached != null) return cached;

            // 1) 内嵌资源
            try
            {
                Assembly asm = Assembly.GetExecutingAssembly();
                foreach (string res in asm.GetManifestResourceNames())
                {
                    if (res.EndsWith("app.ico", StringComparison.OrdinalIgnoreCase))
                    {
                        using (Stream s = asm.GetManifestResourceStream(res))
                        {
                            if (s != null)
                            {
                                Size need = SystemInformation.SmallIconSize;
                                try { cached = new Icon(s, need.Width, need.Height); }
                                catch { s.Position = 0; cached = new Icon(s); }
                                Source = "内嵌资源:" + res + " @ " + need.Width + "px";
                                return cached;
                            }
                        }
                    }
                }
            }
            catch (Exception ex) { Source = "内嵌失败:" + ex.Message; }

            // 2) 同目录 app.ico
            try
            {
                string local = Path.Combine(Path.GetDirectoryName(Application.ExecutablePath), "app.ico");
                if (File.Exists(local))
                {
                    Size need = SystemInformation.SmallIconSize;
                    try { cached = new Icon(local, need.Width, need.Height); }
                    catch { cached = new Icon(local); }
                    Source = "文件:" + local + " @ " + need.Width + "px";
                    return cached;
                }
            }
            catch (Exception ex) { Source += " / 文件失败:" + ex.Message; }

            // 3) 系统图标兜底
            cached = SystemIcons.Shield;
            Source = "系统图标兜底";
            return cached;
        }
    }

    internal sealed class TaskbarWatcher : NativeWindow
    {
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern uint RegisterWindowMessage(string lpString);

        private readonly int msg;
        private readonly Action callback;

        public TaskbarWatcher(Action onCreated)
        {
            callback = onCreated;
            msg = unchecked((int)RegisterWindowMessage("TaskbarCreated"));
            CreateHandle(new CreateParams());
        }

        protected override void WndProc(ref Message m)
        {
            if (msg != 0 && m.Msg == msg)
            {
                try { callback(); } catch { }
            }
            base.WndProc(ref m);
        }

        public void Stop()
        {
            try { DestroyHandle(); } catch { }
        }
    }

    internal sealed class App : ApplicationContext
    {
        private readonly Config cfg;
        private readonly bool dryRun;
        private readonly NotifyIcon tray;
        private readonly System.Windows.Forms.Timer timer;
        private readonly ToolStripMenuItem statusItem;
        private readonly ToolStripMenuItem lockNowItem;
        private readonly ContextMenuStrip menu;
        private readonly ToolStripMenuItem enabledItem;
        private readonly ToolStripMenuItem screenItem;
        private readonly ToolStripMenuItem fullscreenItem;
        private readonly ToolStripMenuItem startupItem;
        private readonly ToolStripMenuItem timeMenu;
        private readonly TaskbarWatcher watcher;
        private bool lockedThisIdle;
        private bool executionStateHeld;

        public App(bool dryRun, int forcedMinutes)
        {
            this.dryRun = dryRun;
            cfg = Config.Load();
            if (forcedMinutes > 0) cfg.Minutes = forcedMinutes;

            statusItem = new ToolStripMenuItem();
            statusItem.Enabled = false;

            enabledItem = new ToolStripMenuItem("", null, OnToggleEnabled);
            screenItem = new ToolStripMenuItem("", null, OnToggleScreen);
            fullscreenItem = new ToolStripMenuItem("", null, OnToggleFullscreen);
            startupItem = new ToolStripMenuItem("", null, OnToggleStartup);
            lockNowItem = new ToolStripMenuItem("立即锁定", null, OnLockNow);

            menu = new ContextMenuStrip();
            menu.DropShadowEnabled = true;
            menu.AutoSize = true;
            // 必须保留图片/复选边距，否则勾选标记会被裁掉，看不出开关状态
            menu.ShowImageMargin = true;
            menu.ShowCheckMargin = true;
            try { menu.Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Regular, GraphicsUnit.Point); }
            catch { }
            try { menu.RenderMode = ToolStripRenderMode.System; } catch { }
            menu.Items.Add(statusItem);
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add(lockNowItem);
            menu.Items.Add(enabledItem);

            timeMenu = new ToolStripMenuItem();
            int[] presets = new int[] { 1, 2, 3, 5, 10, 15, 20, 30, 45, 60 };
            foreach (int m in presets)
            {
                int captured = m;
                ToolStripMenuItem it = new ToolStripMenuItem(m + " 分钟", null, delegate { OnPickMinutes(captured); });
                it.Tag = m;
                it.CheckOnClick = false;
                timeMenu.DropDownItems.Add(it);
            }
            menu.Items.Add(timeMenu);

            menu.Items.Add(screenItem);
            menu.Items.Add(fullscreenItem);
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add(startupItem);
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add(new ToolStripMenuItem("退出", null, OnExit));

            Native.SetProcessDpiAware();

            tray = new NotifyIcon();
            tray.Icon = IconLoader.ForTray();
            tray.Text = BuildTooltip();
            tray.ContextMenuStrip = menu;
            tray.DoubleClick += delegate { OnLockNow(null, EventArgs.Empty); };
            try { tray.ShowBalloonTip(3000, "AutoLock 已启动", BuildTooltip(), ToolTipIcon.Info); } catch { }
            tray.Visible = true;

            watcher = new TaskbarWatcher(ReShowIcon);

            timer = new System.Windows.Forms.Timer();
            timer.Interval = 5000;
            timer.Tick += OnTick;
            timer.Start();

            RefreshMenu();
            ApplyExecutionState();
            EnsureIconRegistered();
            Log.Write("启动  阈值=" + cfg.Minutes + "min  Enabled=" + cfg.Enabled
                + "  KeepScreenOn=" + cfg.KeepScreenOn + "  SkipFullscreen=" + cfg.SkipFullscreen
                + (dryRun ? "  [DRY-RUN]" : ""));
        }

        private void ReShowIcon()
        {
            // Explorer 重启后托盘会被清空，必须重新注册一次
            try
            {
                tray.Visible = false;
                tray.Icon = IconLoader.ForTray();
                tray.Text = BuildTooltip();
                tray.Visible = true;
                Log.Write("托盘图标已重新注册（TaskbarCreated）");
            }
            catch (Exception ex) { Log.Write("重注册托盘失败: " + ex.Message); }
        }

        private int retryCount;

        // 兜底：验证图标是否真的进了托盘，没进去就重注册
        private void EnsureIconRegistered()
        {
            if (tray.Visible && retryCount > 6) return;
            try
            {
                if (!Native.IsTrayReady())
                {
                    retryCount++;
                    if (retryCount <= 6)
                    {
                        tray.Visible = false;
                        tray.Visible = true;
                        Log.Write("托盘尚未就绪，第 " + retryCount + " 次重试注册");
                    }
                    return;
                }
                retryCount = 0;
            }
            catch { }
        }

        // 把当前开关状态直接写进文字里，任何渲染模式下都看得见
        private static string Mark(bool on)
        {
            return on ? "●" : "○";
        }

        private void RefreshMenu()
        {
            statusItem.Text = (cfg.Enabled ? "● 运行中" : "○ 已暂停")
                + " · 空闲 " + cfg.Minutes + " 分钟锁定"
                + (cfg.KeepScreenOn ? " · 屏幕常亮" : "");

            enabledItem.Text = Mark(cfg.Enabled) + "  启用自动锁定";
            enabledItem.Checked = cfg.Enabled;

            screenItem.Text = Mark(cfg.KeepScreenOn) + "  锁定期间保持屏幕常亮（不休眠）";
            screenItem.Checked = cfg.KeepScreenOn;

            fullscreenItem.Text = Mark(cfg.SkipFullscreen) + "  全屏播放时不锁定";
            fullscreenItem.Checked = cfg.SkipFullscreen;

            bool su = IsStartupEnabled();
            startupItem.Text = Mark(su) + "  开机自动启动";
            startupItem.Checked = su;

            timeMenu.Text = "空闲时间：" + cfg.Minutes + " 分钟";

            foreach (ToolStripItem item in timeMenu.DropDownItems)
            {
                ToolStripMenuItem mi = item as ToolStripMenuItem;
                if (mi != null && mi.Tag is int)
                {
                    int v = (int)mi.Tag;
                    mi.Checked = v == cfg.Minutes;
                    mi.Text = Mark(v == cfg.Minutes) + "  " + v + " 分钟";
                }
            }

            lockNowItem.Text = "立即锁定（等效 Win+L）";
            tray.Text = BuildTooltip();
        }

        private string BuildTooltip()
        {
            string s = cfg.Enabled
                ? "AutoLock · 空闲 " + cfg.Minutes + " 分钟后锁定"
                : "AutoLock · 已暂停";
            if (s.Length > 63) s = s.Substring(0, 63);
            return s;
        }

        private static bool IsStartupEnabled()
        {
            try
            {
                using (RegistryKey k = Registry.CurrentUser.OpenSubKey(
                    @"Software\Microsoft\Windows\CurrentVersion\Run", false))
                {
                    return k != null && k.GetValue("AutoLock") != null;
                }
            }
            catch { return false; }
        }

        private void SetStartup(bool on)
        {
            try
            {
                using (RegistryKey k = Registry.CurrentUser.OpenSubKey(
                    @"Software\Microsoft\Windows\CurrentVersion\Run", true))
                {
                    if (k == null) return;
                    if (on)
                    {
                        // 加 --delay：等 Explorer 托盘就绪后再注册图标，避免开机时图标丢失
                        k.SetValue("AutoLock", "\"" + Application.ExecutablePath + "\" --delay --elevate");
                        Log.Write("已设置开机自启（带 --delay）");
                    }
                    else
                    {
                        k.DeleteValue("AutoLock", false);
                        Log.Write("已取消开机自启");
                    }
                }
            }
            catch (Exception ex) { Log.Write("设置开机启动失败: " + ex.Message); }
        }

        private void ApplyExecutionState()
        {
            if (cfg.KeepScreenOn)
            {
                Native.SetThreadExecutionState(
                    Native.ES_CONTINUOUS | Native.ES_SYSTEM_REQUIRED | Native.ES_DISPLAY_REQUIRED);
                executionStateHeld = true;
            }
            else if (executionStateHeld)
            {
                Native.SetThreadExecutionState(Native.ES_CONTINUOUS);
                executionStateHeld = false;
            }
        }

        private void OnTick(object sender, EventArgs e)
        {
            EnsureIconRegistered();
            if (cfg.KeepScreenOn) ApplyExecutionState();

            if (!cfg.Enabled) { lockedThisIdle = false; return; }

            double idle = Native.IdleSeconds();
            double threshold = cfg.Minutes * 60.0;

            if (idle < threshold)
            {
                lockedThisIdle = false;
                return;
            }
            if (lockedThisIdle) return;

            if (cfg.SkipFullscreen && Native.IsFullscreenForeground())
            {
                statusItem.Text = "● 全屏应用中，跳过锁定 · 空闲 " + ((int)(idle / 60)) + " 分钟";
                return;
            }

            lockedThisIdle = true;
            DoLock((int)(idle / 60));
        }

        private void DoLock(int idleMinutes)
        {
            if (dryRun)
            {
                Log.Write("DRY-RUN 将锁定  空闲 " + idleMinutes + " 分钟 (阈值 " + cfg.Minutes + ")");
                return;
            }
            Log.Write("锁定  空闲 " + idleMinutes + " 分钟");
            try
            {
                if (!Native.LockWorkStation()) Log.Write("LockWorkStation 返回失败");
            }
            catch (Exception ex) { Log.Write("锁定异常: " + ex.Message); }
        }

        private void OnLockNow(object sender, EventArgs e)
        {
            lockedThisIdle = true;
            DoLock((int)(Native.IdleSeconds() / 60));
        }

        private void OnToggleEnabled(object sender, EventArgs e)
        {
            cfg.Enabled = !cfg.Enabled;
            cfg.Save();
            RefreshMenu();
        }

        private void OnToggleScreen(object sender, EventArgs e)
        {
            cfg.KeepScreenOn = !cfg.KeepScreenOn;
            cfg.Save();
            ApplyExecutionState();
            RefreshMenu();
        }

        private void OnToggleFullscreen(object sender, EventArgs e)
        {
            cfg.SkipFullscreen = !cfg.SkipFullscreen;
            cfg.Save();
            RefreshMenu();
        }

        private void OnToggleStartup(object sender, EventArgs e)
        {
            SetStartup(!IsStartupEnabled());
            RefreshMenu();
        }

        private void OnPickMinutes(int minutes)
        {
            cfg.Minutes = minutes;
            cfg.Save();
            lockedThisIdle = false;
            RefreshMenu();
        }

        private void OnExit(object sender, EventArgs e)
        {
            Log.Write("退出");
            timer.Stop();
            try { watcher.Stop(); } catch { }
            tray.Visible = false;
            tray.Dispose();
            Native.SetThreadExecutionState(Native.ES_CONTINUOUS);
            ExitThread();
        }
    }

    internal static class Program
    {
        [DllImport("user32.dll")]
        private static extern bool SetProcessDpiAwarenessContext(IntPtr value);

        [DllImport("user32.dll")]
        private static extern bool SetProcessDPIAware();

        [DllImport("shell32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern IntPtr ShellExecute(IntPtr hwnd, string op, string file,
            string param, string dir, int show);

        // 当前进程是否已提权
        internal static bool IsElevated()
        {
            try
            {
                System.Security.Principal.WindowsIdentity id =
                    System.Security.Principal.WindowsIdentity.GetCurrent();
                System.Security.Principal.WindowsPrincipal p =
                    new System.Security.Principal.WindowsPrincipal(id);
                return p.IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator);
            }
            catch { return false; }
        }

        // 以管理员身份重启自己
        internal static bool RelaunchElevated(string extraArgs)
        {
            try
            {
                string dir = Path.GetDirectoryName(Application.ExecutablePath);
                IntPtr r = ShellExecute(IntPtr.Zero, "runas", Application.ExecutablePath,
                    extraArgs, dir, 1);
                return r.ToInt64() > 32;
            }
            catch { return false; }
        }

        [STAThread]
        private static void Main(string[] args)
        {
            bool dryRun = false;
            bool selfTest = false;
            bool scan = false;
            int scanSeconds = 40;
            int forcedMinutes = 0;
            bool delay = false;
            bool wantElevate = false;
            bool noElevate = false;

            for (int i = 0; i < args.Length; i++)
            {
                string a = args[i].ToLowerInvariant();
                if (a == "--delay") delay = true;
                else if (a == "--elevate") wantElevate = true;
                else if (a == "--no-elevate") noElevate = true;
                else if (a == "--dry-run") dryRun = true;
                else if (a == "--selftest") selfTest = true;
                else if (a == "--scan") scan = true;
                else if (a == "--seconds" && i + 1 < args.Length) int.TryParse(args[++i], out scanSeconds);
                else if (a == "--minutes" && i + 1 < args.Length) int.TryParse(args[++i], out forcedMinutes);
            }

            if (selfTest) { SelfTest(); return; }

            bool created;
            Mutex mutex = new Mutex(true, "AutoLock_SingleInstance_DSH", out created);
            if (!created && !scan)
            {
                MessageBox.Show("AutoLock 已经在运行了。\r\n\r\n图标在任务栏右下角的 ^ 折叠区里（蓝色小锁）。\r\n双击它可以立即锁定，右键可以调时间或退出。",
                    "AutoLock", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (scan) { Scan(scanSeconds, forcedMinutes); return; }

            // 需要管理员权限才能把图标注册进 shell 的通知区域
            if (wantElevate && !noElevate && !IsElevated())
            {
                string extra = "--elevate --no-elevate";
                if (delay) extra += " --delay";
                if (dryRun) extra += " --dry-run";
                if (forcedMinutes > 0) extra += " --minutes " + forcedMinutes;
                if (RelaunchElevated(extra))
                {
                    Log.Write("已请求提权重启");
                    return;
                }
                MessageBox.Show("需要管理员权限才能显示托盘图标，但提权被取消了。\r\n\r\n请右键 AutoLock.exe → 以管理员身份运行。",
                    "AutoLock", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // 开机自启场景：先等资源管理器把通知区域建好，否则图标注册不进去
            if (delay)
            {
                for (int i = 0; i < 120; i++)
                {
                    if (Native.IsTrayReady()) break;
                    Thread.Sleep(500);
                }
            }

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            try { SetProcessDpiAwarenessContext(new IntPtr(-4)); } catch { }
            Application.Run(new App(dryRun, forcedMinutes));
        }

        private static void Emit(string s)
        {
            Console.WriteLine(s);
            try { File.AppendAllText(Config.LogPath, s + Environment.NewLine, Encoding.UTF8); } catch { }
        }

        private static double DeviceDpiScale()
        {
            try
            {
                using (Graphics g = Graphics.FromHwnd(IntPtr.Zero))
                    return g.DpiX / 96.0;
            }
            catch { return 1.0; }
        }

        private static void SelfTest()
        {
            Config c = Config.Load();
            Emit("AutoLock 自检");
            Emit("  程序路径 : " + Application.ExecutablePath);
            Emit("  配置文件 : " + Config.FilePath);
            Emit("  日志文件 : " + Config.LogPath);
            Emit("  空闲检测 : " + (int)Native.IdleSeconds() + " 秒");
            Emit("  锁定阈值 : " + c.Minutes + " 分钟");
            Emit("  已启用   : " + c.Enabled);
            Emit("  保持常亮 : " + c.KeepScreenOn);
            Emit("  全屏跳过 : " + c.SkipFullscreen);
            Emit("  屏幕尺寸 : " + Screen.PrimaryScreen.Bounds.Width + "x" + Screen.PrimaryScreen.Bounds.Height);
            Emit("  当前全屏 : " + Native.IsFullscreenForeground());
            Icon ic = IconLoader.ForTray();
            Emit("  图标来源 : " + IconLoader.Source + "  (" + ic.Width + "x" + ic.Height + ")");
            Emit("  缩放比例 : " + (Math.Round(DeviceDpiScale() * 100)) + "%   小图标尺寸: "
                + SystemInformation.SmallIconSize.Width + "px");
        }

        private static void Scan(int seconds, int minutes)
        {
            int threshold = (minutes > 0 ? minutes : Config.Load().Minutes) * 60;
            Emit("扫描模式：阈值 " + (threshold / 60) + " 分钟，共观察 " + seconds + " 秒，不会真的锁定");
            DateTime end = DateTime.Now.AddSeconds(seconds);
            while (DateTime.Now < end)
            {
                double idle = Native.IdleSeconds();
                string flag = idle >= threshold ? "  >>> 已达阈值，将触发锁定" : "";
                string fi;
                bool fs = Native.IsFullscreenForeground(out fi);
                Emit("  空闲 " + (int)idle + " 秒 (" + Math.Round(idle / 60.0, 1) + " 分钟)"
                    + (fs ? "  [前台全屏→跳过]" : "") + flag);
                Emit("      " + fi);
                Thread.Sleep(5000);
            }
            Emit("扫描结束");
        }
    }
}
