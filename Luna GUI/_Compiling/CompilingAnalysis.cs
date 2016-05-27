using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using MahApps.Metro.Controls.Dialogs;
using MoonSharp.Interpreter;
using NLua;

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
            return luaLines.Select(luaLine => luaLine.Contains("on.") ? luaLine.Replace(".", "") : luaLine).ToList();
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
            int ends = 0;
            for (int i = lineIndex - 1; i >= 0; i--)
            {
                string potentialFunctionLine = luaLines[i];

                if (potentialFunctionLine.Contains("end"))
                    ends++;

                if (potentialFunctionLine.Contains("if") || potentialFunctionLine.Contains("while") ||
                    potentialFunctionLine.Contains("for"))
                    ends--;

                if (potentialFunctionLine.Contains("function") && !potentialFunctionLine.Contains("=") && ends == 0)
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
                "while"
            };
            int specialLoopsCount = 0;
            for (int i = functionStartIndex + 1; i < luaLines.Count; i++)
            {
                string potentialEndLine = luaLines[i];
                specialLoopsCount += specialLoops.Count(specialLoop => potentialEndLine.Contains(specialLoop));

                if (potentialEndLine.Contains("end") && specialLoopsCount == 0)
                {
                    int funcEndIndex = luaLines.FindIndex(x => x == potentialEndLine);
                    return funcEndIndex;
                }
                else if (potentialEndLine.Contains("end"))
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
            int epos = varibaleDeclarationLine.IndexOf("=");
            int spos = varibaleDeclarationLine.Contains("local") ? varibaleDeclarationLine.IndexOf("local") + 5 : 0;
            string varname = varibaleDeclarationLine.Substring(spos, epos - spos).Replace(" ", "");
            return varname;
        }

        static bool IsInMethodScope(this string declLine, List<string> luaLines, int declIndex)
        {
            return SearchFunctionStart(luaLines, declIndex) != -1;
        }

        static List<string> CheckDeclarationIssues(this List<string> luaLines)
        {
            List<string> issues = new List<string>();
            List<string> variableNames, variableDeclarationLines;
            GetVariableDeclarationLines(luaLines, out variableNames, out variableDeclarationLines);

            foreach (string variableUseLine in luaLines)
            {
                List<string> varNameOccurrencesInLuaLine = variableNames.Where(var =>
                {
                    if (!variableUseLine.Contains(var))
                        return false;

                    char oneIndexLowerThanVarStartIndex = variableUseLine.IndexOf(var) != 0 ?
                        variableUseLine[variableUseLine.IndexOf(var) - 1] : '(';

                    /*varname not randomly in unknown string included*/
                    bool varNameNotInText = Regex.Matches(oneIndexLowerThanVarStartIndex.ToString(), @"[a-zA-Z]").Count == 0 &&
                        Regex.Matches(variableUseLine[variableUseLine.IndexOf(var) + var.Length].ToString(), @"[a-zA-Z]").Count == 0; //e.g. function hELLO()...

                    return varNameNotInText;
                }).ToList();

                foreach (string varNameOccurrenceInLine in varNameOccurrencesInLuaLine)
                {
                    bool declLineItself = variableDeclarationLines.Any(x =>
                            x.GetVariableName() == varNameOccurrenceInLine && x == variableUseLine);

                    if (declLineItself)
                        continue;

                    string declLine = variableDeclarationLines.First(x => x.GetVariableName() == varNameOccurrenceInLine);
                    if (declLine.IsInOtherMethodScope(luaLines, variableUseLine))
                    {
                        string varName = variableNames.First(variableUseLine.Contains);
                        issues.Add("'" + varName + "' => [CompilingAnalysis] The variable's declaration is not reachable at '" +
                            variableUseLine + "'");
                    }
                }
            }

            return issues;
        }

        private static void GetVariableDeclarationLines(List<string> luaLines, out List<string> variableNames, out List<string> variableDeclarationLines)
        {
            variableNames = new List<string>();
            variableDeclarationLines = new List<string>();
            for (int i = 0; i < luaLines.Count; i++)
            {
                string luaLine = luaLines[i];
                int indexOfEqual = luaLine.IndexOf("=");
                bool isVarSet = luaLine.Contains("=") && luaLine.Count(x => x == '=') == 1 &&
                                luaLine.Any(charr => luaLine.IndexOf(charr) < indexOfEqual);

                Func<bool> alreadyDeclaredGlobally = () =>
                {
                    for (int j = 0; j < luaLines.Count; j++)
                    {
                        if (i == j) continue;

                        string luaLine2 = luaLines[j];
                        int indexOfEqual2 = luaLine2.IndexOf("=");
                        bool isVarSet2 = luaLine2.Contains("=") && luaLine2.Count(x => x == '=') == 1 &&
                                        luaLine2.Any(charr => luaLine2.IndexOf(charr) < indexOfEqual2);

                        if (isVarSet2 && luaLine2 == luaLine) /*other delcaration found*/
                        {
                            if (!luaLine2.IsInMethodScope(luaLines, j))
                                return true;
                        }
                    }

                    return false;
                };

                if (isVarSet && !alreadyDeclaredGlobally())
                {
                    variableNames.Add(luaLine.GetVariableName());
                    variableDeclarationLines.Add(luaLine);
                }
            }
        }

        enum FunctionAttributes
        {
            ScreenUpdate,
            Debug,
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
                    "function onErrorHandle" + randFunctionSeed + "(err)",
                    "print( \"[Debug Error]:\", err )",
                    "end"
                };
                errorHandleFunc.Reverse();

                foreach (string s1 in errorHandleFunc)
                {
                    luaLinesTemplate.Insert(functionLineIndex - 1, s1);
                }
                functionLineIndex += errorHandleFunc.Count;

                luaLinesTemplate.Insert(functionLineIndex + 1, $"xpcall( {detourFunctionName}, onErrorHandle{randFunctionSeed} )");

                /*remove debug attribute*/
                luaLinesTemplate.RemoveAt(functionLineIndex - 1);
            }

            return luaLinesTemplate;
        }

        public static CodeAnalysisInfo RunCodeAnalysis()
        {
            var lines = GetLuaLines();
            lines = lines.ReplaceEvents().Select(x => x.Replace("\t", "")).ToList();
            List<string> funcAttributeErros = ConvertFunctionAttributes(ref lines);
            List<string> fieldAttributeErros = ConvertFieldAttributes(ref lines);

            List<string> declIssues = CheckDeclarationIssues(lines);
            List<string> conversionIssues = CheckConversionIssues(lines);
            declIssues.AddRange(conversionIssues);
            declIssues.AddRange(funcAttributeErros);
            declIssues.AddRange(fieldAttributeErros);

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
                    DynValue res = Script.RunFile(debugPath);
                }
                catch (Exception _errorMessage)
                {
                    errorMessage = "[LUA] " + _errorMessage.Message;
                }

                declIssues.Add(errorMessage != "" ? "\n" + errorMessage : "\n[LUA] Code fine");
                try { File.Delete(debugPath); } catch {/* ignored*/}
            }

            return new CodeAnalysisInfo
            {
                Result = declIssues.Count <= 1 /*Code fine string*/ ?
                    CodeAnalysisResult.CodeFine : CodeAnalysisResult.Warnings,
                Announcements = declIssues
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
