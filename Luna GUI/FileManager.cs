using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;

namespace Luna_GUI
{
    internal static class FileManager
    {
        public class CodeSnippetComparison
        {
            public readonly Dictionary<string, string> _codeChanges;

            public CodeSnippetComparison()
            {
            }

            /// <summary>
            /// called if code has been changed
            /// </summary>
            /// <param name="codeChanges"></param>
            public CodeSnippetComparison(Dictionary<string, string> codeChanges)
            {
                _codeChanges = codeChanges;
            }

            public string snippetName { get; set; }
            /// <summary>
            /// doesnt exist online anymore
            /// </summary>
            public bool gotRemoved { get; set; }
            public bool isMissing { get; set; }

            public bool codeChanged => _codeChanges != null;
            public bool uptodate { get; set; }
        }

        public static readonly string _extensionPath = Environment.CurrentDirectory + @"\Data\Packages";
        private static readonly string projectPath = "https://github.com/DanThePman/LunaCodeSnippets";

        static List<string> GetCurrentCodeSnippets()
        {
            DirectoryInfo dirInfo = new DirectoryInfo(_extensionPath);

            return dirInfo.GetFiles("*.sublime-snippet").
                Select(snippet => snippet.Name).ToList();
        }

        static List<string> GetOnlineCodeSnippets()
        {
            string data = null;

            string urlAddress = projectPath;

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(urlAddress);
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();

            if (response.StatusCode == HttpStatusCode.OK)
            {
                Stream receiveStream = response.GetResponseStream();
                StreamReader readStream = null;

                readStream =
                    response.CharacterSet == null ? new StreamReader(receiveStream) :
                        new StreamReader(receiveStream, Encoding.GetEncoding(response.CharacterSet));

                data = readStream.ReadToEnd();

                response.Close();
                readStream.Close();
            }

            List<string> fileNames = new List<string>();
            using (StringReader reader = new StringReader(data))
            {
                while (true)
                {
                    string line = reader.ReadLine();
                    if (line != null)
                    {
                        string s = ".sublime-snippet\">";
                        if (line.Contains(s))
                        {
                            int spos = line.IndexOf(s) + s.Length;
                            int epos = line.IndexOf("</a>");
                            fileNames.Add(line.Substring(spos, epos - spos));
                        }
                    }
                    else
                        break;
                }
            }

            return fileNames;
        }


        public static string GetOnlineSnippetContent(string snippetName)
        {
            string content = null;

            string urlAddress = "https://raw.githubusercontent.com/DanThePman/LunaCodeSnippets/master/" + snippetName;

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(urlAddress);
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();

            if (response.StatusCode == HttpStatusCode.OK)
            {
                Stream receiveStream = response.GetResponseStream();
                StreamReader readStream = null;

                readStream =
                    response.CharacterSet == null ? new StreamReader(receiveStream) :
                        new StreamReader(receiveStream, Encoding.GetEncoding(response.CharacterSet));

                content = readStream.ReadToEnd();

                response.Close();
                readStream.Close();
            }

            return content;
        }

        public static string GetLocalSnippetContent(string name)
        {
            StreamReader sr = new StreamReader(_extensionPath + "\\" + name);
            string content = sr.ReadToEnd();
            sr.Close();

            return content;
        }

        /// <summary>
        /// returns line numbers of code changes OR null if code is equal
        /// </summary>
        /// <param name="snippetName"></param>
        /// <returns></returns>
        static Dictionary<string, string> CompareCodeToOnlineSnippet(string snippetName)
        {
            string localSnippetContent = File.ReadAllText(_extensionPath + "\\" + snippetName);
            string onlineSnippetContent = GetOnlineSnippetContent(snippetName);

            Dictionary<string, string> codeChangeLines = new Dictionary<string, string>();

            using (StringReader reader = new StringReader(localSnippetContent))
            {
                int localLineIndex = 1;
                while (true)
                {
                    string line = reader.ReadLine();
                    if (line != null)
                    {
                        StringReader onlineReader = new StringReader(onlineSnippetContent);
                        for (int onlineLineIndex = 1; onlineLineIndex < localLineIndex; onlineLineIndex++)
                            onlineReader.ReadLine();

                        string sameLineInOnlineFile = onlineReader.ReadLine();
                        if (sameLineInOnlineFile == line)
                        {
                            //no code changes
                        }
                        else
                        {
                            //code changes
                            codeChangeLines.Add(line, sameLineInOnlineFile);
                        }

                        localLineIndex++;
                    }
                    else
                        break;
                }
            }

            return codeChangeLines.Count > 0 ? codeChangeLines : null;
        }

        public static List<CodeSnippetComparison> CheckCodeSnippets()
        {
            List<CodeSnippetComparison> codeSnippets = new List<CodeSnippetComparison>();

            var onlineCodeSnippets = GetOnlineCodeSnippets();
            var localCodeSnippets = GetCurrentCodeSnippets();

            foreach (string currentCodeSnippet in localCodeSnippets)
            {
                CodeSnippetComparison comparison = null;
                string onlineSnippet = onlineCodeSnippets.FirstOrDefault(x => x == currentCodeSnippet);


                if (onlineSnippet == null)
                    comparison = new CodeSnippetComparison { snippetName = currentCodeSnippet, gotRemoved = true };
                else
                {
                    Dictionary<string, string> codeComparision = CompareCodeToOnlineSnippet(currentCodeSnippet);

                    if (codeComparision != null) //not same code
                    {
                        comparison = new CodeSnippetComparison(codeComparision) { snippetName = currentCodeSnippet };
                    }
                    else //updated
                    {
                        comparison = new CodeSnippetComparison() { snippetName = currentCodeSnippet, uptodate = true};
                    }
                }

                codeSnippets.Add(comparison);
            }

            foreach (var missingOnlineSnippet in onlineCodeSnippets.Where(x => !localCodeSnippets.Contains(x)))
            {
                codeSnippets.Add(new CodeSnippetComparison { snippetName = missingOnlineSnippet, isMissing = true });
            }

            return codeSnippets;
        } 
    }
}
