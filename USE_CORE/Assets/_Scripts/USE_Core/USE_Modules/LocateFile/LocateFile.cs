using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LocateFile{

	public class LocateFile : MonoBehaviour {

		public List<FileSpec> files;
		public Dictionary<string, FileSpec> mapFiles = new Dictionary<string, FileSpec>();

		public FileItem prefabFileItem;
		public Transform containerFileItems;

		// Use this for initialization
		void Start () {
			foreach(FileSpec f in files){
				GameObject g = (GameObject) Instantiate(prefabFileItem.gameObject);
				g.transform.SetParent(containerFileItems, false);

				FileItem fi = g.GetComponent<FileItem>();
				fi.file = f;

				mapFiles.Add(f.name, f);

				g.SetActive(true);
			}
		}
		
		public string GetPath(string keyToFile){
			if (mapFiles.ContainsKey(keyToFile)){
				return mapFiles[keyToFile].path;
			}
			return null;
		}
	}

	[System.Serializable]
	public class FileSpec{
		public string name;
		public bool isFolder = false;
		public string path = "";
	}

}