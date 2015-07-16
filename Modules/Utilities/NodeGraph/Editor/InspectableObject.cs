using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;

namespace Igor
{
	public class InspectableObject {

		protected Dictionary<string, bool> InspectorArrayExpanded = new Dictionary<string, bool>();
		protected bool bInspectorHasChangedProperty = false;
		
		public virtual object GetInspectorInstance()
		{
			return null;
		}
		
		public virtual void GainedFocus()
		{
			if(GetInspectorInstance() != null)
			{
				InspectorPicker.AddHandler(GetTypeName(), EntityOnInspectorGUI);
				
				InspectorPicker.SelectInstance(GetTypeName(), GetInspectorInstance());
			}
		}
		
		public virtual string GetTypeName()
		{
			return "";
		}

		public virtual void InspectorGUIString(string NameOfField, ref string FieldValue, bool bReadOnly = false)
		{
			string PreviousString = FieldValue != null ? FieldValue : "";

			if(bReadOnly)
			{
				EditorGUILayout.LabelField(NameOfField, FieldValue);
			}
			else
			{
				FieldValue = EditorGUILayout.TextField(NameOfField, FieldValue);
			}
			
			if(PreviousString != FieldValue)
			{
				bInspectorHasChangedProperty = true;
			}
		}
		
		public virtual void	InspectorGUIDropDown(string DropDownLabel, ref List<string> Values, ref string CurrentValue, bool bAppendAddNewEntryOption = false, string AddNewEntryOptionText = "", GenericButtonCallback OnAddNewEntry = null)
		{
			List<string> AllValues = Values;
			bool bHasAddNewEntryOption = false;
			
			if(bAppendAddNewEntryOption && AddNewEntryOptionText != "" && OnAddNewEntry != null)
			{
				AllValues = new List<string>();
				AllValues.AddRange(Values);
				AllValues.Add(AddNewEntryOptionText);
				
				bHasAddNewEntryOption = true;
			}
			
			int CurrentIndex = Values.IndexOf(CurrentValue);
			
			if(CurrentIndex == -1)
			{
				CurrentIndex = 0;
			}

			int OldIndex = CurrentIndex;
			
			CurrentIndex = EditorGUILayout.Popup(DropDownLabel, CurrentIndex, AllValues.ToArray());
			
			if(bHasAddNewEntryOption && CurrentIndex == (AllValues.Count - 1))
			{
				OnAddNewEntry();
			}
			else
			{
				CurrentValue = Values[CurrentIndex];
			}

			if(CurrentIndex != OldIndex)
			{
				bInspectorHasChangedProperty = true;
			}
		}
		
		public virtual bool InnerInspectorGUI()
		{
			return false;
		}
		
		public delegate InspectableObject GetEditorWrapper(XMLSerializable WrappedObject, int ItemIndex);

		public static Dictionary<string, int> CachedIntWaitForEnterValues = new Dictionary<string, int>();

		public static bool bRepaint = false;

		public virtual bool InspectorGUIIntWaitForEnter(string UniqueFieldName, string Label, int LastSavedValue, out int NewValue)
		{
			bool bShouldSaveValue = false;
			int CachedValue = 0;

			NewValue = LastSavedValue;

			if(CachedIntWaitForEnterValues.ContainsKey(UniqueFieldName))
			{
				CachedValue = CachedIntWaitForEnterValues[UniqueFieldName];
			}
			else
			{
				CachedValue = LastSavedValue;

				CachedIntWaitForEnterValues.Add(UniqueFieldName, CachedValue);
			}

			GUI.SetNextControlName(UniqueFieldName);
			bShouldSaveValue = EditorGUILayout.IntField(Label, CachedValue).KeyPressed<int>(UniqueFieldName, KeyCode.Return, LastSavedValue, out CachedValue);

			CachedIntWaitForEnterValues[UniqueFieldName] = CachedValue;

			if(bShouldSaveValue)
			{
				bRepaint = true;
				NewValue = CachedValue;
			}

			return bShouldSaveValue;
		}

		public delegate bool ValueInspectorDelegate<ValueType>(ref ValueType ValueToInspect);

