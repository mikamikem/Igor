using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

namespace Igor
{
	public class EditorTypeUtils {

		public enum EntityTestType
		{
			FixOneWayLinks
		}

		static EditorTypeUtils()
		{
			EditorTypeUtils.RegisterEditorTypes();
		}
		
		public static void RegisterEditorTypes()
		{
			TypeUtils.RegisterAllTypes();

			List<Type> InspectableTypes = IgorRuntimeUtils.GetTypesInheritFrom<InspectableObject>();

			foreach(Type CurrentInspectableType in InspectableTypes)
			{
				MethodInfo RegisterFunction = CurrentInspectableType.GetMethod("RegisterEditorType", BindingFlags.Static | BindingFlags.Public | BindingFlags.DeclaredOnly);

				if(RegisterFunction != null)
				{
					RegisterFunction.Invoke(null, new object[]{});
				}
			}
		}
	}
}