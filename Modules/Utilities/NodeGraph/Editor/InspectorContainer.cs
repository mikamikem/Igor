using UnityEngine;
using System.Collections;

namespace Igor
{
	[System.Serializable]
	public class InspectorContainer : ScriptableObject {
		
		public string ClassType;
		public object Instance;

	}
}