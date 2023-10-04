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

using UnityEngine;
using System.Collections.Generic;
using TMPro;
using System;

public class FolderDropdown : MonoBehaviour
{
    public TMP_Dropdown dropdown;

    private void Start()
    {
        dropdown = GetComponent<TMP_Dropdown>();
        if (dropdown == null)
            Debug.LogWarning("DROPDOWN IS NULL!");
    }

    public void SetFolders(List<string> sessionConfigFolders)
    {
        if(sessionConfigFolders == null || sessionConfigFolders.Count < 1)
        {
            Debug.LogWarning("TRYING TO SET FOLDERS BUT THE SessionConfigFolders PARAMETER IS NULL OR EMPTY!");
            return;
        }

        List<TMP_Dropdown.OptionData> options = new List<TMP_Dropdown.OptionData>();

        foreach(string folder in sessionConfigFolders)
        {
            TMP_Dropdown.OptionData option = new TMP_Dropdown.OptionData(folder);
            options.Add(option);
        }
        dropdown.ClearOptions();
        dropdown.AddOptions(options);
    }
}