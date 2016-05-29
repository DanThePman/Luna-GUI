using System;
using System.Collections.Generic;
using System.Linq;

namespace Luna_GUI._Compiling
{
    internal static class LiveDebugging
    {
        private static readonly Random random = new Random();
        private static readonly object syncLock = new object();
        public static int RandomNumber(int max)
        {
            lock (syncLock)
            { // synchronize
                return random.Next(max);
            }
        }

        public static int functionLineIndex { get; set; }

        public static void RemoveAttribute(ref List<string> lines)
        {
            lines.RemoveAt(functionLineIndex - 1);
            functionLineIndex--;
        }

        public static string GetFuncName(List<string> lines)
        {
            int s = lines[functionLineIndex].IndexOf("function ") + 9;
            int e = lines[functionLineIndex].IndexOf("(");
            string funcName = lines[functionLineIndex].Substring(s, e - s);
            return funcName;
        }

        public static string GetFuncArgs(List<string> lines)
        {
            int s3 = lines[functionLineIndex].IndexOf("(") + 1;
            int e3 = lines[functionLineIndex].IndexOf(")");
            string funcArgs = lines[functionLineIndex].Substring(s3, e3 - s3).Replace(" ", "");
            return funcArgs;
        }

        public static string GetLocalCoroutineVariable(List<string> lines, out string randFuncName)
        {
            string funcAsTempFuncName = lines[functionLineIndex].Replace(GetFuncName(lines), "");
            randFuncName = GetFuncName(lines) + "_liveDebug" + RandomNumber(int.MaxValue);
            string ThreadFuncVar = $"local {randFuncName} = coroutine.create({funcAsTempFuncName}";
            return ThreadFuncVar;
        }

        public static List<string> GetOriginalFuncCode(ref List<string> lines, int funcEndIndex)
        {
            int removeIndex = functionLineIndex + 1;
            List<string> functionCodeLines = new List<string>();
            for (int i = functionLineIndex + 1; i < funcEndIndex; i++)
            {
                if (lines[removeIndex].Contains("return "))
                {
                    return new List<string> { "[Error] LiveDebug-Function is not allowed to return a value" };
                }
                else
                    functionCodeLines.Add(lines[removeIndex]);
                lines.RemoveAt(removeIndex);
            }
            functionCodeLines.Reverse();

            return functionCodeLines;
        }

        public static void CreateLocalCoroutine(ref List<string> lines, string ThreadFuncVar, string randFuncName,
            List<string> functionCodeLines)
        {
            List<string> localCorLines = new List<string>
            {
                $"local __liveDebug_currentCodePosition_{randFuncName} = \"No calls of LiveDebug-Function\"",
                $"local __liveDebug_currentStep_{randFuncName} = 0",
                $"local __liveDebug_enterPressed_{randFuncName} = false",
                ThreadFuncVar
            };
            int beginCount = localCorLines.Count;


            functionCodeLines.Reverse();
            for (int i = 0; i < functionCodeLines.Count; i++)
            {
                var localCoroutineCreateLine = functionCodeLines[i];
                if (!localCoroutineCreateLine.Equals("platform.window:invalidate()"))
                {
                    if (i > 0)//dont yield before 1st code line
                        localCorLines.Add("coroutine.yield()");

                    localCorLines.Add($"__liveDebug_currentCodePosition_{randFuncName} = \"{localCoroutineCreateLine}\"");
                    localCorLines.Add($"__liveDebug_currentStep_{randFuncName} = __liveDebug_currentStep_{randFuncName} + " +
                                      $"platform.window:width()/{functionCodeLines.Count(x => !x.Equals("platform.window:invalidate()"))}");
                    localCorLines.Add("platform.window:invalidate()");

                    if (i < functionCodeLines.Count - 1)
                        localCorLines.Add("[Debug]");
                }

                /*main line*/
                localCorLines.Add(localCoroutineCreateLine);
                
            }
            localCorLines.Add("end)");
            localCorLines.Reverse();

            foreach (string localCorLine in localCorLines)
            {
                lines.Insert(functionLineIndex, localCorLine);
            }

            functionLineIndex += beginCount + 1 /*end)*/ + functionCodeLines.Count(x => !x.Equals("platform.window:invalidate()")) * 6
                    + functionCodeLines.Count(x => x.Equals("platform.window:invalidate()")) - 1;
        }

