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
using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Reflection;
using System.ComponentModel;
using UnityEngine.SceneManagement;
using ConfigParsing;

public class ConfigReader : MonoBehaviour{
	//public static ConfigReader instance;  

	private static Dictionary<string,Settings> allConfigs = new Dictionary<string, Settings>();

	public static Settings Get(string target){
		if (!allConfigs.ContainsKey(target)){
			Debug.Log("config not found: " + target);
			return new Settings();
		}else{
			return allConfigs[target];
		}
	}

	public static void ReadConfig(string key, string fullPath){
		Settings tmp = ConfigParsing.ConfigParsing.parseConfigFile(fullPath);
		allConfigs.Add(key,tmp);
		Debug.Log("read in"+key + ", from: "+fullPath);
	}

	public static void ListAllConfigs(){
		foreach (KeyValuePair<string,Settings> kv in allConfigs){
			Debug.Log(kv.Key + ", warning: " + kv.Value.warning);
		}
	}
}
