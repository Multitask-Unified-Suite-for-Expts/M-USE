using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using ConfigDynamicUI;

public class UIRange2Int : MonoBehaviour {


	public Text label;
	public InputField mininput;
	public InputField maxinput;
	public Slider minslider;
	public Slider maxslider;

	ConfigNumberRangedInt number;

	public void setConfigVar(ConfigNumberRangedInt number)
	{
		this.number = number;
		this.number.onValueChanged += () => {
			this.mininput.text = this.number.minvalue + "";
			this.maxinput.text = this.number.maxvalue + "";
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
			this.mininput.onEndEdit.AddListener(valueChangedInputFieldMin);
			this.minslider.onValueChanged.AddListener(valueChangedSliderMin);
			this.maxinput.onEndEdit.AddListener(valueChangedInputFieldMax);
			this.maxslider.onValueChanged.AddListener(valueChangedSliderMax);
		}
	}

	public void updateUI(){
		this.label.text = this.number.name;
		this.mininput.text = this.number.minvalue + "";
		this.maxinput.text = this.number.maxvalue + "";
		this.mininput.contentType = InputField.ContentType.IntegerNumber;
		this.maxinput.contentType = InputField.ContentType.IntegerNumber;

		minslider.minValue = number.Absmin;
		minslider.maxValue = number.Absmax;
		minslider.value = number.minvalue;

		maxslider.minValue = number.Absmin;
		maxslider.maxValue = number.Absmax;
		maxslider.value = number.maxvalue;
	}

	float checkValueBoundary(float v){
		if (v < this.number.Absmin)
			v = this.number.Absmin;
		if (v > this.number.Absmax)
			v = this.number.Absmax;
		string fs = v.ToString("F" + this.number.precision);
		float fv;
		float.TryParse(fs, out fv);
		return fv;
	}

	void valueChangedInputFieldMin(string value)
	{
		float v;
		float.TryParse(value, out v);
		v = checkValueBoundary(v);


		if (this.number != null)
		{
			this.number.minvalue = Mathf.RoundToInt(v);
		}

		if (v > this.maxslider.value) {
			v = this.maxslider.value; 
		}

		this.minslider.value = v;
		this.mininput.text = "" + v;
	}


	void valueChangedInputFieldMax(string value)
	{
		float v;
		float.TryParse(value, out v);
		v = checkValueBoundary(v);


		if (this.number != null)
		{
			this.number.maxvalue = Mathf.RoundToInt(v);
		}

		if (v < this.minslider.value) {
			v = this.minslider.value; 
		}

		this.maxslider.value = v;
		this.maxinput.text = "" + v;
	}


	void valueChangedSliderMin(float value)
	{
		value = checkValueBoundary(value);

		if(this.number != null){
			this.number.minvalue = Mathf.RoundToInt(value);
		}


		if (value > this.maxslider.value) {
			value = this.maxslider.value; 
		}

		this.minslider.value = value;
		this.mininput.text = "" + value;
	}

	void valueChangedSliderMax(float value)
	{
		value = checkValueBoundary(value);

		if(this.number != null){
			this.number.maxvalue = Mathf.RoundToInt(value);
		}


		if (value < this.minslider.value) {
			value = this.minslider.value; 
		}

		this.maxslider.value = value;
		this.maxinput.text = "" + value;
	}
}
