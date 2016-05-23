using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using WindowsInput;
using WindowsInput.Native;
using MahApps.Metro.Controls;
using SHDocVw;

namespace Luna_GUI
{
    internal static class WindowManager
    {
        public static MetroWindow MainWindow;

        private const int SW_SHOWMAXIMIZED = 3;

        private const int leftDown = 0x02;
        private const int leftUp = 0x04;

        private static readonly InputSimulator sim = new InputSimulator();


        [DllImport("user32.dll")]
        public static extern IntPtr FindWindow(string ClassN, string WindN);

        [DllImport("user32.dll")]
        public static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr handle, int nCmdShow);

        public static void ActivateAppMaximised(string captionName)
        {
            // retrieve Notepad main window handle
            var hWnd = FindWindow(null, captionName);
            if (!hWnd.Equals(IntPtr.Zero))
            {
                ShowWindow(hWnd, SW_SHOWMAXIMIZED);
                SetForegroundWindow(hWnd);
            }
        }


        [DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        private static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint cButtons, uint dwExtraInfo);

        public static void MouseClick(double x, double y)
        {
            var inp = new InputSimulator();
            Cursor.Position = new Point((int) x, (int) y);
            inp.Mouse.LeftButtonClick();
        }

        public static void TiMouseClick(double x, double y, int extraTime = 0)
        {
            var t = new Thread(() =>
            {
                Cursor.Position = new Point((int) x, (int) y);
                mouse_event(leftDown, (uint) x, (uint) y, 0, 0);
                Thread.Sleep(250 + extraTime);
                mouse_event(leftUp, (uint) x, (uint) y, 0, 0);
            });
            t.Start();
            t.Join();
        }

        /// <summary>
        ///     Slower than a normal Keypress
        /// </summary>
        /// <param name="key"></param>
        public static void TiKeyPress(VirtualKeyCode key)
        {
            sim.Keyboard.KeyDown(key);
            Thread.Sleep(100);
            sim.Keyboard.KeyUp(key);
        }

        /// <summary>
        ///     only if emulator is on front
        /// </summary>
        /// <param name="key"></param>
        public static void TiKeyboardActionPress(OffsetReader.SpecialClickPoint key)
        {
            if (key != OffsetReader.SpecialClickPoint.menuKey && key != OffsetReader.SpecialClickPoint.docKey &&
                key != OffsetReader.SpecialClickPoint.onKey)
                throw new Exception("TiKeyboardActionPress only for menu | doc | on available");

            var keyboardOffset = OffsetReader.GetSpecialClickPoint(OffsetReader.SpecialClickPoint.keyboardTab);
            TiMouseClick(keyboardOffset.Item1, keyboardOffset.Item2);

            Thread.Sleep(250);

            var specialKeyOffset = OffsetReader.GetSpecialClickPoint(key);
            TiMouseClick(specialKeyOffset.Item1, specialKeyOffset.Item2);
        }

        public static void TiTextInput(IEnumerable<VirtualKeyCode> codes)
        {
            var inputThread = new Thread(() =>
            {
                foreach (var keyCode in codes)
                {
                    Thread.Sleep(100);
                    TiKeyPress(keyCode);
                }
            });
            inputThread.Start();
            inputThread.Join();
        }

        public static void BringLuaFileExplorerToFront(string luapath)
        {
            var _shellWindows = new ShellWindows();

            foreach (InternetExplorer ie in _shellWindows)
            {
                // ReSharper disable once PossibleNullReferenceException
                var processType = Path.GetFileNameWithoutExtension(ie.FullName).ToLower();

                var explorerpath = ie.LocationURL.Replace("/", "\\");
                explorerpath = explorerpath.Substring(explorerpath.IndexOf("\\\\") + 3,
                    explorerpath.Length - (explorerpath.IndexOf("\\\\") + 3));

                var explorerPathToLuaFile = luapath.Substring(0, luapath.LastIndexOf("\\"));
                explorerpath = explorerpath.Replace("%20", " ");

                if (processType.Equals("explorer") && explorerpath.Equals(explorerPathToLuaFile))
                {
                    ShowWindow((IntPtr) ie.HWND, SW_SHOWMAXIMIZED);
                    SetForegroundWindow((IntPtr) ie.HWND);
                }
            }
        }

        public static void BringWindowToFront(string captionName)
        {
            var hWnd = FindWindow(null, captionName);
            if (!hWnd.Equals(IntPtr.Zero))
            {
                SetForegroundWindow(hWnd);
            }
        }

        /// <summary>
        /// for windows-explorer
        /// </summary>
        /// <param name="hWnd"></param>
        public static void BringWindowToFront(IntPtr hWnd)
        {
            ShowWindow(hWnd, SW_SHOWMAXIMIZED);
            SetForegroundWindow(hWnd);
        }

        internal class OffsetInfo
        {
            public string Name { get; set; }
            public double xFactor { get; set; }
            public double yFactor { get; set; }
        }
    }
}