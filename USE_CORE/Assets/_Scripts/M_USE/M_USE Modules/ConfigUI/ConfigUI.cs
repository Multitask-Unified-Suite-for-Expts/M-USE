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
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ConfigDynamicUI
{

	public class ConfigUI : MonoBehaviour
	{
		[HideInInspector]
		public ConfigVarStore store;
		public Transform varUIContainer;

		public List<GameObject> listGeneratedObjects = new List<GameObject>();

        //UI Hotkeys
        List<Selectable> m_orderedSelectables = new List<Selectable>();

        // UI - prefabs
        public GameObject prefabNumberInput;
		public GameObject prefabSlider;
		public GameObject prefabSlider2;
		public GameObject prefabSlider3;
		public GameObject prefabBoolean;

		public void clear()
		{
			store.clear();
			foreach (GameObject g in this.listGeneratedObjects)
			{
				Destroy(g);
			}
			listGeneratedObjects.Clear();
		}
		

		public void GenerateUI()
		{
			foreach (var v in store.getAllVariables())
			{
				GameObject g = null;
				if (v.hidden)
					continue;
				if (v is ConfigNumber) {
					//Debug.Log("Generating config ui for " + v.name + " is of type: " + v.GetType());
					var f = (ConfigNumber)v;
					if (f.isRange) {
						g = GenerateSlider (f);
					} else {
						g = GenerateNumberInput (f);
					}
				}else if (v is ConfigNumberRanged){
					var f = (ConfigNumberRanged)v;
					g = GenerateSlider2 (f);
				}else if (v is ConfigNumberRangedInt){
					var f = (ConfigNumberRangedInt)v;
					g = GenerateSlider3 (f);
				}else if(v is ConfigBoolean){
					var b = (ConfigBoolean)v;
					g = GenerateBoolean(b);
				}
				if(g != null)
					listGeneratedObjects.Add(g);
			}
		}
		
		public GameObject GenerateBoolean(ConfigBoolean b)
		{
			GameObject n = Instantiate(prefabBoolean);
			n.transform.SetParent(varUIContainer, false);
			UIBoolean ui = n.GetComponent<UIBoolean>();
			ui.setConfigVar(b);
			n.SetActive(true);
			return n;
		}
		
		public GameObject GenerateSlider(ConfigNumber f){
			GameObject n = Instantiate(prefabSlider);
			n.transform.SetParent(varUIContainer, false);
			UIRange ui = n.GetComponent<UIRange>();
			ui.setConfigVar(f);
			n.SetActive(true);
			return n;
		}

	
		public GameObject GenerateSlider2(ConfigNumberRanged f){
			GameObject n = Instantiate(prefabSlider2);
			n.transform.SetParent(varUIContainer, false);
			UIRange2 ui = n.GetComponent<UIRange2>();
			ui.setConfigVar(f);
			n.SetActive(true);
			return n;
		}
			
		public GameObject GenerateSlider3(ConfigNumberRangedInt f){
			GameObject n = Instantiate(prefabSlider3);
			n.transform.SetParent(varUIContainer, false);
			UIRange2Int ui = n.GetComponent<UIRange2Int>();
			ui.setConfigVar(f);
			n.SetActive(true);
			return n;
		}

		public GameObject GenerateNumberInput(ConfigNumber f){
			GameObject n = Instantiate(prefabNumberInput);
			n.transform.SetParent(varUIContainer, false);
			UINumber ui = n.GetComponent<UINumber>();
			ui.setConfigVar(f);
			n.SetActive(true);
			return n;
		}
		
		// Helper functions to create, get/set values of specific types


		public void SelectRandomValueForAllNumbers(){
			foreach (var f in store.varsNumberRanged.Values)
			{
				f.SetRandomValue ();
			}
			foreach (var f in store.varsNumberRangedInt.Values)
			{
				f.SetRandomValue ();
			}
		}
		
		public float GetFloat(string variableName){
			return (float) store.get<ConfigNumber>(variableName).value;
		}
		public int GetInt(string variableName)
		{
			return (int) store.get<ConfigNumber>(variableName).value;
		}

		public void SetRandomValueMono(string variableName){
			store.get<ConfigNumber>(variableName).SetRandomValue();
		}

		public float GetFloatRanged(string variableName){
			return (float) store.get<ConfigNumberRanged>(variableName).value;
		}

		public int GetIntRanged(string variableName)
		{
			return (int) store.get<ConfigNumberRangedInt>(variableName).value;
		}
			
		public bool GetBool(string variableName)
		{
			return store.get<ConfigBoolean>(variableName).value;
		}

		public string GetString(string variableName)
		{
			return store.get<ConfigString>(variableName).value;
		}
			
		public ConfigNumber CreateNumber(string name, int value = 0){
			ConfigNumber n = new ConfigNumber(name, value);
			store.putVar(n);
			return n;
		}
		
		public ConfigNumber CreateNumber(string name, float value = 0)
		{
			ConfigNumber n = new ConfigNumber(name, value).SetPrecision(2);
			store.putVar(n);
			return n;
		}
			
		public ConfigNumberRanged CreateNumberRanged(string name, float minvalue = 0, float maxvalue = 0)
		{
			ConfigNumberRanged n = new ConfigNumberRanged(name, minvalue, maxvalue).SetPrecision(2);
			store.putVar(n);
			return n;
		}

		public ConfigNumberRangedInt CreateNumberRangedInt(string name, int minvalue = 0, int maxvalue = 0){
			ConfigNumberRangedInt n = new ConfigNumberRangedInt(name, minvalue, maxvalue);
			store.putVar(n);
			return n;
		}
			
		public ConfigString CreateString(string name, string value = "")
		{
			ConfigString v = new ConfigString(name, value);
			store.putVar(v);
			return v;
		}
		
		public ConfigBoolean CreateBoolean(string name, bool value = false)
		{
			ConfigBoolean v = new ConfigBoolean(name, value);
			store.putVar(v);
			return v;
		}

        //HotKey Methods
        public void HandleHotkeySelect(bool _isNavigateBackward, bool _isWrapAround, bool _isEnterSelect)
        {
            SortSelectables();

            GameObject selectedObject = EventSystem.current.currentSelectedGameObject;
            if (selectedObject != null && selectedObject.activeInHierarchy) // Ensure a selection exists and is not an inactive object.
            {
                Selectable currentSelection = selectedObject.GetComponent<Selectable>();
                if (currentSelection != null)
                {
                    if (_isEnterSelect)
                    {
                        if (currentSelection.GetComponent<InputField>() != null)
                        {
                            ApplyEnterSelect(FindNextSelectable(m_orderedSelectables.IndexOf(currentSelection), _isNavigateBackward, _isWrapAround));
                        }
                        else if (currentSelection.GetComponent<Button>() != null)
                        {
                            currentSelection.GetComponent<Button>().onClick.Invoke();
                        }
                    }
                    else // Tab select.
                    {
                        Selectable nextSelection = FindNextSelectable(m_orderedSelectables.IndexOf(currentSelection), _isNavigateBackward, _isWrapAround);
                        if (nextSelection != null)
                        {
                            nextSelection.Select();
                        }
                    }
                }
                else
                {
                    SelectFirstSelectable(_isEnterSelect);
                }
            }
            else
            {
                SelectFirstSelectable(_isEnterSelect);
            }
        }

        ///<summary> Selects an input field or button, activating the button if one is found. </summary>
        public void ApplyEnterSelect(Selectable _selectionToApply)
        {
            if (_selectionToApply != null)
            {
                if (_selectionToApply.GetComponent<InputField>() != null)
                {
                    _selectionToApply.Select();
                }
                else
                {
                    Button selectedButton = _selectionToApply.GetComponent<Button>();
                    if (selectedButton != null)
                    {
                        _selectionToApply.Select();
                        selectedButton.OnPointerClick(new PointerEventData(EventSystem.current));
                    }
                }
            }
        }

        public void SelectFirstSelectable(bool _isEnterSelect)
        {
            if (m_orderedSelectables.Count > 0)
            {
                Selectable firstSelectable = m_orderedSelectables[0];
                if (_isEnterSelect)
                {
                    ApplyEnterSelect(firstSelectable);
                }
                else
                {
                    firstSelectable.Select();
                }
            }
        }

        public Selectable FindNextSelectable(int _currentSelectableIndex, bool _isNavigateBackward, bool _isWrapAround)
        {
            Selectable nextSelection = null;

            int totalSelectables = m_orderedSelectables.Count;
            if (totalSelectables > 1)
            {
                if (_isNavigateBackward)
                {
                    if (_currentSelectableIndex == 0)
                    {
                        nextSelection = (_isWrapAround) ? m_orderedSelectables[totalSelectables - 1] : null;
                    }
                    else
                    {
                        nextSelection = m_orderedSelectables[_currentSelectableIndex - 1];
                    }
                }
                else // Navigate forward.
                {
                    if (_currentSelectableIndex == (totalSelectables - 1))
                    {
                        nextSelection = (_isWrapAround) ? m_orderedSelectables[0] : null;
                    }
                    else
                    {
                        nextSelection = m_orderedSelectables[_currentSelectableIndex + 1];
                    }
                }
            }

            return (nextSelection);
        }

        public void SortSelectables()
        {
            List<Selectable> originalSelectables = Selectable.allSelectables;
            int totalSelectables = originalSelectables.Count;
            m_orderedSelectables = new List<Selectable>(totalSelectables);
            var buttons = new List<Selectable>(totalSelectables);
            for (int index = 0; index < totalSelectables; ++index)
            {
                Selectable selectable = originalSelectables[index];
                if (selectable.GetComponent<Button>() == null)
                {
                    m_orderedSelectables.Insert(FindSortedIndexForSelectable(m_orderedSelectables.Count, selectable, m_orderedSelectables), selectable);
                }
                else
                {
                    buttons.Insert(FindSortedIndexForSelectable(buttons.Count, selectable, buttons), selectable);
                }
            }

            foreach (Selectable s in buttons)
            {
                m_orderedSelectables.Add(s);
            }

        }

        ///<summary> Recursively finds the sorted index by positional order within m_orderedSelectables (positional order is determined from left-to-right followed by top-to-bottom). </summary>
        public int FindSortedIndexForSelectable(int _selectableIndex, Selectable _selectableToSort, List<Selectable> orderedSelectables)
        {
            int sortedIndex = _selectableIndex;
            if (_selectableIndex > 0)
            {
                int previousIndex = _selectableIndex - 1;
                Vector3 previousSelectablePosition = orderedSelectables[previousIndex].transform.position;
                Vector3 selectablePositionToSort = _selectableToSort.transform.position;

                if (previousSelectablePosition.y == selectablePositionToSort.y)
                {
                    if (previousSelectablePosition.x > selectablePositionToSort.x)
                    {
                        // Previous selectable is in front, try the previous index:
                        sortedIndex = FindSortedIndexForSelectable(previousIndex, _selectableToSort, orderedSelectables);
                    }
                }
                else if (previousSelectablePosition.y < selectablePositionToSort.y)
                {
                    // Previous selectable is in front, try the previous index:
                    sortedIndex = FindSortedIndexForSelectable(previousIndex, _selectableToSort, orderedSelectables);
                }
            }

            return (sortedIndex);
        }
    }

}