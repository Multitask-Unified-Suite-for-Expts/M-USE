using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ConfigDynamicUI
{

	/*
	 * Backlog:
	 *  - Having multiple columns of config UI would be great 
	 *  - Support for settings for font size for all config texts wbg
	 *  - If possible, also suppport for height of config UIs wbg
	 */
	public class ConfigUI : MonoBehaviour
	{
		[HideInInspector]
		public ConfigVarStore store;
		public Transform varUIContainer;

		public List<GameObject> listGeneratedObjects = new List<GameObject>();
		
		
		// UI - prefabs
		public GameObject prefabNumberInput;
		public GameObject prefabSlider;
		public GameObject prefabSlider2;
		public GameObject prefabSlider3;
		public GameObject prefabBoolean;

		public void clear(){
			this.store.clear();
			foreach (GameObject g in this.listGeneratedObjects)
			{
				Destroy(g);
			}
			listGeneratedObjects.Clear();
		}
		

		public void GenerateUI()
		{
			foreach (var v in store.getAllVariables())
			{
				GameObject g = null;
				if (v.hidden)
					continue;
				if (v is ConfigNumber) {
					//Debug.Log("Generating config ui for " + v.name + " is of type: " + v.GetType());
					var f = (ConfigNumber)v;
					if (f.isRange) {
						g = GenerateSlider (f);
					} else {
						g = GenerateNumberInput (f);
					}
				}else if (v is ConfigNumberRanged){
					var f = (ConfigNumberRanged)v;
					g = GenerateSlider2 (f);
				}else if (v is ConfigNumberRangedInt){
					var f = (ConfigNumberRangedInt)v;
					g = GenerateSlider3 (f);
				}else if(v is ConfigBoolean){
					var b = (ConfigBoolean)v;
					g = GenerateBoolean(b);
				}
				if(g != null)
					listGeneratedObjects.Add(g);
			}
		}
		
		public GameObject GenerateBoolean(ConfigBoolean b)
		{
			GameObject n = Instantiate(prefabBoolean);
			n.transform.SetParent(this.varUIContainer, false);
			UIBoolean ui = n.GetComponent<UIBoolean>();
			ui.setConfigVar(b);
			n.SetActive(true);
			return n;
		}
		
		public GameObject GenerateSlider(ConfigNumber f){
			GameObject n = Instantiate(prefabSlider);
			n.transform.SetParent(this.varUIContainer, false);
			UIRange ui = n.GetComponent<UIRange>();
			ui.setConfigVar(f);
			n.SetActive(true);
			return n;
		}

	
		public GameObject GenerateSlider2(ConfigNumberRanged f){
			GameObject n = Instantiate(prefabSlider2);
			n.transform.SetParent(this.varUIContainer, false);
			UIRange2 ui = n.GetComponent<UIRange2>();
			ui.setConfigVar(f);
			n.SetActive(true);
			return n;
		}
			
		public GameObject GenerateSlider3(ConfigNumberRangedInt f){
			GameObject n = Instantiate(prefabSlider3);
			n.transform.SetParent(this.varUIContainer, false);
			UIRange2Int ui = n.GetComponent<UIRange2Int>();
			ui.setConfigVar(f);
			n.SetActive(true);
			return n;
		}

		public GameObject GenerateNumberInput(ConfigNumber f){
			GameObject n = Instantiate(prefabNumberInput);
			n.transform.SetParent(this.varUIContainer, false);
			UINumber ui = n.GetComponent<UINumber>();
			ui.setConfigVar(f);
			n.SetActive(true);
			return n;
		}
		
		// Helper functions to create, get/set values of specific types


		public void SelectRandomValueForAllNumbers(){
			foreach (var f in store.varsNumberRanged.Values)
			{
				f.SetRandomValue ();
			}
			foreach (var f in store.varsNumberRangedInt.Values)
			{
				f.SetRandomValue ();
			}
		}
		
		public float GetFloat(string variableName){
			return (float) this.store.get<ConfigNumber>(variableName).value;
			//			return (float) this.store.get<ConfigNumber>(variableName).SetRandomValue();
		}
		public int GetInt(string variableName)
		{
			return (int) this.store.get<ConfigNumber>(variableName).value;
		}

		public void SetRandomValueMono(string variableName){
			this.store.get<ConfigNumber>(variableName).SetRandomValue();
		}

		public float GetFloatRanged(string variableName){
			return (float) this.store.get<ConfigNumberRanged>(variableName).value;
			//			return (float) this.store.get<ConfigNumber>(variableName).SetRandomValue();
		}

		public int GetIntRanged(string variableName)
		{
			return (int) this.store.get<ConfigNumberRangedInt>(variableName).value;
		}
			
		public bool GetBool(string variableName)
		{
			return this.store.get<ConfigBoolean>(variableName).value;
		}

		public string GetString(string variableName)
		{
			return this.store.get<ConfigString>(variableName).value;
		}
			
		public ConfigNumber CreateNumber(string name, int value = 0){
			ConfigNumber n = new ConfigNumber(name, value);
			this.store.putVar(n);
			return n;
		}
		
		public ConfigNumber CreateNumber(string name, float value = 0)
		{
			ConfigNumber n = new ConfigNumber(name, value).SetPrecision(2);
			this.store.putVar(n);
			return n;
		}
			
		public ConfigNumberRanged CreateNumberRanged(string name, float minvalue = 0, float maxvalue = 0)
		{
			ConfigNumberRanged n = new ConfigNumberRanged(name, minvalue, maxvalue).SetPrecision(2);
			this.store.putVar(n);
			return n;
		}

		public ConfigNumberRangedInt CreateNumberRangedInt(string name, int minvalue = 0, int maxvalue = 0){
			ConfigNumberRangedInt n = new ConfigNumberRangedInt(name, minvalue, maxvalue);
			this.store.putVar(n);
			return n;
		}
			
		public ConfigString CreateString(string name, string value = "")
		{
			ConfigString v = new ConfigString(name, value);
			this.store.putVar(v);
			return v;
		}
		
		public ConfigBoolean CreateBoolean(string name, bool value = false)
		{
			ConfigBoolean v = new ConfigBoolean(name, value);
			this.store.putVar(v);
			return v;
		}
	}

}