using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SessionDetails : MonoBehaviour {

	public GameObject[] enableOnStart;
	public GameObject[] disableOnConfirm;
	public GameObject[] enableOnConfirm;

	public Transform parentItems;	
	public ItemSessionDetailsUI prefabItemInfo;
	public List<ItemSessionDetails> itemLabels;
	

	public Dictionary<string, ItemSessionDetailsUI> items = new Dictionary<string, ItemSessionDetailsUI>();
	void Start(){

		foreach(ItemSessionDetails item in itemLabels){
			GameObject g = Instantiate(prefabItemInfo.gameObject);
			ItemSessionDetailsUI i = g.GetComponent<ItemSessionDetailsUI>();
			items.Add(item.label, i);
			i.item = item;
			i.transform.SetParent(parentItems, false);
			i.gameObject.SetActive(true);
		}

		foreach(GameObject g in enableOnStart)
			g.SetActive(true);
	}
	
	public void Confirm(){
		foreach(GameObject g in disableOnConfirm)
			g.SetActive(false);
		
		foreach (GameObject g in enableOnConfirm)
			g.SetActive(true);
	}

	public string GetItemValue(string key){
		if(items.ContainsKey(key))
			return items[key].item.value;
		return null;
	}

	public void SetItemValue(string key, string value){
		if(items.ContainsKey(key))
			items[key].item.value = value;
	}
}


[System.Serializable]
public class ItemSessionDetails{
	public string label;
	public string value;
}

