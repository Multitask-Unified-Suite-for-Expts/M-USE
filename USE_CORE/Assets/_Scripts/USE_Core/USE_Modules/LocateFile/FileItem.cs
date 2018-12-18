using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using SFB;

public class FileItem : MonoBehaviour {

	// [HideInInspector]
	public FileSpec file;

	public Text label;
	public InputField inputFilePath;


	void Start(){
		if (file != null){
			label.text = file.name;
			inputFilePath.text =  PlayerPrefs.GetString("filepath-" + file.name, "");
			inputFilePath.text = inputFilePath.text.Replace("file://", "");
			inputFilePath.text = inputFilePath.text.Replace("%20", " ");
			this.file.path = inputFilePath.text;
		}
		this.inputFilePath.onEndEdit.AddListener((text)=>{
			UpdatePath(text);
		});
	}


	public void locate(){
		if(!file.isFolder)
			StandaloneFileBrowser.OpenFilePanelAsync("Open File", inputFilePath.text, "", false, (string[] paths) => { OnFileOpened(paths); });
		else
			StandaloneFileBrowser.OpenFolderPanelAsync("Open File", inputFilePath.text, false, (string[] paths) => { OnFileOpened(paths); });
	}

	void OnFileOpened(string[] paths){
		if(paths.Length > 0 && paths[0] != ""){
			var path = paths[0];
			path = path.Replace("file://", "");
			path = path.Replace("%20", " ");
			this.inputFilePath.text = path;
			UpdatePath(path);
		}
	}

	void UpdatePath(string path){
		Debug.Log("updated path of file:" + file.name + " to:" + path);
		PlayerPrefs.SetString("filepath-" + file.name, path);
		this.file.path = path;
	}
}