using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ConfigDynamicUI
{

	public class UIBoolean : MonoBehaviour
	{
		public Toggle toggle;
		public Text label;
		
		ConfigBoolean boolean;

		public void setConfigVar(ConfigBoolean boolean)
		{
			this.boolean = boolean;
			this.boolean.onValueChanged += () => {
				this.toggle.isOn = this.boolean.value;
			};
		}

		private void Awake()
		{
			this.toggle.onValueChanged.AddListener(valueChanged);
		}

		// Use this for initialization
		void Start()
		{
			if (this.boolean != null)
			{
				this.label.text = this.boolean.name;
				this.toggle.isOn = this.boolean.value;
			}
		}

		void valueChanged(bool value)
		{
			if (this.boolean != null)
			{
				this.boolean.SetValue(value);
			}
		}
	}
}