		public virtual void InspectorGUIListEmbedded<ListType>(string ListName, string ItemPrefix, ref List<ListType> CurrentList, ValueInspectorDelegate<ListType> ValueInspector, bool bReadOnly = false, bool bReadOnlyCount = true, bool bStartOpen = false)
																where ListType : XMLSerializable, new()
		{
			if(!InspectorArrayExpanded.ContainsKey(ListName))
			{
				InspectorArrayExpanded.Add(ListName, bStartOpen);
			}
			
			bool bListExpanded = InspectorArrayExpanded[ListName];
			
			bListExpanded = EditorGUILayout.Foldout(bListExpanded, ListName);
			
			InspectorArrayExpanded[ListName] = bListExpanded;
			
			if(bListExpanded)
			{
				EditorGUI.indentLevel += 1;
				
				int NewListCount = CurrentList.Count;
				int OldListCount = NewListCount;
				bool bCountActuallyChanged = false;
				
				if(bReadOnly || bReadOnlyCount)
				{
					EditorGUILayout.LabelField("Count", CurrentList.Count.ToString());
				}
				else
				{
					bCountActuallyChanged = InspectorGUIIntWaitForEnter(ListName + "Count", "Count", OldListCount, out NewListCount);
				}
				
				if(bCountActuallyChanged && NewListCount != CurrentList.Count)
				{
					bInspectorHasChangedProperty = true;

					if(NewListCount > CurrentList.Count)
					{
						for(int CurrentElement = CurrentList.Count; CurrentElement < NewListCount; ++CurrentElement)
						{
							CurrentList.Add(new ListType());
						}
					}
					else
					{
						for(int CurrentElement = NewListCount; CurrentElement < CurrentList.Count;)
						{
							CurrentList.RemoveAt(CurrentElement);
						}
					}
				}

				int SmallestSize = OldListCount > NewListCount ? NewListCount : OldListCount;
				
				for(int CurrentIndex = 0; CurrentIndex < SmallestSize; ++CurrentIndex)
				{
					if(ValueInspector != null)
					{
						EditorGUILayout.BeginVertical("box");

						ListType TempRef = CurrentList[CurrentIndex];

						bInspectorHasChangedProperty = ValueInspector(ref TempRef) || bInspectorHasChangedProperty;

						CurrentList[CurrentIndex] = TempRef;

						EditorGUILayout.EndVertical();
					}
				}
				
				EditorGUI.indentLevel -= 1;
			}
		}
		
