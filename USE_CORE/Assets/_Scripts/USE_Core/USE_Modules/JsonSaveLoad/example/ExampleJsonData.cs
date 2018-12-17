using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class ExampleJsonData
{

	public string text_a = "A";
	public float float_b = 2.5f;
	public int int_c = 3;

	public int[] array = { 2, 1, 3 };

	[System.NonSerialized]
	public float ignoreMe = 2;
}
