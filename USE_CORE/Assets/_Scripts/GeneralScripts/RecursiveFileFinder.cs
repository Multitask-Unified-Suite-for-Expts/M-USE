using System.Collections.Generic;
using System;
using System.IO;
using System.Linq;
using UnityEngine;

public class RecursiveFileFinder
{
    private static Dictionary<(string path, string filename), List<string>> Cache;
    private static HashSet<string> CachedPaths;

    public static List<string> FindFile(string path, string filename, string extension)
    {
        if (Cache == null)
        {
            Cache = new Dictionary<(string, string), List<string>>();
            CachedPaths = new HashSet<string>();
        }

        if (CachedPaths.Contains(path))
        {
            if (Cache.TryGetValue((path, filename), out List<string> filenames))
            {
                return filenames;
            }
            return new List<string>();
        }
        CachedPaths.Add(path);

        Cache.Add((path, filename), new List<string>());
        PopulateCache(path, extension);

        return Cache[(path, filename)];
    }

    private static void PopulateCache(string path, string extension)
    {
        List<string> allFilenames = Directory.GetFiles(path, $"*{extension}", SearchOption.AllDirectories).ToList();
        allFilenames.RemoveAll(t => Path.GetFileName(t).StartsWith("."));

        foreach (string filePath in allFilenames)
        {
            string filename = Path.GetFileName(filePath);
            if (Cache.TryGetValue((path, filename), out List<string> filenames))
            {
                filenames.Add(filePath);
            }
            else
            {
                Cache.Add((path, filename), new List<string>() { filePath });
            }

        }
    }
}