		public virtual void InspectorGUIOrderedListEmbedded<ListType>(string ListName, string ItemPrefix, ref List<ListType> CurrentList, ValueInspectorDelegate<ListType> ValueInspector, bool bReadOnly = false, bool bReadOnlyCount = true, bool bStartOpen = false)
																		where ListType : XMLSerializable, new()
		{
			if(!InspectorArrayExpanded.ContainsKey(ListName))
			{
				InspectorArrayExpanded.Add(ListName, bStartOpen);
			}
			
			bool bListExpanded = InspectorArrayExpanded[ListName];
			
			bListExpanded = EditorGUILayout.Foldout(bListExpanded, ListName);
			
			InspectorArrayExpanded[ListName] = bListExpanded;
			
			if(bListExpanded)
			{
				EditorGUI.indentLevel += 1;
				
				int NewListCount = CurrentList.Count;
				int OldListCount = NewListCount;
				bool bCountActuallyChanged = false;
				
				if(bReadOnly || bReadOnlyCount)
				{
					EditorGUILayout.LabelField("Count", CurrentList.Count.ToString());
				}
				else
				{
					bCountActuallyChanged = InspectorGUIIntWaitForEnter(ListName + "Count", "Count", OldListCount, out NewListCount);
				}
				
				if(bCountActuallyChanged && NewListCount != CurrentList.Count)
				{
					bInspectorHasChangedProperty = true;
					
					if(NewListCount > CurrentList.Count)
					{
						for(int CurrentElement = CurrentList.Count; CurrentElement < NewListCount; ++CurrentElement)
						{
							CurrentList.Add(new ListType());
						}
					}
					else
					{
						for(int CurrentElement = NewListCount; CurrentElement < CurrentList.Count;)
						{
							CurrentList.RemoveAt(CurrentElement);
						}
					}
				}
				
				int MoveUpIndex = -1;
				int MoveDownIndex = -1;
				
				int SmallestSize = OldListCount > NewListCount ? NewListCount : OldListCount;
				
				for(int CurrentIndex = 0; CurrentIndex < SmallestSize; ++CurrentIndex)
				{
					if(ValueInspector != null)
					{
						EditorGUILayout.BeginVertical("box");

						ListType TempRef = CurrentList[CurrentIndex];
						
						bInspectorHasChangedProperty = ValueInspector(ref TempRef) || bInspectorHasChangedProperty;
						
						CurrentList[CurrentIndex] = TempRef;

						EditorGUILayout.BeginHorizontal();
						
						if(GUILayout.Button("Move up"))
						{
							MoveUpIndex = CurrentIndex;
						}
						
						if(GUILayout.Button("Move down"))
						{
							MoveDownIndex = CurrentIndex;
						}
						
						EditorGUILayout.EndHorizontal();

						EditorGUILayout.EndVertical();
					}
				}
				
				if(MoveUpIndex > 0 && MoveUpIndex < SmallestSize)
				{
					ListType ItemToMove = CurrentList[MoveUpIndex];
					List<ListType> NewList = new List<ListType>();
					
					CurrentList.RemoveAt(MoveUpIndex);
					
					for(int NewItem = 0; NewItem < SmallestSize + 1; ++NewItem)
					{
						if(NewItem == MoveUpIndex-1)
						{
							NewList.Add(ItemToMove);
						}
						else if(NewItem >= MoveUpIndex)
						{
							NewList.Add(CurrentList[NewItem - 1]);
						}
						else
						{
							NewList.Add(CurrentList[NewItem]);
						}
					}
					
					CurrentList = NewList;
				}
				
				if(MoveDownIndex > -1 && MoveDownIndex < SmallestSize - 1)
				{
					ListType ItemToMove = CurrentList[MoveDownIndex];
					List<ListType> NewList = new List<ListType>();
					
					CurrentList.RemoveAt(MoveDownIndex);
					
					for(int NewItem = 0; NewItem < SmallestSize + 1; ++NewItem)
					{
						if(NewItem == MoveDownIndex+1)
						{
							NewList.Add(ItemToMove);
						}
						else if(NewItem > MoveDownIndex+1)
						{
							NewList.Add(CurrentList[NewItem - 1]);
						}
						else
						{
							NewList.Add(CurrentList[NewItem]);
						}
					}
					
					CurrentList = NewList;
				}
				
				EditorGUI.indentLevel -= 1;
			}
		}

		public delegate void LabeledInspector<ValueType>(string Label, ref ValueType ValueInst);

		public virtual void InspectorGUIDictionaryEmbeddedAndObject<KeyType, ValueType>(string DictionaryName, string KeyPrefix, string ValuePrefix, ref Dictionary<KeyType, ValueType> CurrentDictionary, LabeledInspector<KeyType> KeyInspector, GetEditorWrapper WrapperForValue, bool bReadOnly = false, bool bStartOpen = false)
				where KeyType : XMLSerializable, new()
				where ValueType : XMLSerializable, new()
		{
			if(!InspectorArrayExpanded.ContainsKey(DictionaryName))
			{
				InspectorArrayExpanded.Add(DictionaryName, bStartOpen);
			}
			
			bool bDictionaryExpanded = InspectorArrayExpanded[DictionaryName];
			
			bDictionaryExpanded = EditorGUILayout.Foldout(bDictionaryExpanded, DictionaryName);
			
			InspectorArrayExpanded[DictionaryName] = bDictionaryExpanded;
			
			if(bDictionaryExpanded)
			{
				EditorGUI.indentLevel += 1;
				
				int NewDictionaryCount = CurrentDictionary.Keys.Count;
				int OldDictionaryCount = NewDictionaryCount;
				bool bCountActuallyChanged = false;
				
				if(bReadOnly)
				{
					EditorGUILayout.LabelField("Count", CurrentDictionary.Keys.Count.ToString());
				}
				else
				{
					bCountActuallyChanged = InspectorGUIIntWaitForEnter(DictionaryName + "Count", "Count", OldDictionaryCount, out NewDictionaryCount);
				}
				
				if(bCountActuallyChanged && NewDictionaryCount != CurrentDictionary.Keys.Count)
				{
					bInspectorHasChangedProperty = true;
					
					if(NewDictionaryCount > CurrentDictionary.Keys.Count)
					{
						for(int CurrentElement = CurrentDictionary.Keys.Count; CurrentElement < NewDictionaryCount; ++CurrentElement)
						{
							CurrentDictionary.Add(new KeyType(), new ValueType());
						}
					}
					else
					{
						int CurrentIndex = 0;
						
						foreach(KeyValuePair<KeyType, ValueType> CurrentElement in CurrentDictionary)
						{
							if(CurrentIndex >= NewDictionaryCount)
							{
								CurrentDictionary.Remove(CurrentElement.Key);
							}
							
							++CurrentIndex;
						}
					}
				}
				
				int SmallestSize = OldDictionaryCount > NewDictionaryCount ? NewDictionaryCount : OldDictionaryCount;
				
				for(int CurrentIndex = 0; CurrentIndex < SmallestSize; ++CurrentIndex)
				{			
					int CurrentDictionaryIndex = 0;
					
					foreach(KeyValuePair<KeyType, ValueType> CurrentElement in CurrentDictionary)
					{
						if(CurrentIndex == CurrentDictionaryIndex)
						{
							EditorGUILayout.BeginVertical("box");
							
							KeyType NewKey = CurrentElement.Key;

							KeyInspector(KeyPrefix + " " + CurrentIndex, ref NewKey);
							
							ValueType NewValue = CurrentElement.Value;
							
							if(NewKey != CurrentElement.Key)
							{
								CurrentDictionary.Remove(CurrentElement.Key);
								CurrentDictionary.Add(NewKey, NewValue);
							}
							
							InspectableObject ValueInspector = WrapperForValue(NewValue, CurrentIndex);
							
							if(ValueInspector != null)
							{
								bInspectorHasChangedProperty = ValueInspector.InnerInspectorGUI() || bInspectorHasChangedProperty;
							}
							
							EditorGUILayout.EndVertical();
							
							break;
						}
						
						++CurrentDictionaryIndex;
					}
				}
				
				EditorGUI.indentLevel -= 1;
			}
		}

