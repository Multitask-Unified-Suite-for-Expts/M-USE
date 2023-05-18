using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ConfigDynamicUI;

public class TestConfigUI : MonoBehaviour {
	public ConfigUI configUI;
    public JsonSaveLoad jsonSaveLoad;

	// Use this for initialization
	void Start () {
		configUI.clear();

        jsonSaveLoad.folderPath = "./";
        jsonSaveLoad.isRelative = false;

        var configStore = jsonSaveLoad.LoadObject<ConfigVarStore>("config", false);
        configUI.store = configStore;

        configUI.GenerateUI();
	}

	public void click(){
		Debug.Log("clicked");
	}
	
	// Update is called once per frame
	void Update () {
		
	}
}
