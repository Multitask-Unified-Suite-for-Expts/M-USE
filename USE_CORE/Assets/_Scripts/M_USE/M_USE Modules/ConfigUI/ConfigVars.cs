using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace ConfigDynamicUI
{

	public delegate void ValueChanged();
	
	[System.Serializable]
	public abstract class ConfigVars
	{
		public string name = null;
		// used for UI to be hidden
		public bool hidden = false;
		public event ValueChanged onValueChanged;
		public void triggerValueChanged(){
			if (onValueChanged != null)
				this.onValueChanged();
		}
	}

	[System.Serializable]
	public class ConfigNumber : ConfigVars
	{
		public float value = 0;

		public bool isRange = false;
		public float max = Mathf.Infinity;
		public float min = Mathf.NegativeInfinity;
		public int precision = 0;

		public ConfigNumber(string name, float value = 0)
		{
			this.name = name;
			this.value = value;
		}

		public ConfigNumber SetPrecision(int p){
			this.precision = p;
			this.triggerValueChanged();
			return this;
		}
		
		public ConfigNumber SetMax(float m)
		{
			this.isRange = true;
			this.max = m;
			if (this.value > max)
				this.value = max;
			this.triggerValueChanged();
			return this;
		}
		
		public ConfigNumber SetMin(float m)
		{
			this.isRange = true;
			this.min = m;
			if (this.value < min)
				this.value = min;
			this.triggerValueChanged();
			return this;
		}
		
		public ConfigNumber SetValue(float value){
			this.value = value;
			if(this.isRange){
				if (this.value < min)
					this.value = min;
				if (this.value > max)
					this.value = max;
			}
			this.triggerValueChanged();
			return this;
		}
		
		public ConfigNumber SetRandomValue(){
			if(this.isRange){
				this.SetValue(Random.Range(this.min, this.max));
			}
			return this;
		}
		
		public ConfigNumber setAsRange(){
			this.isRange = true;
			this.min = this.value;
			this.max = this.value;
			return this;
		}
		
		public ConfigNumber setHidden(bool hide){
			this.hidden = hide;
			return this;
		}


		public override string ToString(){
			return "" + this.value;
		}
	}

	[System.Serializable]
	public class ConfigNumberRanged : ConfigVars
	{

		public float minvalue = 0;
		public float maxvalue = 0;
		public float value = 0; //selected value that is read the same as ConfigNumber above

		public float Absmin = Mathf.NegativeInfinity;
		public float Absmax = Mathf.Infinity;

		public int precision = 0;
		public bool isRange = true;


		public ConfigNumberRanged(string name, float minvalue = 0, float maxvalue = 0)
		{
			this.name = name;
			this.minvalue = minvalue;
			this.maxvalue = maxvalue;
			this.Absmin = this.minvalue;
			this.Absmax = this.maxvalue;
			SetRandomValue();
		}

		public ConfigNumberRanged SetPrecision(int p){
			this.precision = p;
			this.triggerValueChanged();
			return this;
		}

		public ConfigNumberRanged SetAbsMax(float m)
		{
			this.Absmax = m;
			return this;
		}

		public ConfigNumberRanged SetAbsMin(float m)
		{
			this.Absmin = m;
			return this;
		}

		public ConfigNumberRanged SetMinValue(float newvalue){
			this.minvalue = newvalue;
			if (this.minvalue < Absmin) {
				this.minvalue = Absmin;
			}
			this.triggerValueChanged();
			return this;
		}

		public ConfigNumberRanged SetMaxValue(float newvalue){
			this.maxvalue = newvalue;
			if (this.maxvalue > Absmax) {
				this.maxvalue = Absmax;
			}
			this.triggerValueChanged();
			return this;
		}

		public ConfigNumberRanged SetRandomValue(){
			this.value = Random.Range (this.minvalue, this.maxvalue);
			return this;
		}

		public ConfigNumberRanged setAsRange(){
			this.isRange = true;
			return this;
		}

		public ConfigNumberRanged setHidden(bool hide){
			this.hidden = hide;
			return this;
		}

		public override string ToString(){
			return "" + this.value;
		}
	}

	[System.Serializable]
	public class ConfigNumberRangedInt : ConfigVars
	{

		public int minvalue = 0;
		public int maxvalue = 0;
		public int value = 0; //selected value that is read the same as ConfigNumber above

		public int Absmin = 0;
		public int Absmax = 0;
		public bool isRange = true;
		public int precision = 0;

		public ConfigNumberRangedInt(string name, int minvalue = 0, int maxvalue = 0)
		{
			this.name = name;
			this.minvalue = minvalue;
			this.maxvalue = maxvalue;

			this.Absmin = this.minvalue;
			this.Absmax = this.maxvalue;


			SetRandomValue();
		}

		public ConfigNumberRangedInt SetPrecision(int p){
			this.precision = p;
			this.triggerValueChanged();
			return this;
		}

		public ConfigNumberRangedInt SetAbsMax(int m)
		{
			this.Absmax = m;
			return this;
		}

		public ConfigNumberRangedInt SetAbsMin(int m)
		{
			this.Absmin = m;
			return this;
		}

		public ConfigNumberRangedInt SetMinValue(int newvalue){
			this.minvalue = newvalue;
			if (this.minvalue < Absmin) {
				this.minvalue = Absmin;
			}
			this.triggerValueChanged();
			return this;
		}

		public ConfigNumberRangedInt SetMaxValue(int newvalue){
			this.maxvalue = newvalue;
			if (this.maxvalue > Absmax) {
				this.maxvalue = Absmax;
			}
			this.triggerValueChanged();
			return this;
		}

		public ConfigNumberRangedInt SetRandomValue(){
			this.value = Random.Range (this.minvalue, this.maxvalue+1);//add +1 to end since Random.Range is not-inclusive for integers
			return this;
		}

		public ConfigNumberRangedInt setAsRange(){
			this.isRange = true;
			return this;
		}

		public ConfigNumberRangedInt setHidden(bool hide){
			this.hidden = hide;
			return this;
		}

		public override string ToString(){
			return "" + this.value;
		}
	}

	[System.Serializable]
	public class ConfigBoolean : ConfigVars
	{
		public bool value;
		
		public ConfigBoolean()
		{

		}
		public ConfigBoolean(string name, bool value = false)
		{
			this.name = name;
			this.value = value;
		}
		
		public ConfigBoolean SetValue(bool value)
		{
			this.value = value;			
			this.triggerValueChanged();
			return this;
		}
		
		public ConfigBoolean setHidden(bool hide)
		{
			this.hidden = hide;
			return this;
		}

		public override string ToString(){
			return "" + this.value;
		}		
	}

	[System.Serializable]
	public class ConfigString : ConfigVars
	{
		public string value;
		
		public ConfigString()
		{

		}
		public ConfigString(string name, string value = "")
		{
			this.name = name;
			this.value = value;
		}
		
		public ConfigString SetValue(string value)
		{
			this.value = value;
			this.triggerValueChanged();
			return this;
		}
		
		public ConfigString setHidden(bool hide)
		{
			this.hidden = hide;
			return this;
		}

		public override string ToString(){
			return "" + this.value;
		}
	}

	[System.Serializable]
	public class ConfigVarStore
	{
		public Dictionary<string, ConfigNumber> varsNumber = new Dictionary<string, ConfigNumber>();
		public Dictionary<string, ConfigNumberRanged> varsNumberRanged = new Dictionary<string, ConfigNumberRanged>();
		public Dictionary<string, ConfigBoolean> varsBoolean = new Dictionary<string, ConfigBoolean>();
		public Dictionary<string, ConfigString> varsString = new Dictionary<string, ConfigString>();
		public Dictionary<string, ConfigNumberRangedInt> varsNumberRangedInt = new Dictionary<string, ConfigNumberRangedInt>();


		public List<ConfigVars> getAllVariables(){
			var allVars = new List<ConfigVars>();
			
			foreach(var v in varsNumber.Values){
				allVars.Add( (ConfigVars) v);
			}
			foreach(var v in varsBoolean.Values){
				allVars.Add( (ConfigVars) v);
			}
			foreach(var v in varsNumberRanged.Values){
				allVars.Add( (ConfigVars) v);
			}
			foreach(var v in varsNumberRangedInt.Values){
				allVars.Add( (ConfigVars) v);
			}
			foreach(var v in varsString.Values){
				allVars.Add( (ConfigVars) v);
			}
			return allVars;
		}

		public void putVar(ConfigVars variable, string name = "")
		{
			if (variable != null)
			{
				if (variable.name == null)
					variable.name = name;
				if (variable.name.Equals(""))
					throw new System.Exception("Config variable name is not provided");

				// if(variable is ConfigNumber)
				// 	this.varsNumber[variable.name] = (ConfigNumber) variable;
				// else if(variable is ConfigNumberRanged)
				// 	this.varsNumberRanged[variable.name] = (ConfigNumberRanged) variable;
				// else if(variable is ConfigBoolean)
				// 	this.varsBoolean[variable.name] = (ConfigBoolean) variable;
				// else if(variable is ConfigString)
				// 	this.varsString[variable.name] = (ConfigString) variable;

				getStore(variable)[variable.name] = variable;
			}
		}

		IDictionary getStore(ConfigVars variable){
			if(variable is ConfigNumber)
				return varsNumber;
			else if(variable is ConfigNumberRanged)
				return varsNumberRanged;
			else if(variable is ConfigNumberRangedInt)
				return varsNumberRangedInt;
			else if(variable is ConfigBoolean)
				return varsBoolean;
			else if(variable is ConfigString)
				return varsString;
			return null;
		}

		IDictionary getStore<T>(){
			if(typeof(T) == typeof(ConfigNumber))
				return varsNumber;
			else if(typeof(T) == typeof(ConfigNumberRanged))
				return varsNumberRanged;
			else if(typeof(T) == typeof(ConfigNumberRangedInt))
				return varsNumberRangedInt;
			else if(typeof(T) == typeof(ConfigBoolean))
				return varsBoolean;
			else if(typeof(T) == typeof(ConfigString))
				return varsString;
			return null;
		}

		public void removeVar(ConfigVars variable)
		{
			if (variable != null)
				getStore(variable).Remove(variable.name);
		}
		
		public T get<T>(string name) where T : ConfigVars{
			var store = getStore<T>();
			if(store.Contains(name))
				return (T) store[name];
			return null;
		}
		
		public void clear(){
			this.varsNumber.Clear();
			this.varsNumberRanged.Clear();
			this.varsNumberRangedInt.Clear();
			this.varsString.Clear();
			this.varsBoolean.Clear();
		}
	}
}