		public virtual void InspectorGUIDictionaryDropAndObject<KeyType, ValueType>(string DictionaryName, string KeyPrefix, string ValuePrefix, ref Dictionary<KeyType, ValueType> CurrentDictionary, DragAndDropHandler DropHandler, GetEditorWrapper WrapperForValue, bool bReadOnly = false, bool bStartOpen = false)
																where KeyType : XMLSerializable, new()
																where ValueType : XMLSerializable, new()
		{
			if(!InspectorArrayExpanded.ContainsKey(DictionaryName))
			{
				InspectorArrayExpanded.Add(DictionaryName, bStartOpen);
			}
			
			bool bDictionaryExpanded = InspectorArrayExpanded[DictionaryName];
			
			bDictionaryExpanded = EditorGUILayout.Foldout(bDictionaryExpanded, DictionaryName);
			
			InspectorArrayExpanded[DictionaryName] = bDictionaryExpanded;
			
			if(bDictionaryExpanded)
			{
				EditorGUI.indentLevel += 1;
				
				int NewDictionaryCount = CurrentDictionary.Keys.Count;
				int OldDictionaryCount = NewDictionaryCount;
				bool bCountActuallyChanged = false;
				
				if(bReadOnly)
				{
					EditorGUILayout.LabelField("Count", CurrentDictionary.Keys.Count.ToString());
				}
				else
				{
					bCountActuallyChanged = InspectorGUIIntWaitForEnter(DictionaryName + "Count", "Count", OldDictionaryCount, out NewDictionaryCount);
				}
				
				if(bCountActuallyChanged && NewDictionaryCount != CurrentDictionary.Keys.Count)
				{
					bInspectorHasChangedProperty = true;
					
					if(NewDictionaryCount > CurrentDictionary.Keys.Count)
					{
						for(int CurrentElement = CurrentDictionary.Keys.Count; CurrentElement < NewDictionaryCount; ++CurrentElement)
						{
							CurrentDictionary.Add(new KeyType(), new ValueType());
						}
					}
					else
					{
						int CurrentIndex = 0;
						
						foreach(KeyValuePair<KeyType, ValueType> CurrentElement in CurrentDictionary)
						{
							if(CurrentIndex >= NewDictionaryCount)
							{
								CurrentDictionary.Remove(CurrentElement.Key);
							}

							++CurrentIndex;
						}
					}
				}
				
				int SmallestSize = OldDictionaryCount > NewDictionaryCount ? NewDictionaryCount : OldDictionaryCount;
				
				for(int CurrentIndex = 0; CurrentIndex < SmallestSize; ++CurrentIndex)
				{			
					int CurrentDictionaryIndex = 0;
		
					foreach(KeyValuePair<KeyType, ValueType> CurrentElement in CurrentDictionary)
					{
						if(CurrentIndex == CurrentDictionaryIndex)
						{
							EditorGUILayout.BeginVertical("box");

							KeyType NewKey = (KeyType)InspectorGUIDragAndDropField(KeyPrefix + " " + CurrentIndex, CurrentElement.Key, DropHandler, CurrentIndex);
							
							ValueType NewValue = CurrentElement.Value;
							
							if(NewKey != CurrentElement.Key)
							{
								CurrentDictionary.Remove(CurrentElement.Key);
								CurrentDictionary.Add(NewKey, NewValue);
							}
							
							InspectableObject ValueInspector = WrapperForValue(NewValue, CurrentIndex);
							
							if(ValueInspector != null)
							{
								bInspectorHasChangedProperty = ValueInspector.InnerInspectorGUI() || bInspectorHasChangedProperty;
							}

							EditorGUILayout.EndVertical();

							break;
						}
						
						++CurrentDictionaryIndex;
					}
				}
				
				EditorGUI.indentLevel -= 1;
			}
		}

