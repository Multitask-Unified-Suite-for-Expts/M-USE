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
		
		void valueChanged(string value)
		{
			if(this.number != null){
				float v;
				float.TryParse(value, out v);
				this.number.SetValue(v);
			}
		}
	}
}