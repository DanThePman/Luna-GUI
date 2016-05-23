using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Luna_GUI
{
    internal static class OffsetReader
    {
        private static readonly string configpath = Environment.CurrentDirectory + "\\LunaUI_positionOffets.config";
        public static void FillOffsetList()
        {
            using (StreamReader sr = new StreamReader(configpath))
            {
                while (!sr.EndOfStream)
                {
                    string line = sr.ReadLine();

                    if (line.Contains("pixelsToMove"))
                        //pixels to move downwards in explorer
                    {
                        double value = Convert.ToDouble(line.Substring(line.IndexOf(":") + 1, line.Length - (line.IndexOf(":") + 1)));
                        DragNDrop.pixelsPerItem = value;
                    }
                    else
                    {
                        string offsetname = line.Substring(0, line.IndexOf(":"));
                        double xFactor = Convert.ToDouble(line.Substring(line.IndexOf(":") + 1, line.IndexOf(";") - (line.IndexOf(":") + 1)));
                        double yFactor = Convert.ToDouble(line.Substring(line.IndexOf(";") + 1, line.Length - (line.IndexOf(";") + 1)));

                        OffsetInfoList.Add(new WindowManager.OffsetInfo
                        {
                            Name = offsetname, xFactor = xFactor, yFactor = yFactor
                        });
                    }     

                }
                sr.Close();
            }
        }

        public static List<WindowManager.OffsetInfo> OffsetInfoList = new List<WindowManager.OffsetInfo>();

        public enum SpecialClickPoint
        {
            reload_button,
            screen_mid,
            dataTab,
            LuaFilePosInExplorer,

            settingsTab,
            keyboardTab,

            menuKey,
            onKey,
            docKey,

            dataTransfereConfiguration,
            dataTranferePathTextBox,

            firebirdTrayIconPos
        }

        public static Tuple<double, double> GetSpecialClickPoint(SpecialClickPoint point)
        {
            double w = System.Windows.SystemParameters.PrimaryScreenWidth;
            double h = System.Windows.SystemParameters.PrimaryScreenHeight;

            double offsetx = OffsetInfoList.First(x => x.Name == point.ToString("G")).xFactor;
            double offsety = OffsetInfoList.First(x => x.Name == point.ToString("G")).yFactor;
            return new Tuple<double, double>(w * offsetx, h * offsety);
        }
    }
}