		public virtual void InspectorGUIDictionaryEmbeddedFloat<ListType>(string DictionaryName, string KeyPrefix, string ValuePrefix, ref Dictionary<ListType, float> CurrentDictionary, LabeledInspector<ListType> KeyInspector, bool bReadOnly = false, bool bStartOpen = false) where ListType : XMLSerializable, new()
		{
			if(!InspectorArrayExpanded.ContainsKey(DictionaryName))
			{
				InspectorArrayExpanded.Add(DictionaryName, bStartOpen);
			}
			
			bool bDictionaryExpanded = InspectorArrayExpanded[DictionaryName];
			
			bDictionaryExpanded = EditorGUILayout.Foldout(bDictionaryExpanded, DictionaryName);
			
			InspectorArrayExpanded[DictionaryName] = bDictionaryExpanded;
			
			if(bDictionaryExpanded)
			{
				EditorGUI.indentLevel += 1;
				
				int NewDictionaryCount = CurrentDictionary.Keys.Count;
				int OldDictionaryCount = NewDictionaryCount;
				bool bCountActuallyChanged = false;
				
				if(bReadOnly)
				{
					EditorGUILayout.LabelField("Count", CurrentDictionary.Keys.Count.ToString());
				}
				else
				{
					bCountActuallyChanged = InspectorGUIIntWaitForEnter(DictionaryName + "Count", "Count", OldDictionaryCount, out NewDictionaryCount);
				}
				
				if(bCountActuallyChanged && NewDictionaryCount != CurrentDictionary.Keys.Count)
				{
					bInspectorHasChangedProperty = true;
					
					if(NewDictionaryCount > CurrentDictionary.Keys.Count)
					{
						for(int CurrentElement = CurrentDictionary.Keys.Count; CurrentElement < NewDictionaryCount; ++CurrentElement)
						{
							CurrentDictionary.Add(new ListType(), 0.0f);
						}
					}
					else
					{
						int CurrentIndex = 0;
						
						foreach(KeyValuePair<ListType, float> CurrentElement in CurrentDictionary)
						{
							if(CurrentIndex >= NewDictionaryCount)
							{
								CurrentDictionary.Remove(CurrentElement.Key);
							}
							
							++CurrentIndex;
						}
					}
				}
				
				int SmallestSize = OldDictionaryCount > NewDictionaryCount ? NewDictionaryCount : OldDictionaryCount;
				
				for(int CurrentIndex = 0; CurrentIndex < SmallestSize; ++CurrentIndex)
				{			
					int CurrentDictionaryIndex = 0;
					
					foreach(KeyValuePair<ListType, float> CurrentElement in CurrentDictionary)
					{
						if(CurrentIndex == CurrentDictionaryIndex)
						{
							EditorGUILayout.BeginVertical("box");
							
							ListType NewKey = CurrentElement.Key;

							KeyInspector(KeyPrefix + " " + CurrentIndex, ref NewKey);
							
							float FloatValue = CurrentElement.Value;
							
							if(NewKey != CurrentElement.Key)
							{
								CurrentDictionary.Remove(CurrentElement.Key);
								CurrentDictionary.Add(NewKey, FloatValue);
							}
							
							InspectorGUIFloat(ValuePrefix + " " + CurrentIndex, ref FloatValue, bReadOnly);
							
							CurrentDictionary[NewKey] = FloatValue;
							
							EditorGUILayout.EndVertical();
							
							break;
						}
						
						++CurrentDictionaryIndex;
					}
				}
				
				EditorGUI.indentLevel -= 1;
			}
		}
		
