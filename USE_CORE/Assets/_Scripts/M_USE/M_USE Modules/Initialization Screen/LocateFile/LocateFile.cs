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
using UnityEngine;
using System.IO;


public class LocateFile : MonoBehaviour
{
	public Dictionary<string, FileSpec> mapFiles = new Dictionary<string, FileSpec>();

	public void AddToFilesDict(FileSpec file)
	{
		mapFiles.Add(file.name, file);
	}	
	public string GetPath(string keyToFile)
	{
		if (mapFiles.ContainsKey(keyToFile))
			return mapFiles[keyToFile].path;
		else
			Debug.LogError("DID NOT FIND A PATH FOR FILE KEY: " + keyToFile);
		return null;
	}
	public string FindFilePathInExternalFolder(string keyToFolder, string stringPattern)
	{
		string[] possibleFiles = Directory.GetFiles(keyToFolder, stringPattern);

		if (possibleFiles.Length == 1)
			return possibleFiles[0];
		else if (possibleFiles.Length == 0)
		{
			Debug.LogWarning("No file following pattern " + stringPattern + " is found at path " + keyToFolder + ".");
			return "";
		}
		else
		{
			Debug.LogWarning(possibleFiles.Length + " Files found following pattern " + stringPattern + " at path " + keyToFolder + " | Using the first file found.");
			return possibleFiles[0];
		}
	}
}

[System.Serializable]
public class FileSpec
{
	public string name;
	public bool isFolder = false;
	public string path = "";

}
