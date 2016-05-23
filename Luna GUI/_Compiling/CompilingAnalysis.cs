using System.Collections.Generic;
using System.Diagnostics;
using MahApps.Metro.Controls.Dialogs;

namespace Luna_GUI._Compiling
{
    internal static class CompilingAnalysis
    {
        public enum CodeAnalysisResult
        {
            //Errors,
            Warnings,
            CodeFine
        }
        public class CodeAnalysisInfo
        {
            public CodeAnalysisResult Result { get; set; }
            public List<string> Announcements = new List<string>();
        }
        /// <summary>
        /// using hopefully extracted luna
        /// </summary>
        /// <returns></returns>
        public static string CompileLuaFile()
        {
            if (!MyResourceManager.loadedResources)
            {
                WindowManager.MainWindow.ShowMessageAsync("Warnung",
                    "Es wird auf das Laden aller Resourcen gewartet...Bitte warte kurz");
                while (!MyResourceManager.loadedResources) { /*wait*/}
            }

            string explorerPathToLuaFile = Testing.luapath.Substring(0, Testing.luapath.LastIndexOf("\\"));
            string tnsOutputPath = explorerPathToLuaFile + "\\" + Testing.luafilenameNoExtension + ".tns";


            Process process = new Process
            {
                StartInfo =
                {
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    FileName = Testing.lunaPath,
                    Arguments = "\"" + Testing.luapath + "\"" + " \"" + tnsOutputPath + "\""
                }
            };
            process.Start();

            return tnsOutputPath;
        }

        public static CodeAnalysisInfo RunCodeAnalysis()
        {
            //TODO: Implement
            return new CodeAnalysisInfo {Result = CodeAnalysisResult.CodeFine};
        }
    }
}
