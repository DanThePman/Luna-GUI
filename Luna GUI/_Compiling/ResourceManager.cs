using System;
using System.IO;
using System.Linq.Expressions;
using System.Threading;
using Shell32;

namespace Luna_GUI._Compiling
{
    internal static class MyResourceManager
    {
        private static int resourceLoadCount_SinceAppStart;

        public static bool loadedResources { get; set; }

        public static bool LuaCompilerInstalled => true; /*Nlua*/

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

            var objShell = new Shell();
            var destinationFolder = objShell.NameSpace(folderPath);
            var sourceFile = objShell.NameSpace(zipFile);

            foreach (var file in sourceFile.Items())
            {
                destinationFolder.CopyHere(file, 4 | 16);
            }
        }

        /// <summary>
        /// </summary>
        /// <param name="resource"></param>
        /// <param name="name"></param>
        /// <param name="extension"></param>
        /// <returns></returns>
        public static void ExractExecutableResource(byte[] resource, string name, string extension)
        {
            var t = new Thread(() =>
            {
                var outputPath = Environment.CurrentDirectory + "\\" + name + extension;

                if (!File.Exists(outputPath))
                {
                    File.WriteAllBytes(outputPath, resource);

                    var zipfile = extension == ".zip";
                    if (zipfile)
                    {
                        UnZip(outputPath, Environment.CurrentDirectory + "\\" + name);
                    }
                }

                resourceLoadCount_SinceAppStart++;

                if (resourceLoadCount_SinceAppStart == 6)
                    loadedResources = true;
            });
            t.SetApartmentState(ApartmentState.STA);
            t.Start();
        }
    }
}