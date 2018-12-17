using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ItemSessionDetailsUI : MonoBehaviour {

	public InputField input;
	public Text label;

	[HideInInspector]
	public ItemSessionDetails item;

	void Start(){
		this.label.text = item.label;
		this.input.text = item.value;

		item.value = input.text =  PlayerPrefs.GetString("exp-info-" + item.label, item.value);
		this.input.onEndEdit.AddListener((text)=>{
			PlayerPrefs.SetString("exp-info-" + item.label, text);
			item.value = text;
		});
	}
}
