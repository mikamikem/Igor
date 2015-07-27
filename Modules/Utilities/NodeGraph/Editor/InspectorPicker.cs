using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;

namespace Igor
{
	[CustomEditor(typeof(InspectorContainer))]
	public class InspectorPicker : Editor {
		
		public delegate void HandleTypeOnInspectorGUI(object Instance);
		
		private static Dictionary<string, HandleTypeOnInspectorGUI> TypeDelegateLookup = new Dictionary<string, HandleTypeOnInspectorGUI>();
		private static HandleTypeOnInspectorGUI CurrentDelegate = null;
		private static string CurrentType = "";
		
		public static void SelectInstance(string Type, object Instance)
		{
			InspectorContainer NewContainer = (InspectorContainer)ScriptableObject.CreateInstance(typeof(InspectorContainer));
			NewContainer.ClassType = Type;
			NewContainer.Instance = Instance;
			
			UpdateCurrentDelegate(Type);

			Selection.activeObject = null;

			Selection.activeObject = NewContainer;
		}
		
		public override void OnInspectorGUI()
		{
			InspectorContainer Target = ((InspectorContainer)target);
			
			if(Target.ClassType != CurrentType)
			{
				UpdateCurrentDelegate(Target.ClassType);
			}
			
			if(CurrentDelegate != null)
			{
				CurrentDelegate(Target.Instance);
			}

			if(InspectableObject.bRepaint)
			{
				Repaint();

				InspectableObject.bRepaint = false;
			}
		}
		
		public static void AddHandler(string TypeName, HandleTypeOnInspectorGUI Handler)
		{
			TypeDelegateLookup[TypeName] = Handler;
		}
		
		public static void UpdateCurrentDelegate(string TypeName)
		{
			if(!TypeDelegateLookup.ContainsKey(TypeName))
			{
				CurrentDelegate = null;
			}
			else
			{
				CurrentDelegate = TypeDelegateLookup[TypeName];
			}
			CurrentType = TypeName;
		}
	}
}