		public virtual void InspectorGUIBool(string NameOfField, ref bool FieldValue, bool bReadOnly = false)
		{
			bool PreviousBool = FieldValue;
			
			if(bReadOnly)
			{
				EditorGUILayout.LabelField(NameOfField, FieldValue.ToString());
			}
			else
			{
				FieldValue = EditorGUILayout.Toggle(NameOfField, FieldValue);
			}
			
			if(PreviousBool != FieldValue)
			{
				bInspectorHasChangedProperty = true;
			}
		}
		
		public virtual void InspectorGUIInt(string NameOfField, ref int FieldValue, bool bReadOnly = false)
		{
			int PreviousInt = FieldValue;
			
			if(bReadOnly)
			{
				EditorGUILayout.LabelField(NameOfField, FieldValue.ToString());
			}
			else
			{
				FieldValue = EditorGUILayout.IntField(NameOfField, FieldValue);
			}
			
			if(PreviousInt != FieldValue)
			{
				bInspectorHasChangedProperty = true;
			}
		}
		
		public virtual void InspectorGUIFloat(string NameOfField, ref float FieldValue, bool bReadOnly = false)
		{
			float PreviousFloat = FieldValue;
			
			if(bReadOnly)
			{
				EditorGUILayout.LabelField(NameOfField, FieldValue.ToString());
			}
			else
			{
				FieldValue = EditorGUILayout.FloatField(NameOfField, FieldValue);
			}
			
			if(PreviousFloat != FieldValue)
			{
				bInspectorHasChangedProperty = true;
			}
		}

		public virtual void InspectorGUIVector3(string NameOfField, ref Vector3 FieldValue, bool bReadOnly = false)
		{
			Vector3 PreviousValue = FieldValue;

			if(bReadOnly)
			{
				EditorGUILayout.LabelField(NameOfField, "X: " + FieldValue.x + " Y: " + FieldValue.y + " Z: " + FieldValue.z);
			}
			else
			{
				FieldValue = EditorGUILayout.Vector3Field(NameOfField, FieldValue);
			}

			if(PreviousValue != FieldValue)
			{
				bInspectorHasChangedProperty = true;
			}
		}
		
		public delegate void GenericButtonCallback();
		
		public virtual void InspectorGUIButton(string ButtonText, GenericButtonCallback ButtonFunction)
		{
			if(GUILayout.Button(ButtonText))
			{
				ButtonFunction();
			}
		}

		public delegate void DragAndDropObjectHandler<ObjectType>(ref string ObjectKey, ObjectType NewObject, string NewObjectKey);
		
		public virtual void InspectorGUIDragAndDropObjectField<ObjectType>(string FieldLabel, ref string ObjectStringReference, ref ObjectType ObjectReference, DragAndDropObjectHandler<ObjectType> NewObjectHandler, string NewObjectKey)
														where ObjectType : Object
		{
			EditorGUILayout.BeginHorizontal();
			
			EditorGUILayout.PrefixLabel(FieldLabel);
			
			ObjectType NewObject = (ObjectType)EditorGUILayout.ObjectField(ObjectReference, typeof(ObjectType), true);
			
			EditorGUILayout.EndHorizontal();
			
			ObjectReference = NewObject;
			
			string OldString = ObjectStringReference;
			
			NewObjectHandler(ref ObjectStringReference, ObjectReference, NewObjectKey);
			
			if(OldString != ObjectStringReference)
			{
				bInspectorHasChangedProperty = true;
			}
		}

		public delegate void DragAndDropAudioClipHandler(ref string ObjectKey, AudioClip NewObject, string NewObjectKey);
		
