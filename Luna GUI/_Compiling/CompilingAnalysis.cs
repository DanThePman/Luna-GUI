﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using MahApps.Metro.Controls.Dialogs;
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

        private static int SearchFunctionEnd(List<string> luaLines, int firstFunctionIndex)
        {
            if (firstFunctionIndex == -1) //not in a function
                return -1;

            List<string> specialLoops = new List<string>
            {
                "if",
                "for",
                "while"
            };
            int specialLoopsCount = 0;
            for (int i = firstFunctionIndex + 1; i < luaLines.Count; i++)
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

                string num_str = luaLine.Substring(sp, ep - sp).Replace("\"", "");

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

        static List<string> CheckDeclarationIssues(this List<string> luaLines)
        {
            List<string> issues = new List<string>();

            List<string> variableNames = new List<string>();
            List<string> variableDeclarationLines = luaLines.Where(currentLine =>
            {
                int indexOfEqual = currentLine.IndexOf("=");
                bool isVarSet = currentLine.Contains("=") && currentLine.Count(x => x == '=') == 1 &&
                    currentLine.Any(charr => currentLine.IndexOf(charr) < indexOfEqual);

                if (isVarSet)
                {
                    variableNames.Add(currentLine.GetVariableName());
                }

                return isVarSet;
            }).ToList();

            Dictionary<string, string> varNames = new Dictionary<string, string>();
            foreach (string variableUseLine in luaLines.Where(luaLine =>
            {
                string varName = variableNames.Where(var =>
                {
                    if (!luaLine.Contains(var))
                        return false;

                    char oneIndexLowerThanVarStartIndex = luaLine.IndexOf(var) != 0 ? luaLine[luaLine.IndexOf(var) - 1] : '(';

                    /*varname not randomly in unknown string included*/
                    bool varNameNotInText = Regex.Matches(oneIndexLowerThanVarStartIndex.ToString(), @"[a-zA-Z]").Count == 0 &&
                        Regex.Matches(luaLine[luaLine.IndexOf(var) + var.Length].ToString(), @"[a-zA-Z]").Count == 0; //e.g. function hELLO()...

                    return varNameNotInText;
                }).FirstOrDefault();

                if (varName == null) return false;

                bool notDeclLineItself = variableDeclarationLines.All(x => !(x.GetVariableName() == varName && x == luaLine));

                if (notDeclLineItself)
                    varNames.Add(luaLine, varName);
                /*not the decl line itself*/
                return notDeclLineItself;
            }))
            {
                int currIndex = luaLines.FindIndex(x => x == variableUseLine);

                string varname = varNames.First(x => x.Key == variableUseLine).Value;
                string declLine = variableDeclarationLines.First(x => x.GetVariableName() == varname);
                if (declLine.IsInOtherMethodScope(luaLines, variableUseLine))
                {
                    string varName = variableNames.First(variableUseLine.Contains);
                    issues.Add("'"  + varName + "' => [CompilingAnalysis] The variable's declaration is not reachable at '" +
                        variableUseLine + "'");
                }
            }

            return issues;
        } 

        public static CodeAnalysisInfo RunCodeAnalysis()
        {
            var lines = GetLuaLines();
            lines = lines.ReplaceEvents();

            List<string> declIssues = CheckDeclarationIssues(lines);
            List<string> conversionIssues = CheckConversionIssues(lines);
            declIssues.AddRange(conversionIssues);

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

                Lua state = new Lua();
                string errorMessage = "";
                try
                {
                    state.DoFile(debugPath);
                }
                catch (Exception _errorMessage)
                {
                    int spos = _errorMessage.Message.LastIndexOf(".lua") + 4;
                    errorMessage = "[LUA] " +  _errorMessage.Message.Substring(spos, _errorMessage.Message.Length - spos);
                }

                declIssues.Add(errorMessage != "" ? "\n" + errorMessage : "\n[LUA] Code fine");
                try {File.Delete(debugPath);} catch {/* ignored*/}
            }

            return new CodeAnalysisInfo
            {
                Result = declIssues.Count <= 1 /*Code fine string*/ ? 
                    CodeAnalysisResult.CodeFine : CodeAnalysisResult.Warnings, Announcements = declIssues
            };
        }
    }
}