        public static void CreateLocalResumeFunction(ref List<string> lines, string ThreadFuncVar, string randFuncName,
            List<string> functionCodeLines, string funcArgs)
        {
            List<string> ResumeFunc = new List<string>
            {
                $"function ResumeFunc_{randFuncName}({funcArgs})",
                $"if coroutine.status({randFuncName}) == \"dead\" then",
                ThreadFuncVar.Replace("local ", ""),
            };
            /*re-define coroutine if dead*/
            for (int i = 0; i < functionCodeLines.Count; i++)
            {
                var localCoroutineCreateLine = functionCodeLines[i];

                if (!localCoroutineCreateLine.Equals("platform.window:invalidate()"))
                {
                    if (i > 0)
                        ResumeFunc.Add("coroutine.yield()");

                    ResumeFunc.Add($"__liveDebug_currentCodePosition_{randFuncName} = \"{localCoroutineCreateLine}\"");
                    ResumeFunc.Add($"__liveDebug_currentStep_{randFuncName} = __liveDebug_currentStep_{randFuncName} + " +
                                   $"platform.window:width()/{functionCodeLines.Count(x => !x.Equals("platform.window:invalidate()"))}");
                    ResumeFunc.Add("platform.window:invalidate()");

                    if (i < functionCodeLines.Count - 1)
                        ResumeFunc.Add("[Debug]");
                }

                /*main line*/
                ResumeFunc.Add(localCoroutineCreateLine);
                
            }
            ResumeFunc.Add("end)");//temp func end
            ResumeFunc.Add("end");//if end


            ResumeFunc.Add($"if not coroutine.running({randFuncName}) and __liveDebug_enterPressed_{randFuncName} then");
            ResumeFunc.Add(funcArgs.Replace(" ", "") != string.Empty ? $"coroutine.resume({randFuncName}({funcArgs}))" : $"coroutine.resume({randFuncName})");
            ResumeFunc.Add($"__liveDebug_enterPressed_{randFuncName} = false");
            ResumeFunc.Add("end");//second if (running && enterpressed


            ResumeFunc.Add("end");//ResumeFunc end
            ResumeFunc.Reverse();
            /*create ResumeFunction*/
            foreach (var VARIABLE in ResumeFunc)
            {
                lines.Insert(functionLineIndex, VARIABLE);
            }
            functionLineIndex += 10 /*const*/ + functionCodeLines.Count(x => !x.Equals("platform.window:invalidate()")) * 6
               + functionCodeLines.Count(x => x.Equals("platform.window:invalidate()")) - 1;
        }

        public static void SetOnTabKeyFunctionUp(ref List<string> lines, string funcArgs, string randFuncName)
        {
            int enterKeyFuncIndex = lines.FindIndex(x => x.Contains("function ontabKey"));
            if (enterKeyFuncIndex == -1)
            {
                List<string> onTabKeySetup = new List<string>
                    {
                        "function ontabKey()",
                        $"__liveDebug_enterPressed_{randFuncName} = true",
                        $"if __liveDebug_currentStep_{randFuncName} > platform.window:width() - 1 then __liveDebug_currentStep_{randFuncName} = 0 end",
                        $"ResumeFunc_{randFuncName}({funcArgs})",
                        "end"
                    };
                //onTabKeySetup.Reverse();

                foreach (var VARIABLE in onTabKeySetup)
                {
                    lines.Insert(lines.Count, VARIABLE);
                }
            }
            else
            {
                List<string> onTabKeySetup = new List<string>
                    {
                        $"__liveDebug_enterPressed_{randFuncName} = true",
                        $"if __liveDebug_currentStep_{randFuncName} > platform.window:width() - 1 then __liveDebug_currentStep_{randFuncName} = 0 end",
                        $"ResumeFunc_{randFuncName}({funcArgs})",
                    };
                onTabKeySetup.Reverse();

                foreach (var VARIABLE in onTabKeySetup)
                {
                    lines.Insert(enterKeyFuncIndex + 1, VARIABLE);
                }
            }

            /*drawString currentCodePosition*/


            InsertOnPaint(ref lines, randFuncName);
        }

        private static void InsertOnPaint(ref List<string> lines, string randFuncName)
        {
            List<string> onpaintInsert = new List<string>
            {
                $"__liveDebugPreviousFont_{randFuncName} = gc:setFont(\"serif\",\"bi\",8)",
                $"gc:drawString(\"[StackTrace]\"..__liveDebug_currentCodePosition_{randFuncName}, 0 , platform.window:height() - 10, \"top\")",
                $"gc:setFont(__liveDebugPreviousFont_{randFuncName},\"r\",10)",

                "gc:setColorRGB(255, 0, 0)",
                $"gc:fillRect(0,0,__liveDebug_currentStep_{randFuncName},3)",
                "gc:setColorRGB(0, 0, 0)"
            };
            onpaintInsert.Reverse();

            int onpaintIndex = lines.FindIndex(x => x.Contains("function onpaint"));
            foreach (var VARIABLE in onpaintInsert)
            {
                lines.Insert(onpaintIndex + 1, VARIABLE);
            }
        }
    }
}
