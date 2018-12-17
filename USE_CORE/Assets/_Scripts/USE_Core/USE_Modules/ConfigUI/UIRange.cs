using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using ConfigDynamicUI;

public class UIRange : MonoBehaviour {

	
	public Text label;
	public InputField input;
	public Slider slider;

	ConfigNumber number;

	public void setConfigVar(ConfigNumber number)
	{
		this.number = number;
		this.number.onValueChanged += () => {
			this.input.text = this.number.value + "";
			updateUI();
		};
	}

	private void Awake()
	{
	}

	// Use this for initialization
	void Start()
	{
		if(this.number != null){
			updateUI();
			this.input.onEndEdit.AddListener(valueChangedInputField);
			this.slider.onValueChanged.AddListener(valueChangedSlider);
		}
	}
	
	public void updateUI(){
		this.label.text = this.number.name;
		this.input.text = this.number.value + "";
		if (this.number.precision == 0)
			this.input.contentType = InputField.ContentType.IntegerNumber;
		else
			this.input.contentType = InputField.ContentType.DecimalNumber;

		slider.minValue = number.min;
		slider.maxValue = number.max;
		slider.value = number.value;
	}
	
	float checkValueBoundary(float v){
		if (v < this.number.min)
			v = this.number.min;
		if (v > this.number.max)
			v = this.number.max;
		string fs = v.ToString("F" + this.number.precision);
		float fv;
		float.TryParse(fs, out fv);
		return fv;
	}
		
	void valueChangedInputField(string value)
	{
		float v;
		float.TryParse(value, out v);
		v = checkValueBoundary(v);
		
		
		if (this.number != null)
		{
			this.number.SetValue(v);
		}
		
		this.slider.value = v;
		this.input.text = "" + v;
	}
	
	void valueChangedSlider(float value)
	{
		value = checkValueBoundary(value);
		
		if(this.number != null){
			this.number.SetValue(value);
		}
		
		this.slider.value = value;
		this.input.text = "" + value;
	}
	
}
