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



using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SessionDetails : MonoBehaviour
{
	public Transform parentItems;	
	public ItemSessionDetailsUI prefabItemInfo;
	public List<ItemSessionDetails> itemLabels;
	
	public Dictionary<string, ItemSessionDetailsUI> items = new Dictionary<string, ItemSessionDetailsUI>();

	void Start()
	{
		foreach(ItemSessionDetails item in itemLabels)
		{
			GameObject g = Instantiate(prefabItemInfo.gameObject);
			ItemSessionDetailsUI i = g.GetComponent<ItemSessionDetailsUI>();
			items.Add(item.label, i);
			i.item = item;
			i.transform.SetParent(parentItems, false);
			i.gameObject.SetActive(true);
		}
	}
	
	public string GetItemValue(string key)
	{
		if(items.ContainsKey(key))
			return items[key].item.value;
		return null;
	}

	public void SetItemValue(string key, string value)
	{
		if(items.ContainsKey(key))
			items[key].item.value = value;
	}
}


[System.Serializable]
public class ItemSessionDetails
{
	public string label;
	public string value;
}

