/*
This software is part of the Unified Suite for Experiments (USE).
Information on USE is available at
http://accl.psy.vanderbilt.edu/resources/analysis-tools/unifiedsuiteforexperiments/

Copyright (c) <2018> <Marcus Watson>

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

1) The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.
2) If this software is used as a component of a project that leads to publication
(e.g. a paper in a scientific journal or a student thesis), the published work
will give appropriate attribution (e.g. citation) to the following paper:
Watson, M.R., Voloh, B., Thomas, C., Hasan, A., Womelsdorf, T. (2018). USE: An
integrative suite for temporally-precise psychophysical experiments in virtual
environments for human, nonhuman, and artificially intelligent agents. BioRxiv:
http://dx.doi.org/10.1101/434944

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
*/

using TMPro;
using UnityEngine;
using UnityEngine.UI;
#if (!UNITY_WEBGL)
	using SFB;
#endif


public class FileItem_TMP : MonoBehaviour
{
    public FileSpec File;
    //public TMP_InputField InputField_FilePath;
    public InputField InputField_FilePath;
    public Text Text;


    public void ManualStart(FileSpec file, InputField inputField, Text text)  
    {
        File = file;
        InputField_FilePath = inputField;
        Text = text;

        if (File != null)
        {
            Text.text = PlayerPrefs.GetString("filepath-" + File.name, "");
            Text.text = Text.text.Replace("file://", "");
            Text.text = Text.text.Replace("%20", " ");
            File.path = Text.text;
        }
        InputField_FilePath.onEndEdit.AddListener((text) => {
            UpdatePath(text);
        });
    }

    public void Locate()
    {
        #if (!UNITY_WEBGL)
		    if(!File.isFolder)
			    StandaloneFileBrowser.OpenFilePanelAsync("Open File", Text.text, "", false, (string[] paths) => { OnFileOpened(paths); });
		    else
			    StandaloneFileBrowser.OpenFolderPanelAsync("Open File", Text.text, false, (string[] paths) => { OnFileOpened(paths); });
        #endif
    }

    void OnFileOpened(string[] paths)
    {
        if (paths.Length > 0 && paths[0] != "")
        {
            var path = paths[0];
            path = path.Replace("file://", "");
            path = path.Replace("%20", " ");
            Text.text = path;
            UpdatePath(path);
        }
    }

    void UpdatePath(string path)
    {
        Debug.Log("updated path of file:" + File.name + " to:" + path);
        PlayerPrefs.SetString("filepath-" + File.name, path);
        File.path = path;
    }
}