		public virtual void InspectorGUIDragAndDropAudioClip(string FieldLabel, ref string ObjectStringReference, ref AudioClip ObjectReference, DragAndDropAudioClipHandler NewObjectHandler, string NewObjectKey)
	//													where ObjectType : Object
		{
	#if !USE_OBJECT_FIELD
			EditorGUILayout.BeginHorizontal();

			EditorGUILayout.PrefixLabel(FieldLabel);

			AudioClip NewClip = (AudioClip)EditorGUILayout.ObjectField(ObjectReference, typeof(AudioClip), false);

			EditorGUILayout.EndHorizontal();

			ObjectReference = NewClip;

			string OldString = ObjectStringReference;

			NewObjectHandler(ref ObjectStringReference, ObjectReference, NewObjectKey);

			if(OldString != ObjectStringReference)
			{
				bInspectorHasChangedProperty = true;
			}
	#else
			int IndentLevel = EditorGUI.indentLevel;
			
			EditorGUILayout.BeginHorizontal();
			
			EditorGUI.indentLevel = IndentLevel;
			
			EditorGUILayout.PrefixLabel(FieldLabel);
			
			GUILayout.Box(ObjectReference != null ? ObjectReference.ToString() : "", GUILayout.ExpandWidth(true));
			
			Rect DropBoxRect = GUILayoutUtility.GetLastRect();
			
			if(DropBoxRect.Contains(Event.current.mousePosition))
			{
				EventType TypeOfEvent = Event.current.type;
				
				if(TypeOfEvent == EventType.DragUpdated || TypeOfEvent == EventType.DragPerform)
				{
					DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
					
					if(TypeOfEvent == EventType.DragPerform)
					{
						DragAndDrop.AcceptDrag(); 
						
						AudioClip NewObject = null;
						Object NewObjectRef = DragAndDrop.objectReferences[0];
						
						if(NewObjectRef.GetType().IsAssignableFrom(typeof(AudioClip)))
						{
							NewObject = (AudioClip)DragAndDrop.objectReferences[0];
						}
						
						if(NewObject != null)
						{
							ObjectReference = NewObject;
						}
						
						string OldString = ObjectStringReference;
						
						NewObjectHandler(ref ObjectStringReference, ObjectReference);
						
						if(OldString != ObjectStringReference)
						{
							bInspectorHasChangedProperty = true;
						}
					}
					
					Event.current.Use();
				}
			}
			
			EditorGUILayout.EndHorizontal();
	#endif
		}
		
		public delegate XMLSerializable DragAndDropHandler(string DragAndDropData, int ItemIndex);
		
		public virtual XMLSerializable InspectorGUIDragAndDropField(string FieldLabel, XMLSerializable CurrentValue, DragAndDropHandler HandlerFunction, int Index = -1)
		{
			XMLSerializable NewObject = CurrentValue;
			
			EditorGUILayout.BeginHorizontal();
			
			EditorGUILayout.PrefixLabel(FieldLabel);
			
			GUILayout.Box(CurrentValue != null ? CurrentValue.EditorGetDisplayName() : "", GUILayout.ExpandWidth(true));
			
			Rect DropBoxRect = GUILayoutUtility.GetLastRect();
			
			if(DropBoxRect.Contains(Event.current.mousePosition))
			{
				EventType TypeOfEvent = Event.current.type;
				
				if(TypeOfEvent == EventType.DragUpdated || TypeOfEvent == EventType.DragPerform)
				{
					DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
					
					if(TypeOfEvent == EventType.DragPerform)
					{
						DragAndDrop.AcceptDrag(); 
						
						NewObject = HandlerFunction(DragAndDrop.paths[0], Index);
						
						if(NewObject == null)
						{
							NewObject = CurrentValue;
						}
					}
					
					Event.current.Use();
				}
			}
			
			EditorGUILayout.EndHorizontal();
			
			return NewObject;
		}

