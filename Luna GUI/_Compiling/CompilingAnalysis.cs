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
                    "CompileFileForLuna" + new Random().Next(int.MaxValue) + ".lua");
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
            bool varNameNotInText = Regex.Matches(oneIndexEarlierThanVarStartIndex.ToString(), @"[a-zA-Z]").Count == 0 &&
                Regex.Matches(oneIndexLaterThanVarStartIndex.ToString(), @"[a-zA-Z]").Count == 0; //e.g. function hELLO()...

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
                        if (currentLineIndex < declIndex)
                        {
                            string varName = variableNames.First(variableUseLine.Contains);
                            issues.Add("'" + varName +
                                   "' => [CompilingAnalysis] The variable's declaration is too late in code at'" +
                                   declLine + "'");
                        }
                    }
                }
            }

            return issues;
        }

        private static void GetVariableDeclarationLines(List<string> luaLines, 
            out List<string> variableNames, out List<string> variableDeclarationLines)
        {
            variableNames = new List<string>();
            variableDeclarationLines = new List<string>();
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

                if (isVarSet&& !alreadyDeclaredGlobally())
                {
                    variableNames.Add(luaLine.GetVariableName());
                    variableDeclarationLines.Add(luaLine);
                }
            }
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
                    List<string> variableNames, variableDeclarationLines;
                    GetVariableDeclarationLines(luaLines, out variableNames, out variableDeclarationLines);

                    bool isAttributeOfField = variableNames.Any(luaLines[i + 1].Contains) || 
                        variableDeclarationLines.Any(luaLines[i + 1].Equals);

                    if (isAttributeOfField)
                    {
                        luaTemplateLines = HandleFieldAttribute(luaTemplateLines, fieldAttribute, i);
                    }
                }
            }

            error = luaTemplateLines.Any(x => x.Contains("[Error]"));
            return luaTemplateLines;
        }

        private static List<string> HandleFieldAttribute(List<string> luaLines, FieldAttributes fieldAttribute, int i)
        {
            int fieldIndex = i + 1;
            List<string> luaTemplateLines = luaLines;

            if (fieldAttribute == FieldAttributes.Debug)
            {
                Random rand = new Random();
                string seed = rand.Next(int.MaxValue).ToString();
                int funcStartIndex = SearchFunctionStart(luaLines, fieldIndex);
                string fieldstr = luaLines[fieldIndex];

                List<string> OnFieldCallFunc = new List<string>
                {
                    $"function OnFieldCall{seed}()",
                    fieldstr,
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

                /*clear debug attribute + field*/
                luaTemplateLines.RemoveAt(fieldIndex - 1);
                luaTemplateLines.RemoveAt(fieldIndex - 1);
                luaTemplateLines.Insert(fieldIndex - 1, $"xpcall( OnFieldCall{seed}, OnFieldCall{seed}_Fail )");

                int errorHandleCount = luaTemplateLines.Count(x => x.Contains("local __errorHandleVar"));
                luaTemplateLines.Insert(luaTemplateLines.FindIndex(
                    x => x.Contains("function on.paint()")) + 2, $"gc:drawString(\"[DebugMode] \"..__errorHandleVar{seed}," +
                                                                 $" 150, {5 * errorHandleCount}, \"top\")");
            }

            return luaTemplateLines;
        }

        private static List<string> HandleFunctionAttribute(List<string> luaLines, FunctionAttributes funcAttribute, int lineIndex)
        {
            List<string> luaLinesTemplate = luaLines;
            int functionLineIndex = lineIndex + 1;

            if (funcAttribute == FunctionAttributes.ScreenUpdate)
            {
                int EndFuncIndex = SearchFunctionEnd(luaLinesTemplate, functionLineIndex);
                luaLinesTemplate.Insert(EndFuncIndex, "platform.window:invalidate()");
                luaLinesTemplate.RemoveAt(functionLineIndex - 1);
            }
            else if (funcAttribute == FunctionAttributes.Debug)
            {
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
                Random seed = new Random();
                string randFunctionSeed = seed.Next(int.MaxValue).ToString();
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
                    x => x.Contains("function on.paint()")) + 2, $"gc:drawString(\"[DebugMode] \"..__errorHandleVar{randFunctionSeed}, " +
                                                                 $"150, {5 * errorHandleCount}, \"top\")");
            }
            else if (funcAttribute == FunctionAttributes.Thread)
            {
                int s = luaLinesTemplate[functionLineIndex].IndexOf("function ") + 9;
                int e = luaLinesTemplate[functionLineIndex].IndexOf("(");
                string funcName = luaLinesTemplate[functionLineIndex].Substring(s, e - s);

                string funcAsTempFuncName = luaLinesTemplate[functionLineIndex].Replace(funcName, "");
                string randFuncName = funcName + new Random().Next(int.MaxValue);
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
                    $"return {randFuncName}()" : $"{randFuncName}()");

                luaLinesTemplate.Insert(functionLineIndex, "end)");
                foreach (string functionCodeLine in functionCodeLines)
                {
                    luaLinesTemplate.Insert(functionLineIndex, functionCodeLine);
                }
                luaLinesTemplate.Insert(functionLineIndex, ThreadFuncVar);
                luaLinesTemplate.Insert(functionLineIndex, $"function ThreadCloneFunc_{funcName}()");

                /*call clone func from original func*/
                luaLinesTemplate.Insert(functionLineIndex + 6 + functionCodeLines.Count, 
                    isReturnFunction ? $"return ThreadCloneFunc_{funcName}()" : $"ThreadCloneFunc_{funcName}()");

                /*remove attribute*/
                luaLinesTemplate.RemoveAt(functionLineIndex - 1);
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
            foreach (object funcAttribute in Enum.GetValues(typeof(FunctionAttributes)))
            {
                bool err = false;
                var output = lines.CheckFunctionAttribute((FunctionAttributes)funcAttribute, out err);

                if (err)
                    attributeErros.AddRange(output);
                else
                    lines = output;
            }

            return attributeErros;
        }
    }
}
