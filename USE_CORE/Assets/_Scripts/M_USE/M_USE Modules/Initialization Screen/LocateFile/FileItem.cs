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




using TMPro;
using UnityEngine;
using UnityEngine.UI;
#if (!UNITY_WEBGL)
	using SFB;
#endif


public class FileItem_TMP : MonoBehaviour
{
    public FileSpec File;
    public TMP_InputField FilePath_InputField;
    public TextMeshProUGUI Text;


    public void ManualStart(FileSpec file, TMP_InputField inputField, TextMeshProUGUI text)  
    {
        File = file;
        FilePath_InputField = inputField;
        Text = text;

        if (File != null)
        {
            Text.text = PlayerPrefs.GetString("filepath-" + File.name, "");
            Text.text = Text.text.Replace("file://", "");
            Text.text = Text.text.Replace("%20", " ");
            File.path = Text.text;
        }

        FilePath_InputField.onEndEdit.AddListener((text) => {
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
        PlayerPrefs.SetString("filepath-" + File.name, path);
        File.path = path;
    }
}