		public virtual void InspectorGUIOrderedStringList(string ListName, string ItemPrefix, ref List<string> CurrentList, bool bReadOnly = false, bool bReadOnlyCount = true, bool bStartOpen = false)
		{
			if(!InspectorArrayExpanded.ContainsKey(ListName))
			{
				InspectorArrayExpanded.Add(ListName, bStartOpen);
			}
			
			bool bListExpanded = InspectorArrayExpanded[ListName];
			
			bListExpanded = EditorGUILayout.Foldout(bListExpanded, ListName);
			
			InspectorArrayExpanded[ListName] = bListExpanded;
			
			if(bListExpanded)
			{
				EditorGUI.indentLevel += 1;
				
				int NewListCount = CurrentList.Count;
				int OldListCount = NewListCount;
				bool bCountActuallyChanged = false;
				
				if(bReadOnly || bReadOnlyCount)
				{
					EditorGUILayout.LabelField("Count", CurrentList.Count.ToString());
				}
				else
				{
					bCountActuallyChanged = InspectorGUIIntWaitForEnter(ListName + "Count", "Count", OldListCount, out NewListCount);
				}
				
				if(bCountActuallyChanged && NewListCount != CurrentList.Count)
				{
					bInspectorHasChangedProperty = true;
					
					if(NewListCount > CurrentList.Count)
					{
						for(int CurrentElement = CurrentList.Count; CurrentElement < NewListCount; ++CurrentElement)
						{
							CurrentList.Add("");
						}
					}
					else
					{
						for(int CurrentElement = NewListCount; CurrentElement < CurrentList.Count;)
						{
							CurrentList.RemoveAt(CurrentElement);
						}
					}
				}
				
				int MoveUpIndex = -1;
				int MoveDownIndex = -1;
				
				int SmallestSize = OldListCount > NewListCount ? NewListCount : OldListCount;
				
				for(int CurrentIndex = 0; CurrentIndex < SmallestSize; ++CurrentIndex)
				{
					EditorGUILayout.BeginVertical("box");
					
					string TempRef = CurrentList[CurrentIndex];

					InspectorGUIString(ItemPrefix + " " + CurrentIndex, ref TempRef, bReadOnly);

					CurrentList[CurrentIndex] = TempRef;
					
					EditorGUILayout.BeginHorizontal();
					
					if(GUILayout.Button("Move up"))
					{
						MoveUpIndex = CurrentIndex;
					}
					
					if(GUILayout.Button("Move down"))
					{
						MoveDownIndex = CurrentIndex;
					}
					
					EditorGUILayout.EndHorizontal();
					
					EditorGUILayout.EndVertical();
				}
				
				if(MoveUpIndex > 0 && MoveUpIndex < SmallestSize)
				{
					string ItemToMove = CurrentList[MoveUpIndex];
					List<string> NewList = new List<string>();
					
					CurrentList.RemoveAt(MoveUpIndex);
					
					for(int NewItem = 0; NewItem < SmallestSize; ++NewItem)
					{
						if(NewItem == MoveUpIndex-1)
						{
							NewList.Add(ItemToMove);
						}
						else if(NewItem >= MoveUpIndex)
						{
							NewList.Add(CurrentList[NewItem - 1]);
						}
						else
						{
							NewList.Add(CurrentList[NewItem]);
						}
					}
					
					CurrentList = NewList;

					bInspectorHasChangedProperty = true;
				}
				
				if(MoveDownIndex > -1 && MoveDownIndex < SmallestSize - 1)
				{
					string ItemToMove = CurrentList[MoveDownIndex];
					List<string> NewList = new List<string>();
					
					CurrentList.RemoveAt(MoveDownIndex);
					
					for(int NewItem = 0; NewItem < SmallestSize; ++NewItem)
					{
						if(NewItem == MoveDownIndex+1)
						{
							NewList.Add(ItemToMove);
						}
						else if(NewItem > MoveDownIndex+1)
						{
							NewList.Add(CurrentList[NewItem - 1]);
						}
						else
						{
							NewList.Add(CurrentList[NewItem]);
						}
					}
					
					CurrentList = NewList;
					
					bInspectorHasChangedProperty = true;
				}
				
				EditorGUI.indentLevel -= 1;
			}
		}

		public virtual void EntityOnInspectorGUI(object Instance)
		{
			EntityDrawInspectorWidgets(Instance);
			EntityPostDrawInspectorWidgets(Instance);
		}
		
		public virtual void EntityDrawInspectorWidgets(object Instance)
		{
		}
		
		public virtual void EntityPostDrawInspectorWidgets(object Instance)
		{
		}
		
		public virtual void OnInspectorGUIDrawSaveButton(string SaveText)
		{
			GUI.enabled = false;
			
			if(bInspectorHasChangedProperty)
			{
				GUI.enabled = true;
			}
			
			if(GUILayout.Button(SaveText))
			{
				OnInspectorGUIClickedSaveButton();
				bInspectorHasChangedProperty = false;
			}
			
			GUI.enabled = true;
		}
		
		public virtual void OnInspectorGUIClickedSaveButton()
		{
		}
	}
}
