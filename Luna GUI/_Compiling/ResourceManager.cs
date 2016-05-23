using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq.Expressions;
using System.Threading;

namespace Luna_GUI._Compiling
{
    internal static class MyResourceManager
    {
        public static string GetNameOf<T>(Expression<Func<T>> property)
        {
            // ReSharper disable once PossibleNullReferenceException
            return (property.Body as MemberExpression).Member.Name;
        }

        private static void UnZip(string zipFile, string folderPath)
        {
            if (!File.Exists(zipFile))
                throw new FileNotFoundException();

            if (!Directory.Exists(folderPath))
                Directory.CreateDirectory(folderPath);

            Shell32.Shell objShell = new Shell32.Shell();
            Shell32.Folder destinationFolder = objShell.NameSpace(folderPath);
            Shell32.Folder sourceFile = objShell.NameSpace(zipFile);

            foreach (var file in sourceFile.Items())
            {
                destinationFolder.CopyHere(file, 4 | 16);
            }
        }

        public delegate void OnRemoveResourceCacheH();
        static readonly List<OnRemoveResourceCacheH> resourceRemoveFuncs = new List<OnRemoveResourceCacheH>();

        public static bool loadedResources { get; set; } = false;
        private static int resourceLoadCount_SinceAppStart = 0;
        /// <summary>
        /// 
        /// </summary>
        /// <param name="resource"></param>
        /// <param name="name"></param>
        /// <param name="extension"></param>
        /// <returns></returns>
        public static void ExractExecutableResource(byte[] resource, string name, string extension)
        {
            Thread t = new Thread(() =>
            {
                string outputPath = Environment.CurrentDirectory + "\\" + name + extension;

                File.WriteAllBytes(outputPath, resource);

                bool zipfile = extension == ".zip";
                if (zipfile)
                {
                    UnZip(outputPath, Environment.CurrentDirectory + "\\" + name);
                }

                OnRemoveResourceCacheH removeMethod = () =>
                {
                    File.Delete(outputPath);
                    if (zipfile)
                        Directory.Delete(Environment.CurrentDirectory + "\\" + name, true);
                };

                resourceRemoveFuncs.Add(removeMethod);
                resourceLoadCount_SinceAppStart++;

                if (resourceLoadCount_SinceAppStart == 3) //luna libs
                    loadedResources = true;
            });
            t.SetApartmentState(ApartmentState.STA);
            t.Start();
        }

        public static void ClearResourceFiles()
        {
            foreach (OnRemoveResourceCacheH removeFunc in resourceRemoveFuncs)
            {
                removeFunc();
            }
        }

        public static void CheckIfLuaIsInstalled()
        {
            string x86_programFilePath = Environment.Is64BitOperatingSystem
                ? Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86)
                : Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);

            if (!Directory.Exists(x86_programFilePath + "\\Lua"))
            {
                ExractExecutableResource(Properties.Resources.LuaForWindows_v5_1_4_46,
                    GetNameOf(() => Properties.Resources.LuaForWindows_v5_1_4_46), ".exe");
                string setupPath = Environment.CurrentDirectory + "\\" + GetNameOf(() 
                    => Properties.Resources.LuaForWindows_v5_1_4_46) + ".exe";
                Process.Start(setupPath);
            }
        }
    }
}
