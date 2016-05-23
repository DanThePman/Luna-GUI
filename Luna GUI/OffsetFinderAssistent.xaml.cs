using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using MahApps.Metro.Controls;

namespace Luna_GUI
{
    /// <summary>
    ///     Interaction logic for OffsetFinderAssistent.xaml
    /// </summary>
    public partial class OffsetFinderAssistent
    {
        private readonly string configpath = Environment.CurrentDirectory + "\\LunaUI_positionOffets.config";
        public MetroWindow mainWindow = WindowManager.MainWindow;
        public CallOnFinishH finishCall = null;
        public string luaExplorerPath = null;

        public delegate void CallOnFinishH();

        private readonly List<string> controlsToSet = new List<string>
        {
            "reload_button",
            "screen_mid",
            "dataTab",
            "LuaFilePosInExplorer",
            "pixelsToMove",
            "settingsTab",
            "keyboardTab",
            "menuKey",
            "onKey",
            "docKey",
            "dataTransfereConfiguration",
            "dataTranferePathTextBox",
            "firebirdTrayIconPos"
        };

        public OffsetFinderAssistent()
        {
            InitializeComponent();
        }

        string GetInfo(string item)
        {
            if (item == "reload_button")
                return "Refresh-Button";
            if (item == "screen_mid")
                return "Emulator-Display";
            if (item == "dataTab")
                return "Dateiübertragungs-Tab";
            if (item == "LuaFilePosInExplorer")
                return "Lua-Datei-Position";

            if (item == "pixelsToMove")
                return "Skip";

            if (item == "settingsTab")
                return "Einstellungs-Tab";
            if (item == "keyboardTab")
                return "Tastatur-Tab";

            if (item == "menuKey")
                return "menu-Taste";
            if (item == "onKey")
                return "on-Taste";
            if (item == "docKey")
                return "doc-Taste";

            if (item == "dataTransfereConfiguration")
                return "Einstellungen->Dateiübtragungs-Feld";
            if (item == "dataTranferePathTextBox")
                return "Zielordner-Textfeld";
            if (item == "firebirdTrayIconPos")
                return "Taskleistensymbol";

            return null;
        }

        private void SetConfig(string item, string xFac, string yFac)
        {
            var newLine = item + ":" + xFac + ";" + yFac;
            var currentConfigLines = new List<string>();
            using (var sr = new StreamReader(configpath))
            {
                while (!sr.EndOfStream)
                {
                    var line = sr.ReadLine();
                    currentConfigLines.Add(line);
                }
                sr.Close();
            }
            var index = currentConfigLines.FindIndex(x => x.Contains(item));
            currentConfigLines[index] = newLine;

            using (var sw = new StreamWriter(configpath))
            {
                foreach (var line in currentConfigLines)
                {
                    if (line == currentConfigLines.Last())
                        sw.Write(line);
                    else
                        sw.WriteLine(line);
                }
                sw.Close();
            }
        }

        private void MetroWindow_Loaded(object sender, RoutedEventArgs e)
        {
            string title = this.Title;
            string mainWinTitle = mainWindow.Title;

            var w = SystemParameters.PrimaryScreenWidth;
            var h = SystemParameters.PrimaryScreenHeight;

            var startThread = new Thread(() =>
            {
                Thread.Sleep(3000);
                foreach (var controlToSet in controlsToSet)
                {
                    /*leave 17 pixels in file as default*/
                    if (controlToSet == "pixelsToMove")
                    {
                        continue;
                    }

                    WindowManager.BringWindowToFront(title);

                    Dispatcher.Invoke(new Action(() =>
                    {
                        textInfo.Text = GetInfo(controlToSet);
                    }));

                    /*set countdown*/
                    var oldTick = Environment.TickCount;
                    while (Environment.TickCount - oldTick <= 5000)
                    {
                        var timeLeft = (5000 - (Environment.TickCount - oldTick)) / 1000;
                        Dispatcher.Invoke(new Action(() => countDownText.Text = timeLeft.ToString()));
                    }

                    if (controlToSet != "LuaFilePosInExplorer")
                        WindowManager.ActivateAppMaximised("TI-Nspire Emulator");
                    else /*bring explorer to front*/
                    {
                        WindowManager.BringLuaFileExplorerToFront(Testing.luapath);
                        Thread.Sleep(1000);
                    }

                    while (true)
                    {
                        if (Keyboard.IsKeyDown(Key.F9))
                            break;
                    }

                    var x = System.Windows.Forms.Cursor.Position.X / w;
                    var y = System.Windows.Forms.Cursor.Position.Y / h;

                    var xfac = x.ToString().Replace(".", ",");
                    var yfac = y.ToString().Replace(".", ",");

                    SetConfig(controlToSet, xfac, yfac);
                }

                WindowManager.BringWindowToFront(mainWinTitle);

                Dispatcher.Invoke(new Action(() =>
                {
                    OffsetReader.FillOffsetList();
                    finishCall(); /*green position set*/
                    this.Close();
                }));
            });
            startThread.SetApartmentState(ApartmentState.STA);
            startThread.Start();
        }
    }
}