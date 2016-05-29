using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using MahApps.Metro.Controls.Dialogs;
using MoonSharp.Interpreter;

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
            public List<string> LuaLines { get; set; }

            private string CompileFileForLunaPath;
            public string GetLuaLinesFile()
            {
                string path = Path.Combine(Path.GetTempPath(),
                    "CompileFileForLuna" + RandomNumber(int.MaxValue) + ".lua");
                CompileFileForLunaPath = path;

                using (StreamWriter sw = new StreamWriter(path))
                {
                    foreach (string luaLine in LuaLines)
                    {
                        sw.WriteLine(luaLine);
                    }
                    sw.Close();
                }

                return path;
            }

            public void RemoveLunaFile()
            {
                try
                {
                    File.Delete(CompileFileForLunaPath);
                }
                catch { /**/}
            }
        }
        /// <summary>
        /// using hopefully extracted luna
        /// </summary>
        /// <returns></returns>
        public static string CompileLuaFile(string luaFilePath)
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
                    Arguments = "\"" + luaFilePath + "\"" + " \"" + tnsOutputPath + "\""
                }
            };
            process.Start();
            process.WaitForExit();

            return tnsOutputPath;
        }

        static List<string> GetLuaLines()
        {
            List<string> lines = new List<string>();
            using (StreamReader sr = new StreamReader(Testing.luapath))
            {
                while (!sr.EndOfStream)
                {
                    string line = sr.ReadLine();
                    lines.Add(line);
                }
                sr.Close();
            }
            return lines.Where(x => x.Length > 0).ToList();
        }

        private static readonly Random random = new Random();
        private static readonly object syncLock = new object();
        public static int RandomNumber(int max)
        {
            lock (syncLock)
            { // synchronize
                return random.Next(max);
            }
        }
        static List<string> ReplaceEvents(this List<string> luaLines)
        {
            return luaLines.Select(luaLine => luaLine.Contains("function on.") ? luaLine.Replace(".", "") : luaLine).ToList();
        }

        static List<string> RestoreEvents(this List<string> luaLines)
        {
            List<string> tempLines = luaLines;

            for (int i = 0; i < tempLines.Count; i++)
            {
                var line = tempLines[i];
                if (line.Contains("function on"))
                {
                    int spos = line.IndexOf("function on") + 11;
                    int epos = line.Length;

                    tempLines[i] = "function on." + line.Substring(spos, epos - spos);
                }
            }
            return tempLines;
        }

        static bool IsInOtherMethodScope(this string lineWitDeclVar, List<string> luaLines, string conversionLine)
        {
            int varLineIndex = luaLines.FindIndex(x => x == lineWitDeclVar);
            int conversionLineIndex = luaLines.FindIndex(x => x == conversionLine);

            List<int> functionStartPositions = new List<int>(2);
            List<bool> inMethod = new List<bool>(2);
            foreach (int currentLineIndex in new[] { varLineIndex, conversionLineIndex })
            {
                /*function start index*/
                int funcStart = SearchFunctionStart(luaLines, currentLineIndex);
                functionStartPositions.Add(funcStart);

                int functionEndIndex = SearchFunctionEnd(luaLines, funcStart);

                inMethod.Add(funcStart != -1 && functionEndIndex != -1);
            }

            bool inDifferentFuncs = functionStartPositions[0] != functionStartPositions[1];
            return inMethod[0] && inDifferentFuncs;
        }

        private static int SearchFunctionStart(List<string> luaLines, int lineIndex)
        {
            List<string> specialLoops = new List<string>
            {
                "if",
                "for",
                "while",
            };

            int ends = 0;
            for (int i = lineIndex - 1; i >= 0; i--)
            {
                string potentialFunctionLine = luaLines[i];

                if (potentialFunctionLine.Contains("end") && "end".stringNotInText(potentialFunctionLine))
                    ends++;

                ends -= specialLoops.Count(specialLoop => potentialFunctionLine.Contains(specialLoop) &&
                                                          specialLoop.stringNotInText(potentialFunctionLine));
                if (ends < 0) ends = 0;
               
                if (potentialFunctionLine.Contains("function")
                    /*&& !potentialFunctionLine.Contains("=") TEMP FUNC OK TOO*/ && ends == 0)
                    return i;
            }

            return -1;
        }

        private static int SearchFunctionEnd(List<string> luaLines, int functionStartIndex)
        {
            if (functionStartIndex == -1) //not in a function
                return -1;

            List<string> specialLoops = new List<string>
            {
                "if",
                "for",
                "while",
                "function ()",
                "function()"
            };

            int specialLoopsCount = 0;
            for (int i = functionStartIndex + 1; i < luaLines.Count; i++)
            {
                string potentialEndLine = luaLines[i];
                specialLoopsCount += specialLoops.Count(specialLoop => potentialEndLine.Contains(specialLoop) && 
                    specialLoop.stringNotInText(potentialEndLine));

                if (potentialEndLine.Contains("end") && specialLoopsCount == 0)
                {
                    return i;
                }
                else if (potentialEndLine.Contains("end") && "end".stringNotInText(potentialEndLine))
                    specialLoopsCount--;
                if (specialLoopsCount < 0) specialLoopsCount = 0;
            }

            return -1;
        }

        static List<string> CheckConversionIssues(this List<string> luaLines)
        {
            List<string> issues = new List<string>();

            foreach (string luaLine in luaLines.Where(x => x.Contains("tonumber(")))
            {
                int sp = luaLine.IndexOf("tonumber(") + 9;
                int ep = luaLine.IndexOf(")", sp);

                string num_str = luaLine.Substring(sp, ep - sp).Replace("\"", "").Replace(" ", "");

                try {Convert.ToDouble(num_str);}
                catch //num_str is a variable => failing to convert
                {
                    foreach (string variableLine in luaLines.Where(x => 
                        x.Contains(num_str) &&
                        x.Contains("=") &&
                        x.IndexOf(num_str) < x.IndexOf("=")
                    ))
                    {
                        if (variableLine.IsInOtherMethodScope(luaLines, luaLine))
                            issues.Add(variableLine + "=> [CompilingAnalysis] Number not declared in same function at '" +
                                       luaLine + "'");
                    }

                    if (luaLines.All(x => !(x.Contains(num_str) && x.Contains("=") && x.IndexOf(num_str) < x.IndexOf("="))))
                        issues.Add(luaLine + "=> [NaN] '" + num_str +"' not declared as number");
                }
            }

            return issues;
        }

        static string GetVariableName(this string varibaleDeclarationLine)
        {
            if (varibaleDeclarationLine.Contains("="))
            {
                int epos = varibaleDeclarationLine.IndexOf("=");
                int spos = varibaleDeclarationLine.Contains("local") ? varibaleDeclarationLine.IndexOf("local") + 5 : 0;
                string varname = varibaleDeclarationLine.Substring(spos, epos - spos).Replace(" ", "");
                return varname;
            }
            else //contains local
            {
                int epos = varibaleDeclarationLine.Length;
                int spos = varibaleDeclarationLine.IndexOf("local") + 5;
                string varname = varibaleDeclarationLine.Substring(spos, epos - spos).Replace(" ", "");
                return varname;
            }
        }

        static bool IsInMethodScope(this string declLine, List<string> luaLines, int declIndex)
        {
            return SearchFunctionStart(luaLines, declIndex) != -1;
        }

        static bool stringNotInText(this string str, string line)
        {
            char oneIndexEarlierThanVarStartIndex = line.IndexOf(str) != 0 ?
                        line[line.IndexOf(str) - 1] : '(';
            char oneIndexLaterThanVarStartIndex = line.IndexOf(str) + str.Length <= line.Length - 1 ?
                line[line.IndexOf(str) + str.Length] : ')';

            /*varname not randomly in unknown string included*/
            bool varNameNotInText = Regex.Matches(oneIndexEarlierThanVarStartIndex.ToString(), @"[a-zA-Z0-9]").Count == 0 &&
                Regex.Matches(oneIndexLaterThanVarStartIndex.ToString(), @"[a-zA-Z0-9]").Count == 0; //e.g. function hELLO()...

            if (varNameNotInText && (oneIndexEarlierThanVarStartIndex == '_' || oneIndexLaterThanVarStartIndex == '_'))
                varNameNotInText = false;


            return varNameNotInText;
        }

        static List<string> CheckDeclarationIssues(this List<string> luaLines)
        {
            List<string> issues = new List<string>();
            List<string> variableNames, variableDeclarationLines;
            List<int> varDeclIndexes;
            GetVariableDeclarationLines(luaLines, out variableNames, out variableDeclarationLines, out varDeclIndexes);

            // ReSharper disable once LoopCanBeConvertedToQuery
            // ReSharper disable once ForCanBeConvertedToForeach
            for (int currentLineIndex = 0; currentLineIndex < luaLines.Count; currentLineIndex++)
            {
                string variableUseLine = luaLines[currentLineIndex];
                List<string> varNameOccurrencesInLuaLine = variableNames.Where(var =>
                {
                    if (!variableUseLine.Contains(var))
                        return false;

                    return var.stringNotInText(variableUseLine);
                }).ToList();

                // ReSharper disable once LoopCanBeConvertedToQuery
                foreach (string varNameOccurrenceInLine in varNameOccurrencesInLuaLine)
                {
                    bool declLineItself = variableDeclarationLines.Any(x =>
                        x.GetVariableName() == varNameOccurrenceInLine && x == variableUseLine);

                    if (declLineItself)
                        continue;

                    string declLine = variableDeclarationLines.First(x => x.GetVariableName() == varNameOccurrenceInLine);
                    int declIndex = varDeclIndexes.First(x => luaLines[x] == declLine);

                    if (declLine.IsInOtherMethodScope(luaLines, variableUseLine))
                    {
                        string varName = variableNames.First(variableUseLine.Contains);
                        issues.Add("'" + varName +
                                   "' => [CompilingAnalysis] The variable's declaration is not reachable at '" +
                                   variableUseLine + "'");
                    }
                    else //var theoretically accessable but maybe declared too late
                    {
                        if (currentLineIndex < declIndex) //only too late definition only at global vars
                        {
                            string varName = variableNames.First(variableUseLine.Contains);
                            issues.Add("'" + varName +
                                   "' => [CompilingAnalysis] The variable's declaration is too late in code at'" +
                                   declLine + $"'\nMaximum allowed declaration line: {SearchFunctionStart(luaLines, currentLineIndex) - 1}");
                        }
                    }
                }
            }

            return issues;
        }

        private static void GetVariableDeclarationLines(List<string> luaLines,
            out List<string> variableNames, out List<string> variableDeclarationLines, out List<int> varDeclIndexes)
        {
            variableNames = new List<string>();
            variableDeclarationLines = new List<string>();
            varDeclIndexes = new List<int>();
            for (int i = 0; i < luaLines.Count; i++)
            {
                string luaLine = luaLines[i];
                int indexOfEqual = luaLine.IndexOf("=");
                bool isVarSet = luaLine.Contains("=") && luaLine.Count(x => x == '=') == 1 &&
                                luaLine.Any(charr => luaLine.IndexOf(charr) < indexOfEqual);
                if (!isVarSet && luaLine.Contains("local ") && !luaLine.Contains("local function"))
                {
                    isVarSet = true;
                }

                Func<bool> alreadyDeclaredGlobally = () =>
                {
                    for (int j = 0; j < luaLines.Count; j++)
                    {
                        if (i == j) continue;

                        string luaLine2 = luaLines[j];
                        int indexOfEqual2 = luaLine2.IndexOf("=");
                        bool isVarSet2 = luaLine2.Contains("=") && luaLine2.Count(x => x == '=') == 1 &&
                                        luaLine2.Any(charr => luaLine2.IndexOf(charr) < indexOfEqual2);
                        if (!isVarSet2 && luaLine2.Contains("local ") && !luaLine2.Contains("local function"))
                        {
                            isVarSet2 = true;
                        }

                        if (isVarSet2 && luaLine2.GetVariableName() == luaLine.GetVariableName()) /*other delcaration found*/
                        {
                            if (!luaLine2.IsInMethodScope(luaLines, j))
                                return true;
                        }
                    }
                    

                    return false;
                };

                if (isVarSet && !alreadyDeclaredGlobally())
                {
                    varDeclIndexes.Add(i);
                    variableNames.Add(luaLine.GetVariableName());
                    variableDeclarationLines.Add(luaLine);
                }
            }
        }

        enum FunctionAttributes
        {
            ScreenUpdate,
            Debug,
            LiveDebug,
            Thread
        }

        enum FieldAttributes
        {
            Debug,
        } 

        static List<string> CheckFunctionAttribute(this List<string> luaLines, FunctionAttributes funcAttribute, 
            out bool error)
        {
            List<string> luaTemplateLines = luaLines;

            for (int i = 0; i < luaLines.Count; i++)
            {
                string currentLuaLine = luaLines[i];
                if (currentLuaLine.Contains($"[{funcAttribute.ToString("G")}]"))
                {
                    bool isAttributeOfFunction = luaLines[i + 1].Contains("function");

                    if (isAttributeOfFunction)
                    {
                        luaTemplateLines = HandleFunctionAttribute(luaTemplateLines, funcAttribute,i);
                    }
                }
            }

            error = luaTemplateLines.Any(x => x.Contains("[Error]"));
            return luaTemplateLines;
        }
        static List<string> CheckFieldAttribute(this List<string> luaLines, FieldAttributes fieldAttribute,
            out bool error)
        {
            List<string> luaTemplateLines = luaLines;

            for (int i = 0; i < luaLines.Count; i++)
            {
                string currentLuaLine = luaLines[i];
                if (currentLuaLine.Contains($"[{fieldAttribute.ToString("G")}]"))
                {
                    luaTemplateLines = HandleFieldAttribute(luaTemplateLines, fieldAttribute, i);
                }
            }

            error = luaTemplateLines.Any(x => x.Contains("[Error]"));
            return luaTemplateLines;
        }

        private static List<string> HandleFieldAttribute(List<string> luaLines, FieldAttributes fieldAttribute, int i)
        {
            int fieldIndex = i + 1;
            bool nextLineIsScreenRefresh = luaLines[fieldIndex + 1].Equals("platform.window:invalidate()");
            List<string> luaTemplateLines = luaLines;

            if (fieldAttribute == FieldAttributes.Debug)
            {
                string seed = RandomNumber(int.MaxValue).ToString();
                int funcStartIndex = SearchFunctionStart(luaLines, fieldIndex);
                bool startFuncIsTemp = luaLines[funcStartIndex].Contains("function ()") ||
                                       luaLines[funcStartIndex].Contains("function()");
                if (startFuncIsTemp)
                    funcStartIndex = SearchFunctionStart(luaLines, funcStartIndex);
                if (funcStartIndex == -1) //temp func is first in script
                    funcStartIndex = SearchFunctionStart(luaLines, fieldIndex);

                string fieldstr = luaLines[fieldIndex];

                List<string> OnFieldCallFunc = !nextLineIsScreenRefresh ? new List<string>
                {
                    $"function OnFieldCall{seed}()",
                    fieldstr,
                    "end"
                } 
                : 
                new List<string>
                {
                    $"function OnFieldCall{seed}()",
                    fieldstr,
                    "platform.window:invalidate()",
                    "end"
                };
                OnFieldCallFunc.Reverse();
                List<string> OnFieldCallFuncFail = new List<string>
                {
                    $"local __errorHandleVar{seed} = \"\"",
                    $"function OnFieldCall{seed}_Fail(err)",
                    $"__errorHandleVar{seed} = tostring(err)",
                    "end"
                };
                OnFieldCallFuncFail.Reverse();

                foreach (var VARIABLE in OnFieldCallFunc)
                {
                    luaTemplateLines.Insert(funcStartIndex, VARIABLE);
                }
                foreach (var VARIABLE in OnFieldCallFuncFail)
                {
                    luaTemplateLines.Insert(funcStartIndex, VARIABLE);
                }
                funcStartIndex += OnFieldCallFunc.Count + OnFieldCallFuncFail.Count;
                fieldIndex += OnFieldCallFunc.Count + OnFieldCallFuncFail.Count;

                /*clear debug attribute + field + screenUpdate?*/
                luaTemplateLines.RemoveAt(fieldIndex - 1);
                luaTemplateLines.RemoveAt(fieldIndex - 1);
                if (nextLineIsScreenRefresh) luaTemplateLines.RemoveAt(fieldIndex - 1);
                luaTemplateLines.Insert(fieldIndex - 1, $"xpcall( OnFieldCall{seed}, OnFieldCall{seed}_Fail )");

                int errorHandleCount = luaTemplateLines.Count(x => x.Contains("local __errorHandleVar"));
                luaTemplateLines.Insert(luaTemplateLines.FindIndex(
                   x => x.Contains("function onpaint")) + 1, $"if __errorHandleVar{seed} ~= \"\" then gc:drawString(\"[DebugError] \"..__errorHandleVar{seed}, 0, platform.window:height() - {20 /**errorHandleCount*/}, \"top\") end");
            }

            return luaTemplateLines;
        }

        private static List<string> HandleFunctionAttribute(List<string> luaLines, FunctionAttributes funcAttribute, int lineIndex)
        {
            List<string> luaLinesTemplate = luaLines;
            int functionLineIndex = lineIndex + 1;

            if (funcAttribute == FunctionAttributes.ScreenUpdate)
            {
                #region ScreenUpdate
                int EndFuncIndex = SearchFunctionEnd(luaLinesTemplate, functionLineIndex);
                luaLinesTemplate.Insert(EndFuncIndex, "platform.window:invalidate()");
                luaLinesTemplate.RemoveAt(functionLineIndex - 1);
                #endregion
            }
            else if (funcAttribute == FunctionAttributes.Debug)
            {
                #region Debug
                int s = luaLinesTemplate[functionLineIndex].IndexOf("(") + 1;
                int e = luaLinesTemplate[functionLineIndex].IndexOf(")");
                string rawFuncArgs = luaLinesTemplate[functionLineIndex].Substring(s, e - s).Replace(" ", "");
                if (rawFuncArgs != string.Empty)
                    return new List<string> { "[Error] Debug function contains parameters" };
                int funcEndIndex = SearchFunctionEnd(luaLinesTemplate, functionLineIndex);

                /*remove and safe func code*/
                int removeIndex = functionLineIndex + 1;
                List<string> functionCodeLines = new List<string>();
                for (int i = functionLineIndex + 1; i < funcEndIndex; i++)
                {
                    functionCodeLines.Add(luaLinesTemplate[removeIndex]);
                    luaLinesTemplate.RemoveAt(removeIndex);
                }

                /*create detour func*/
                string randFunctionSeed = RandomNumber(int.MaxValue).ToString();
                string detourFunctionName =
                    luaLinesTemplate[functionLineIndex].Substring(luaLinesTemplate[functionLineIndex].IndexOf("function ") + 9,
                        luaLinesTemplate[functionLineIndex].IndexOf("(") -
                        (luaLinesTemplate[functionLineIndex].IndexOf("function ") + 9)).Replace(" ", "") + randFunctionSeed;

                //new function name
                luaLinesTemplate.Insert(functionLineIndex - 1, "function " + detourFunctionName + " ()");
                functionCodeLines.Reverse();
                foreach (string functionCodeLine in functionCodeLines)
                {
                    luaLinesTemplate.Insert(functionLineIndex, functionCodeLine);
                }
                //function end
                luaLinesTemplate.Insert(functionLineIndex + functionCodeLines.Count, "end");
                functionLineIndex += functionCodeLines.Count + 2;

                /*create errorHandleFunc*/
                List<string> errorHandleFunc = new List<string>
                {
                    $"local __errorHandleVar{randFunctionSeed} = \"\"",
                    $"function onFunction{randFunctionSeed}_Fail(err)",
                    $"__errorHandleVar{randFunctionSeed} = tostring(err)",
                    "end"
                };
                errorHandleFunc.Reverse();

                foreach (string s1 in errorHandleFunc)
                {
                    luaLinesTemplate.Insert(functionLineIndex - 1, s1);
                }
                functionLineIndex += errorHandleFunc.Count;

                luaLinesTemplate.Insert(functionLineIndex + 1, $"xpcall( {detourFunctionName}, onFunction{randFunctionSeed}_Fail )");

                /*remove debug attribute*/
                luaLinesTemplate.RemoveAt(functionLineIndex - 1);

                int errorHandleCount = luaLinesTemplate.Count(x => x.Contains("local __errorHandleVar"));
                luaLinesTemplate.Insert(luaLinesTemplate.FindIndex(
                    x => x.Contains("function onpaint")) + 1, $"if __errorHandleVar{randFunctionSeed} ~= \"\" then gc:drawString(\"[DebugError] \"..__errorHandleVar{randFunctionSeed}, 0, platform.window:height() - {20 /**errorHandleCount*/}, \"top\") end");
                #endregion
            }
            else if (funcAttribute == FunctionAttributes.Thread)
            {
                #region Thread
                int s = luaLinesTemplate[functionLineIndex].IndexOf("function ") + 9;
                int e = luaLinesTemplate[functionLineIndex].IndexOf("(");
                string funcName = luaLinesTemplate[functionLineIndex].Substring(s, e - s);

                int s3 = luaLinesTemplate[functionLineIndex].IndexOf("(") + 1;
                int e3 = luaLinesTemplate[functionLineIndex].IndexOf(")");
                string funcArgs = luaLinesTemplate[functionLineIndex].Substring(s3, e3 - s3).Replace(" ", "");

                string funcAsTempFuncName = luaLinesTemplate[functionLineIndex].Replace(funcName, "");
                string randFuncName = funcName + RandomNumber(int.MaxValue);
                string ThreadFuncVar = $"local {randFuncName} = coroutine.wrap({funcAsTempFuncName}";

                /*remove func code and safe in list*/
                int removeIndex = functionLineIndex + 1;
                int funcEndIndex = SearchFunctionEnd(luaLines, functionLineIndex);
                bool isReturnFunction = false;
                List<string> functionCodeLines = new List<string>();
                for (int i = functionLineIndex + 1; i < funcEndIndex; i++)
                {
                    if (luaLinesTemplate[removeIndex].Contains("return "))
                    {
                        isReturnFunction = true;
                        int s2 = luaLinesTemplate[removeIndex].IndexOf("return") + 6;
                        string returnVal = luaLinesTemplate[removeIndex].Substring(s2,
                            luaLinesTemplate[removeIndex].Length - s2).Replace(" ", "");
                        functionCodeLines.Add($"coroutine.yield({returnVal})");
                    }
                    else
                        functionCodeLines.Add(luaLinesTemplate[removeIndex]);
                    luaLinesTemplate.RemoveAt(removeIndex);
                }
                functionCodeLines.Reverse();

                /*createThreadCloneFunc*/
                luaLinesTemplate.Insert(functionLineIndex, "end");
                luaLinesTemplate.Insert(functionLineIndex, isReturnFunction ? 
                    $"return {randFuncName}({funcArgs})" : $"{randFuncName}({funcArgs})");

                luaLinesTemplate.Insert(functionLineIndex, "end)");
                foreach (string functionCodeLine in functionCodeLines)
                {
                    luaLinesTemplate.Insert(functionLineIndex, functionCodeLine);
                }
                luaLinesTemplate.Insert(functionLineIndex, ThreadFuncVar);
                luaLinesTemplate.Insert(functionLineIndex, $"function ThreadCloneFunc_{funcName}({funcArgs})");

                /*call clone func from original func*/
                luaLinesTemplate.Insert(functionLineIndex + 6 + functionCodeLines.Count, 
                    isReturnFunction ? $"return ThreadCloneFunc_{funcName}({funcArgs})" : $"ThreadCloneFunc_{funcName}({funcArgs})");

                /*remove attribute*/
                luaLinesTemplate.RemoveAt(functionLineIndex - 1);
                #endregion
            }
            else if (funcAttribute == FunctionAttributes.LiveDebug)
            {
                #region LiveDebug
                /*remove attribute*/
                luaLinesTemplate.RemoveAt(functionLineIndex - 1);
                functionLineIndex--;
                
                int s = luaLinesTemplate[functionLineIndex].IndexOf("function ") + 9;
                int e = luaLinesTemplate[functionLineIndex].IndexOf("(");
                string funcName = luaLinesTemplate[functionLineIndex].Substring(s, e - s);

                int s3 = luaLinesTemplate[functionLineIndex].IndexOf("(") + 1;
                int e3 = luaLinesTemplate[functionLineIndex].IndexOf(")");
                string funcArgs = luaLinesTemplate[functionLineIndex].Substring(s3, e3 - s3).Replace(" ", "");

                string funcAsTempFuncName = luaLinesTemplate[functionLineIndex].Replace(funcName, "");
                string randFuncName = funcName + "_liveDebug" + RandomNumber(int.MaxValue);
                string ThreadFuncVar = $"local {randFuncName} = coroutine.create({funcAsTempFuncName}";

                /*remove func code and safe in list*/
                int removeIndex = functionLineIndex + 1;
                int funcEndIndex = SearchFunctionEnd(luaLines, functionLineIndex);
                List<string> functionCodeLines = new List<string>();
                for (int i = functionLineIndex + 1; i < funcEndIndex; i++)
                {
                    if (luaLinesTemplate[removeIndex].Contains("return "))
                    {
                        return new List<string> { "[Error] LiveDebug-Function is not allowed to return a value" };
                    }
                    else
                        functionCodeLines.Add(luaLinesTemplate[removeIndex]);
                    luaLinesTemplate.RemoveAt(removeIndex);
                }
                functionCodeLines.Reverse();

                /*create local coroutine.create var*/
                luaLinesTemplate.Insert(functionLineIndex, "end)");
                for (int i = 0; i < functionCodeLines.Count; i++)
                {
                    var localCoroutineCreateLine = functionCodeLines[i];
                    luaLinesTemplate.Insert(functionLineIndex, localCoroutineCreateLine);
                    if (!localCoroutineCreateLine.Equals("platform.window:invalidate()"))
                    {
                        /*before / reversed*/
                        luaLinesTemplate.Insert(functionLineIndex, "[Debug]");
                        luaLinesTemplate.Insert(functionLineIndex, "platform.window:invalidate()");
                        luaLinesTemplate.Insert(functionLineIndex,
                            $"__liveDebug_currentCodePosition_{randFuncName} = \"{localCoroutineCreateLine}\"");

                        if (i < functionCodeLines.Count - 1)//dont yield before 1st code line
                            luaLinesTemplate.Insert(functionLineIndex, "coroutine.yield()");
                    }
                }
                luaLinesTemplate.Insert(functionLineIndex, ThreadFuncVar);
                luaLinesTemplate.Insert(functionLineIndex, $"local __liveDebug_currentCodePosition_{randFuncName} = \"No calls of LiveDebug-Function\"");
                luaLinesTemplate.Insert(functionLineIndex, $"local __liveDebug_enterPressed_{randFuncName} = false");

                functionLineIndex += 4 + functionCodeLines.Count(x=> !x.Equals("platform.window:invalidate()"))*5
                    + functionCodeLines.Count(x => x.Equals("platform.window:invalidate()")) - 1;


                List<string> ResumeFunc = new List<string>
                {
                    "end", //ResumeFunc end

                    "end",//second if (running && enterpressed)
                    $"__liveDebug_enterPressed_{randFuncName} = false",
                    funcArgs.Replace(" ", "") != string.Empty ? $"coroutine.resume({randFuncName}({funcArgs}))" : 
                                                                                            $"coroutine.resume({randFuncName})",
                    $"if not coroutine.running({randFuncName}) and __liveDebug_enterPressed_{randFuncName} then",


                    "end",//if end
                    "end)"//temp func end
                };
                /*re-define coroutine if dead*/
                for (int i = 0; i < functionCodeLines.Count; i++)
                {
                    var localCoroutineCreateLine = functionCodeLines[i];
                    ResumeFunc.Add(localCoroutineCreateLine);
                    if (!localCoroutineCreateLine.Equals("platform.window:invalidate()"))
                    {
                        ResumeFunc.Add("[Debug]");
                        ResumeFunc.Add("platform.window:invalidate()");
                        ResumeFunc.Add(
                            $"__liveDebug_currentCodePosition_{randFuncName} = \"{localCoroutineCreateLine}\"");

                        if (i < functionCodeLines.Count - 1)
                            ResumeFunc.Add("coroutine.yield()");
                    }
                }
                ResumeFunc.Add(ThreadFuncVar.Replace("local ", ""));
                ResumeFunc.Add($"if coroutine.status({randFuncName}) == \"dead\" then");
                ResumeFunc.Add($"function ResumeFunc_{randFuncName}({funcArgs})");

                
                /*create ResumeFunction*/
                foreach (var VARIABLE in ResumeFunc)
                {
                    luaLinesTemplate.Insert(functionLineIndex, VARIABLE);
                }
                functionLineIndex += 10 /*const*/ + functionCodeLines.Count(x => !x.Equals("platform.window:invalidate()")) * 5
                   + functionCodeLines.Count(x => x.Equals("platform.window:invalidate()")) - 1;

                /*call ResumeFunction at orignial func*/
                luaLinesTemplate.Insert(functionLineIndex + 1, $"ResumeFunc_{randFuncName}({funcArgs})");


                /*set on.tabKey function up*/
                int enterKeyFuncIndex = luaLinesTemplate.FindIndex(x => x.Contains("function ontabKey"));
                if (enterKeyFuncIndex == -1)
                {
                    List<string> onTabKeySetup = new List<string>
                    {
                        "function ontabKey()",
                        $"__liveDebug_enterPressed_{randFuncName} = true",
                        $"ResumeFunc_{randFuncName}({funcArgs})",
                        "end"
                    };
                    //onTabKeySetup.Reverse();

                    foreach (var VARIABLE in onTabKeySetup)
                    {
                        luaLinesTemplate.Insert(luaLinesTemplate.Count, VARIABLE);
                    }
                }
                else
                {
                    List<string> onTabKeySetup = new List<string>
                    {
                        $"__liveDebug_enterPressed_{randFuncName} = true",
                        $"ResumeFunc_{randFuncName}({funcArgs})",
                    };
                    onTabKeySetup.Reverse();

                    foreach (var VARIABLE in onTabKeySetup)
                    {
                        luaLinesTemplate.Insert(enterKeyFuncIndex + 1, VARIABLE);
                    }
                }

                /*drawString currentCodePosition*/
                int onpaintIndex = luaLinesTemplate.FindIndex(x => x.Contains("function onpaint"));
                luaLinesTemplate.Insert(onpaintIndex + 1,
                    $"gc:drawString(\"[StackTrace]\"..__liveDebug_currentCodePosition_{randFuncName}, 0 , platform.window:height() - 20, \"top\")");

                #endregion
            }

            return luaLinesTemplate;
        }

        public static CodeAnalysisInfo RunCodeAnalysis()
        {
            var lines = GetLuaLines();
            lines = lines.ReplaceEvents().Select(x => x.Replace("\t", "")).ToList();
            List<string> declIssues = CheckDeclarationIssues(lines);

            List<string> funcAttributeErros = ConvertFunctionAttributes(ref lines);
            List<string> fieldAttributeErros = ConvertFieldAttributes(ref lines);

            try
            {
                List<string> conversionIssues = CheckConversionIssues(lines);
                declIssues.AddRange(conversionIssues);
                declIssues.AddRange(funcAttributeErros);
                declIssues.AddRange(fieldAttributeErros);
            }
            catch
            {
                //codeAnalysis fail
                WindowManager.MainWindow.ShowMessageAsync("Information", "CodeAnalyse fehlgeschlagen..Lua->Luna->Kompilierung wird fortgesetzt.");
            }

            #region through the compiler
            if (MyResourceManager.LuaCompilerInstalled)
            {
                string debugPath = Path.GetTempPath() + Testing.luafilenameNoExtension + "_Debug.lua";

                using (StreamWriter sw = new StreamWriter(debugPath))
                {
                    foreach (string line in lines)
                    {
                        sw.WriteLine(line);
                    }
                    sw.Close();
                }

                string errorMessage = "";
                try
                {
                    Script.RunFile(debugPath);
                }
                catch (Exception _errorMessage)
                {
                    errorMessage = "[LUA] " + _errorMessage.Message;
                }

                declIssues.Add("\n" + errorMessage);
                try { File.Delete(debugPath); } catch {/* ignored*/}
            }
            #endregion

            if (Testing.DebugMode)
                File.WriteAllLines(Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + "\\luaLines.txt", 
                lines);

            return new CodeAnalysisInfo
            {
                Result = declIssues.All(x => x == "" || x == "\n") /*Code fine string*/ ?
                    CodeAnalysisResult.CodeFine : CodeAnalysisResult.Warnings,
                Announcements = declIssues,
                LuaLines = lines.RestoreEvents()
            };
        }

        private static List<string> ConvertFieldAttributes(ref List<string> lines)
        {
            List<string> attributeErros = new List<string>();
            foreach (object fieldAttribute in Enum.GetValues(typeof(FieldAttributes)))
            {
                bool err = false;
                var output = lines.CheckFieldAttribute((FieldAttributes)fieldAttribute, out err);

                if (err)
                    attributeErros.AddRange(output);
                else
                    lines = output;
            }

            return attributeErros;
        }

        private static List<string> ConvertFunctionAttributes(ref List<string> lines)
        {
            List<string> attributeErros = new List<string>();
            foreach (FunctionAttributes funcAttribute in Enum.GetValues(typeof(FunctionAttributes)).
                Cast<FunctionAttributes>().OrderBy(x => x == FunctionAttributes.LiveDebug))
            {
                bool err = false;
                var output = lines.CheckFunctionAttribute(funcAttribute, out err);

                if (err)
                    attributeErros.AddRange(output);
                else
                    lines = output;
            }

            return attributeErros;
        }
    }
}
