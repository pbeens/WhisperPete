using System;
using System.IO;
using System.Windows;
using System.Drawing;
using System.Windows.Forms;
using Application = System.Windows.Application;
using WhisperPete.Core;

namespace WhisperPete.Tray
{
    public partial class App : Application
    {
        private NotifyIcon? _notifyIcon;

        protected override void OnStartup(StartupEventArgs e)
        {
            try 
            {
                DispatcherUnhandledException += (s, args) => 
                {
                    Log($"FATAL UI ERROR: {args.Exception.Message}\n{args.Exception.StackTrace}");
                    System.Windows.MessageBox.Show("A fatal error occurred. Please check app_log.txt.");
                    args.Handled = true;
                };

                AppDomain.CurrentDomain.UnhandledException += (s, args) =>
                {
                    Log($"FATAL DOMAIN ERROR: {(args.ExceptionObject as Exception)?.Message}");
                };

                base.OnStartup(e);
                Log("Application starting...");
                
                _notifyIcon = new NotifyIcon();
                _notifyIcon.Text = "WhisperPete";
                
                try 
                {
                    Icon? loadedIcon = null;
                    var iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "app_icon.ico");
                    var pngPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "app_icon.png");

                    // 1. Try Loading ICO directly
                    if (File.Exists(iconPath))
                    {
                        try { loadedIcon = new Icon(iconPath); } catch { }
                    }

                    // 2. Try Loading PNG and converting to Icon
                    if (loadedIcon == null && File.Exists(pngPath))
                    {
                        try 
                        {
                            using (var bmp = new Bitmap(pngPath))
                            {
                                loadedIcon = Icon.FromHandle(bmp.GetHicon());
                            }
                        }
                        catch { }
                    }

                    // 3. Last resort fallback
                    _notifyIcon.Icon = loadedIcon ?? SystemIcons.Information;
                }
                catch (Exception ex)
                {
                    Log($"Global icon error: {ex.Message}. Falling back to system default.");
                    _notifyIcon.Icon = SystemIcons.Information;
                }
                
                _notifyIcon.Visible = true;
                
                var contextMenu = new ContextMenuStrip();
                contextMenu.Items.Add("Settings", null, Settings_Click);
                contextMenu.Items.Add("-");
                contextMenu.Items.Add("Exit", null, Exit_Click);
                
                _notifyIcon.ContextMenuStrip = contextMenu;
                Log("Tray icon initialized.");

                // Force creation of MainWindow and its handle to trigger SourceInitialized (hotkey/settings)
                var mainWin = new MainWindow();
                new System.Windows.Interop.WindowInteropHelper(mainWin).EnsureHandle();
                this.MainWindow = mainWin;
                
                Log("Global hotkey registered.");
            }
            catch (Exception ex)
            {
                Log($"Crash in OnStartup: {ex}");
                System.Windows.MessageBox.Show($"Startup Error: {ex.Message}");
                Shutdown();
            }
        }

        public void Log(string message)
        {
            WhisperPete.Core.Logger.Log(message);
        }

        private void Settings_Click(object? sender, EventArgs e)
        {
            if (MainWindow != null)
            {
                MainWindow.Show();
                MainWindow.WindowState = WindowState.Normal;
                MainWindow.Activate();
            }
        }

        private void Exit_Click(object? sender, EventArgs e)
        {
            Shutdown();
        }

        public void ShowNotification(string title, string text, int duration = 1000)
        {
            if (_notifyIcon != null)
            {
                _notifyIcon.ShowBalloonTip(duration, title, text, ToolTipIcon.Info);
            }
        }

        protected override void OnExit(ExitEventArgs e)
        {
            _notifyIcon?.Dispose();
            base.OnExit(e);
        }
    }
}
