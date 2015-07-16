#if IGOR_RUNTIME || UNITY_EDITOR
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace Igor
{
	public class ManagerList<ListType> : XMLSerializable where ListType : XMLSerializable, new() {
		
	#if UNITY_EDITOR
		public virtual ListType EditorGetValueAtIndex(int Index)
		{
			return null;
		}
		
		public virtual int EditorHasCompareString(string CompareString)
		{
			return -1;
		}
		
		public virtual void EditorSetDisplayName(int Index, string NewName)
		{
			if(EditorGetValueAtIndex(Index) != null)
			{
				EditorGetValueAtIndex(Index).EditorSetDisplayName(NewName);
			}
		}
		
		public virtual string EditorGetDisplayName(int Index)
		{
			if(EditorGetValueAtIndex(Index) != null)
			{
				return EditorGetValueAtIndex(Index).EditorGetDisplayName();
			}
			
			return "";
		}
		
		public virtual string EditorGetDragAndDropText(int Index)
		{
			if(EditorGetValueAtIndex(Index) != null)
			{
				return EditorGetValueAtIndex(Index).EditorGetDragAndDropText();
			}
			
			return "";
		}
		
		public virtual string EditorGetCompareString(int Index)
		{
			if(EditorGetValueAtIndex(Index) != null)
			{
				return EditorGetValueAtIndex(Index).EditorGetCompareString();
			}
			
			return "";
		}
		
		public virtual int EditorGetListCount()
		{
			return 0;
		}
	#endif // UNITY_EDITOR
	}
}

#endif // IGOR_RUNTIME || UNITY_EDITOR
