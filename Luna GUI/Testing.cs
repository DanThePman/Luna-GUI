﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using WindowsInput;
using WindowsInput.Native;
using Luna_GUI._Compiling;
using MahApps.Metro.Controls.Dialogs;
using SHDocVw;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;

namespace Luna_GUI
{
    internal static class Testing
    {
        public static bool DebugMode => false;
            //WindowManager.MainWindow.GetType().Assembly.GetCustomAttributes(false).
            //    OfType<DebuggableAttribute>().Select(da => da.IsJITTrackingEnabled).FirstOrDefault() ||
            //File.Exists(Environment.CurrentDirectory + "\\LunaGUI.debug");

        public static string lunaPath = Environment.CurrentDirectory + "\\" + "luna.exe";
        public static string luapath { get; set; }
        public static string explorerPathToLuaFile { get; set; }
        public static string luafilenameNoExtension { get; set; }

        private static int runCount = 0;
        static bool firstRun => runCount == 1;

        /// <summary>
        /// returns selected file path
        /// </summary>
        /// <returns></returns>
        public static string OnSelectLuaFile()
        {
            try
            {
                OpenFileDialog dialog = new OpenFileDialog
                {
                    Multiselect = false,
                    AddExtension = true,
                    Filter = "Lua Code (.lua)|*.lua"
                };
                bool? result = dialog.ShowDialog();

                if (!result.HasValue)
                {
                    WindowManager.MainWindow.ShowMessageAsync("Fehler",
                        "Es gab einen Fehler bei der Auswahl der Luadatei..");
                    return string.Empty;
                }

                int s = dialog.FileName.LastIndexOf("\\") + 1;
                int e = dialog.FileName.Length;

                string luafileNameWithExtension = dialog.FileName.Substring(s, e - s);
                int e2 = luafileNameWithExtension.IndexOf(".");

                luapath = dialog.FileName;
                luafilenameNoExtension = luafileNameWithExtension.Substring(0, e2);

                #region setup explorer for drag and dop

                Thread t = new Thread(() =>
                {
                    CloseExplorerWindows();

                    Thread.Sleep(!WindowManager.IsWindowsXP ? 1000 : 3000);

                    explorerPathToLuaFile = luapath.Substring(0, luapath.LastIndexOf("\\"));
                    Process.Start("explorer.exe", explorerPathToLuaFile);
                });
                if (!DebugMode)
                    t.Start();
                WindowManager.BringWindowToFront(WindowManager.MainWindow.Title);

                #endregion setup explorer for drag and dop

                return luafileNameWithExtension;
            }
            catch
            {
                WindowManager.MainWindow.ShowMessageAsync("Fehler",
                        "Es gab einen Fehler bei der Auswahl der Luadatei..");
                return string.Empty;
            }
        }


        public static bool SetUped { get; set; } = false;

        /// <summary>
        /// sets up explorer-window for the lua file | clears ti explorer
         /// </summary>
        static void SetUp()
        {
            Thread t = new Thread(() =>
            {
                #region removeCxCasTrash

                WindowManager.ActivateAppMaximised("TI-Nspire Emulator");
                Thread.Sleep(!WindowManager.IsWindowsXP ? 500 : 1500);

                var cxCasScreenPoint = OffsetReader.GetSpecialClickPoint(OffsetReader.SpecialClickPoint.screen_mid);
                WindowManager.MouseClick(cxCasScreenPoint.Item1, cxCasScreenPoint.Item2);

                Thread.Sleep(!WindowManager.IsWindowsXP ? 100 : 500);
                WindowManager.TiKeyboardActionPress(OffsetReader.SpecialClickPoint.onKey);
                Thread.Sleep(!WindowManager.IsWindowsXP ? 100 : 500);
                WindowManager.TiKeyPress(VirtualKeyCode.VK_2);

                /*down -> delete file .. down...*/
                RemoveTiTrashFiles();
                #endregion removeCxCasTrash

                #region Create __Lua folder
                Thread.Sleep(!WindowManager.IsWindowsXP ? 100 : 500);
                WindowManager.TiKeyboardActionPress(OffsetReader.SpecialClickPoint.menuKey);
                Thread.Sleep(!WindowManager.IsWindowsXP ? 100 : 500);
                WindowManager.TiKeyPress(VirtualKeyCode.VK_1);
                Thread.Sleep(!WindowManager.IsWindowsXP ? 100 : 500);

                IEnumerable<VirtualKeyCode> tiLuaFolderName = new List<VirtualKeyCode>
                {
                    (VirtualKeyCode)0x00BD, //-
                    (VirtualKeyCode)0x00BD,
                    VirtualKeyCode.VK_L,
                    VirtualKeyCode.VK_U,
                    VirtualKeyCode.VK_A
                };

                WindowManager.TiTextInput(tiLuaFolderName);

                Thread.Sleep(!WindowManager.IsWindowsXP ? 100 : 500);
                WindowManager.TiKeyPress(VirtualKeyCode.RETURN);
                Thread.Sleep(!WindowManager.IsWindowsXP ? 100 : 500);
                #endregion Create __Lua folder

                #region ConfigureTranferePath in Emulator
                var settingsPos = OffsetReader.GetSpecialClickPoint(OffsetReader.SpecialClickPoint.settingsTab);
                WindowManager.TiMouseClick(settingsPos.Item1, settingsPos.Item2);
                Thread.Sleep(!WindowManager.IsWindowsXP ? 100 : 500);

                var dataTransfereButtonTab = OffsetReader.GetSpecialClickPoint(OffsetReader.SpecialClickPoint.dataTransfereConfiguration);
                WindowManager.TiMouseClick(dataTransfereButtonTab.Item1, dataTransfereButtonTab.Item2);
                Thread.Sleep(!WindowManager.IsWindowsXP ? 100 : 500);

                var pathTextBoxPos = OffsetReader.GetSpecialClickPoint(OffsetReader.SpecialClickPoint.dataTranferePathTextBox);
                WindowManager.TiMouseClick(pathTextBoxPos.Item1, pathTextBoxPos.Item2);
                Thread.Sleep(!WindowManager.IsWindowsXP ? 100 : 500);

                /*remove old tranfere path*/
                for (int i = 0; i < 15; i++)
                {
                    new InputSimulator().Keyboard.KeyPress(VirtualKeyCode.BACK);
                    Thread.Sleep(!WindowManager.IsWindowsXP ? 50 : 100);
                }

                new InputSimulator().Keyboard.TextEntry("/--lua");
                #endregion

                SetUped = true;
            });
            t.Start();
        }

