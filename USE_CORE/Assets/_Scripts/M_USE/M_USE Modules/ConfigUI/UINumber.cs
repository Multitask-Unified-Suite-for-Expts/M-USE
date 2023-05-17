using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ConfigDynamicUI
{

	public class UINumber : MonoBehaviour
	{

		public Text label;
		public InputField input;
		
		ConfigNumber number;
		
		public void setConfigVar(ConfigNumber number)
		{
			this.number = number;
			this.number.onValueChanged += () => {
				this.input.text = this.number.value + "";
			};
		}

		private void Awake()
		{
			this.input.onValueChanged.AddListener(valueChanged);	
		}

		// Use this for initialization
		void Start()
		{
			if (this.number != null)
			{
				this.label.text = this.number.name;
				this.input.text = this.number.value + "";
				if (this.number.precision == 0)
					this.input.contentType = InputField.ContentType.IntegerNumber;
				else
					this.input.contentType = InputField.ContentType.DecimalNumber;
			}
		}
		
		void valueChanged(string value){
			if(this.number != null){
				float v;
				float.TryParse(value, out v);
				this.number.SetValue(v);
			}
		}
	}
}