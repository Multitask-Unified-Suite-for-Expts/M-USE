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