        private static void RemoveTiTrashFiles()
        {
            for (int i = 0; i < 5; i++)
            {
                WindowManager.TiKeyPress(VirtualKeyCode.DOWN);
                Thread.Sleep(!WindowManager.IsWindowsXP ? 50 : 100);
                WindowManager.TiKeyPress(VirtualKeyCode.BACK);
                Thread.Sleep(!WindowManager.IsWindowsXP ? 50 : 100);
                WindowManager.TiKeyPress(VirtualKeyCode.RETURN);
                Thread.Sleep(!WindowManager.IsWindowsXP ? 50 : 100);
            }
        }

        private static void CloseExplorerWindows()
        {
            ShellWindows _shellWindows = new ShellWindows();

            foreach (InternetExplorer ie in _shellWindows)
            {
                // ReSharper disable once PossibleNullReferenceException
                var processType = Path.GetFileNameWithoutExtension(ie.FullName).ToLower();

                if (processType.Equals("explorer"))
                {
                    ie.Quit();
                }
            }
        }

        public static void OnCompile(bool? _overclock)
        {
            runCount++;
            bool overclock = _overclock.HasValue && _overclock.Value;

            if (firstRun)
                SetUp();

            Thread clickThread = new Thread(() =>
            {
                while (!SetUped) { /*wait*/}

                string tnsOutputPath = null;
                var codeAnalysis = CompilingAnalysis.RunCodeAnalysis();
                if (codeAnalysis.Result == CompilingAnalysis.CodeAnalysisResult.CodeFine)
                {
                    tnsOutputPath = CompilingAnalysis.CompileLuaFile(codeAnalysis.GetLuaLinesFile());
                    codeAnalysis.RemoveLunaFile();
                }
                else
                {
                    string compilingWarning = string.Join("\n", codeAnalysis.Announcements.ToArray());
                    WindowManager.SetForegroundWindow(WindowManager.MainWindow.Title);
                    var messageResult = WindowManager.MainWindow.ShowMessageAsync("Warnung", "Es wurden folgende Code-Warnungen endeckt:\n" +
                        compilingWarning + "\n\nTrotzdem kompilieren?", MessageDialogStyle.AffirmativeAndNegative).Result;

                    if (messageResult == MessageDialogResult.Affirmative)
                    {
                        tnsOutputPath = CompilingAnalysis.CompileLuaFile(codeAnalysis.GetLuaLinesFile());
                        codeAnalysis.RemoveLunaFile();
                    }
                    else return;
                }

                /*reload button*/
                WindowManager.ActivateAppMaximised("TI-Nspire Emulator");
                Thread.Sleep(!WindowManager.IsWindowsXP ? 1000 : 3000);
                var dataTabPoint = OffsetReader.GetSpecialClickPoint(OffsetReader.SpecialClickPoint.dataTab);
                var reloadPoint = OffsetReader.GetSpecialClickPoint(OffsetReader.SpecialClickPoint.reload_button);
                WindowManager.TiMouseClick(dataTabPoint.Item1, dataTabPoint.Item2);
                Thread.Sleep(!WindowManager.IsWindowsXP ? 100 : 500);
                WindowManager.TiMouseClick(reloadPoint.Item1, reloadPoint.Item2, 500);
                Thread.Sleep(!WindowManager.IsWindowsXP ? 1000 : 3000);
                WindowManager.TiMouseClick(reloadPoint.Item1, reloadPoint.Item2, 500);
                /*reload button*/
                Thread.Sleep(!WindowManager.IsWindowsXP ? 100 : 500);

                DragNDrop.DragFileNDropTo_CX_CAS(tnsOutputPath, luafilenameNoExtension);
            });
            clickThread.Start();
        }
    }
}
