using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Reflection;

namespace Igor
{
	public static class InspectableObjectHelper
	{
		public static FieldInfo LastControlIdField=typeof(EditorGUI).GetField("lastControlID", BindingFlags.Static|BindingFlags.NonPublic);
		public static int GetLastControlId(){
			if(LastControlIdField==null){
				Debug.LogError("Compatibility with Unity broke: can't find lastControlId field in EditorGUI");
				return 0;
			}
			return (int)LastControlIdField.GetValue(null);
		}

		public static bool KeyPressed<T>(this T s, string controlName,KeyCode key, T currentSetValue, out T fieldValue)
		{
			fieldValue = s;
			
			if(GUI.GetNameOfFocusedControl()==controlName)
			{
				if ((Event.current.type == EventType.KeyUp) && (Event.current.keyCode == key))
				{
					return true;
				}
				
				return false;
			}
			else
			{
				fieldValue = currentSetValue;

				return false;
			}
		}
	}
}
