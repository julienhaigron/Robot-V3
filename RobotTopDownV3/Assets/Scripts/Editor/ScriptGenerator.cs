using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;


public static class ScriptGenerator
{
    private static string m_generatedPath = Application.dataPath + "/Generated/";

    public static void SetContent ( string file, string tag, string contentToAdd )
    {
        SetContent(file, tag, contentToAdd.Split("\n"));
    }

    public static void SetContent ( string file, string tag, string[] linesToAdd )
    {
        string filePath = GetFilePath(file);
        string startTag = FormatTag(tag, false);
        string endTag = FormatTag(tag, true);

        List<string> content = new List<string>();
        IEnumerable<string> lines = File.ReadLines(filePath);

        bool encounteredStartTag = false;
        bool encounteredEndTag = false;
        int lineIndex = 0;

        foreach (string iterationLine in lines)
        {
            string line = iterationLine;
            if (lineIndex == 0)
            {

                Regex rgx = new Regex("//GENERATED [0 - 9]*");
                if (!rgx.Match(line).Success)
                {
                    content.Add("//GENERATED 1");
                }
                else
                {
                    line = "//GENERATED " + (int.Parse((line.Substring(line.IndexOf(" ")))) + 1);
                }
            }
            lineIndex++;
            if (!encounteredStartTag || encounteredEndTag)
            {
                content.Add(line);
            }
            if (!encounteredStartTag)
            {
                if (line.Contains(startTag))
                {
                    content.AddRange(linesToAdd);
                    encounteredStartTag = true;
                }
            }
            else if (!encounteredEndTag && line.Contains(endTag))
            {
                content.Add(line);
                encounteredEndTag = true;
            }
        }
        if (encounteredEndTag)
        {
            File.WriteAllLines(filePath, content.ToArray());
        }
        AssetDatabase.Refresh();
    }
    public static void AppendContent ( string file, string tag, string contentToAdd, bool preventDuplicate )
    {
        AppendContent(file, tag, contentToAdd.Split("\n"), preventDuplicate);
    }

    public static void AppendContent ( string file, string tag, string[] linesToAdd, bool preventDuplicate )
    {
        string filePath = GetFilePath(file);
        string startTag = FormatTag(tag, false);
        string endTag = FormatTag(tag, true);

        List<string> content = new List<string>();
        IEnumerable<string> lines = File.ReadLines(filePath);

        bool encounteredStartTag = false;
        bool encounteredEndTag = false;

        List<string> contentAlreadyAdded = new List<string>();
        int lineIndex = 0;

        foreach (string iterationLine in lines)
        {
            string line = iterationLine;
            if (lineIndex == 0)
            {

                Regex rgx = new Regex("//GENERATED [0-9]+");
                if (!rgx.Match(line).Success)
                {
                    content.Add("//GENERATED 1");
                }
                else
                {
                    line = "//GENERATED " + (int.Parse((line.Substring(line.IndexOf(" ")))) + 1);
                }
            }
            lineIndex++;
            if (!encounteredStartTag)
            {
                content.Add(line);
                if (line.Contains(startTag))
                {
                    encounteredStartTag = true;
                }
            }
            else if (!encounteredEndTag)
            {
                if (line.Contains(endTag))
                {
                    if (preventDuplicate)
                    {
                        List<string> linesToAddNoDuplicate = new List<string>(linesToAdd);
                        foreach (string alreadyAddedLine in contentAlreadyAdded)
                        {
                            linesToAddNoDuplicate.Remove(alreadyAddedLine);
                        }
                        content.AddRange(linesToAddNoDuplicate);
                    }
                    else
                    {
                        content.AddRange(linesToAdd);
                    }
                    encounteredEndTag = true;
                    content.Add(line);
                }
                else
                {
                    content.Add(line);
                    if (preventDuplicate)
                    {
                        contentAlreadyAdded.Add(line);
                    }
                }
            }
            else
            {
                content.Add(line);
            }
        }
        if (encounteredEndTag)
        {
            File.WriteAllLines(filePath, content.ToArray());
        }
        AssetDatabase.Refresh();
    }


    private static string GetFilePath ( string file )
    {
        if (!file.StartsWith(m_generatedPath))
        {
            file = m_generatedPath + file; ;
        }
        return file;
    }

    //#START#XXXX#//
    //#END#XXXX#//
    private static string FormatTag ( string tag, bool isEnd )
    {
        tag = tag.ToUpper();
        string tagStart = "//#" + (isEnd ? "END" : "START") + "#";
        string tagEnd = "#//";
        if (!tag.Contains(tagStart))
        {
            tag = tagStart + tag;
        }
        if (!tag.EndsWith(tagEnd))
        {
            tag += tagEnd;
        }
        return tag;
    }
}