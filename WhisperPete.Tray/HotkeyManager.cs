using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace WhisperPete.Tray
{
    public class HotkeyManager : IDisposable
    {
        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        private const int HOTKEY_ID = 9000;
        private IntPtr _hWnd;
        private HwndSource? _source;

        public event Action? HotkeyPressed;

        public void Register(Window window, uint modifiers, uint key)
        {
            var helper = new WindowInteropHelper(window);
            _hWnd = helper.EnsureHandle();
            _source = HwndSource.FromHwnd(_hWnd);
            _source.AddHook(HwndHook);

            if (!RegisterHotKey(_hWnd, HOTKEY_ID, modifiers, key))
            {
                // Log but don't crash
                System.Diagnostics.Debug.WriteLine("Failed to register global hotkey.");
            }
        }

        private IntPtr HwndHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            const int WM_HOTKEY = 0x0312;
            if (msg == WM_HOTKEY && wParam.ToInt32() == HOTKEY_ID)
            {
                HotkeyPressed?.Invoke();
                handled = true;
            }
            return IntPtr.Zero;
        }

        public void Dispose()
        {
            _source?.RemoveHook(HwndHook);
            UnregisterHotKey(_hWnd, HOTKEY_ID);
        }
    }
}
