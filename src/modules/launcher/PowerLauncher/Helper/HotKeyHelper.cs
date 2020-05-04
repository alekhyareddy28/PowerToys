using System;
using System.Runtime.InteropServices;

namespace PowerLauncher.Helper
{
    // This class helps create Keyboard hooks for PowerToys run
    public class HotKeyHelper : IDisposable
    {
        #region locals
        #endregion


        // Contains the structure of the keyboard hook
        // https://docs.microsoft.com/en-us/previous-versions/windows/desktop/legacy/ms644985(v=vs.85)
        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

        #region extern
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);
        #endregion

        private void start_lowlevel_keyboard_hook()
        {

        }


        // Function to setup the keyboard hook
        public HotKeyHelper()
        {
            start_lowlevel_keyboard_hook();
        }

        // Function to dispose the keyboard hook
        ~HotKeyHelper()
        {
            Dispose();
        }

        public void stop_lowlevel_keyboard_hook()
        {

        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }
    }
}
