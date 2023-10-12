/*
MIT License

Copyright (c) 2023 Multitask - Unified - Suite -for-Expts

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files(the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and / or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
*/


using System.Collections.Generic;
using System;
using System.IO;
using System.Linq;
using UnityEngine;

public class RecursiveFileFinder
{
    private static Dictionary<(string path, string filename), List<string>> Cache;
    private static HashSet<string> CachedPaths;
    private static HashSet<string> CachedExtensions; //ADDED THIS!

    public static List<string> FindFile(string folderPath, string filename, string extension)
    {
        if (Cache == null)
        {
            Cache = new Dictionary<(string, string), List<string>>();
            CachedPaths = new HashSet<string>();
            CachedExtensions = new HashSet<string>();
        }

        if (CachedPaths.Contains(folderPath) && CachedExtensions.Contains(extension)) //ADDED 2nd HALF OF THIS!
        {
            if (Cache.TryGetValue((folderPath, filename), out List<string> filenames))
            {
                return filenames;
            }
            return new List<string>();
        }
        CachedPaths.Add(folderPath);

        CachedExtensions.Add(extension); //ADDED THIS!

        Cache.Add((folderPath, filename), new List<string>());
        PopulateCache(folderPath, extension);

        return Cache[(folderPath, filename)];
    }

    private static void PopulateCache(string folderPath, string extension)
    {
        List<string> allFilenames = Directory.GetFiles(folderPath, $"*{extension}", SearchOption.AllDirectories).ToList();
        allFilenames.RemoveAll(t => Path.GetFileName(t).StartsWith("."));

        foreach (string filePath in allFilenames)
        {
            string filename = Path.GetFileName(filePath);
            if (Cache.TryGetValue((folderPath, filename), out List<string> filenames))
            {
                filenames.Add(filePath);
            }
            else
            {
                Cache.Add((folderPath, filename), new List<string>() { filePath });
            }

        }
    }
}
