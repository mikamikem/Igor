using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;

namespace Igor
{
	public class ListWindow<ManagerType, ListType> : EditorWindow
						where ManagerType : ManagerList<ListType>
						where ListType : XMLSerializable, new() {
		
		static ListWindow()
		{
			EditorTypeUtils.RegisterEditorTypes();
		}
		
		protected Vector2 ItemScrollPosition = new Vector2(0.0f, 0.0f);
		protected List<ListItem<ListType>> ListItems = new List<ListItem<ListType>>(); 
		protected bool bWindowHasFocus = false;
		
		protected bool bHasRunFirstRebuild = false;
		
		protected int RepaintDelay = 20;
		
		public virtual ManagerType GetManager()
		{
			return null;
		}

		public virtual void Initialize()
		{
			RebuildList();
		}

		protected virtual void Update()
		{
			if(EditorApplication.isCompiling)
			{
				bHasRunFirstRebuild = false;
			}

			ConditionalRepaint();
		}
		
		protected virtual void ConditionalRepaint()
		{
			if(--RepaintDelay == 0)
			{
	//			RebuildList();
				
				Repaint();
				
				RepaintDelay = 20;
			}
		}
		
		protected virtual string GetAddButtonText()
		{
			return "Add item";
		}
		
		protected virtual string GetRemoveButtonText()
		{
			if(GetSelectionCount() > 1)
			{
				return "Remove items";
			}
			else
			{
				return "Remove item";
			}
		}
		
		protected virtual ListItem<ListType> CreateNewItem(int IndexToWatch)
		{
			return null;
		}
		
		protected virtual void AddNewItem()
		{
			ListItems.Add(CreateNewItem(-1));
		}
		
		protected virtual void OnGUI()
		{
			if(EditorApplication.isCompiling)
			{
				EditorGUILayout.LabelField("Igor is waiting for Unity to finish recompiling!");
			}
			else
			{
				try
				{
					InputState.Update(this, Vector2.zero);

					DrawItems();
					
					// This needs to happen second so clicking on the button doesn't invalidate the selection.
					CheckForMouseEvents();

					CheckForKeyboardEvents();
				}
				catch(System.Exception e)
				{
					Debug.Log("ListWindow threw an exception in OnGUI() - " + e.ToString());
				}
			}
		}
		
		protected virtual void OnFocus()
		{
			bWindowHasFocus = true;
		}
		
		protected virtual void OnLostFocus()
		{
			bWindowHasFocus = false;
		}
		
		public virtual bool HasFocus()
		{
			return bWindowHasFocus;
		}
		
		public virtual void UpdateSingleSelect(ListItem<ListType> ItemToUpdate, bool bIsSelected)
		{
			for(int CurrentItem = 0; CurrentItem < ListItems.Count; ++CurrentItem)
			{
				if(ListItems[CurrentItem] != ItemToUpdate)
				{
					ListItems[CurrentItem].ManagerSetSelected(false);
				}
			}
			
			ItemToUpdate.ManagerSetSelected(bIsSelected);
		}
		
		public virtual void UpdateMultiSelect(ListItem<ListType> ItemToUpdate, bool bIsSelected)
		{
			ItemToUpdate.ManagerSetSelected(bIsSelected);
		}
		
		public virtual void ClearSelection()
		{
			for(int CurrentItem = 0; CurrentItem < ListItems.Count; ++CurrentItem)
			{
				ListItems[CurrentItem].ManagerSetSelected(false);
			}
			
			Selection.activeObject = null;
		}
		
		public virtual void SetAllToViewMode()
		{
			for(int CurrentItem = 0; CurrentItem < ListItems.Count; ++CurrentItem)
			{
				ListItems[CurrentItem].SetIsInEditMode(false);
			}
		}
		
		public virtual void CheckForMouseEvents()
		{
			if(!bWindowHasFocus)
			{
				return;
			}
			
			Vector2 ClickPosition = InputState.GetLocalMousePosition(this, new Vector2(0.0f, 40.0f));
			bool bHandleClick = false;
			InputState.MouseButton ButtonType = InputState.MouseButton.Mouse_Left;
			
			if(Event.current.type == EventType.MouseDrag)
			{
				for(int CurrentItem = 0; CurrentItem < ListItems.Count; ++CurrentItem)
				{
					if(ListItems[CurrentItem].IsWithinBounds(ClickPosition) &&
					   !ListItems[CurrentItem].IsInEditMode())
					{
						DragAndDrop.PrepareStartDrag();
						
						List<string> Paths = new List<string>();
						Paths.Add(ListItems[CurrentItem].GetDragAndDropText());
						DragAndDrop.paths = Paths.ToArray();
						
						DragAndDrop.StartDrag(ListItems[CurrentItem].GetDisplayText());
						
						Event.current.Use();
					}
				}
			}
			else if(!InputState.IsMouseButtonDown(InputState.MouseButton.Mouse_Left) &&
					!InputState.WasLastMouseUpHandled(this, InputState.MouseButton.Mouse_Left))
			{
				bHandleClick = true;
				ButtonType = InputState.MouseButton.Mouse_Left;
			}
			else if(!InputState.IsMouseButtonDown(InputState.MouseButton.Mouse_Right) &&
					!InputState.WasLastMouseUpHandled(this, InputState.MouseButton.Mouse_Right))
			{
				bHandleClick = true;
				ButtonType = InputState.MouseButton.Mouse_Right;
			}
			
			if(bHandleClick)
			{
				bool bCtrl = InputState.IsModifierDown(InputState.ModifierKeys.Key_Control);
				bool bShift = InputState.IsModifierDown(InputState.ModifierKeys.Key_Shift);
				bool bDoubleClick = InputState.WasLastClickDoubleClick(ButtonType);
				
				bool bClickHandled = false;
				
				for(int CurrentItem = 0; CurrentItem < ListItems.Count; ++CurrentItem)
				{
					if(ListItems[CurrentItem].IsWithinBounds(ClickPosition))
					{
						bClickHandled = true;
						
						if(ListItems[CurrentItem].HandleMouseClick(ClickPosition, ButtonType, bCtrl, bShift, bDoubleClick))
						{
							GUIUtility.keyboardControl = 0;

							InputState.HandledMouseUp(ButtonType);
							
							if(bDoubleClick)
							{
								InputState.HandledDoubleClick(ButtonType);
							}
							
							break;
						}
					}
				}
				
				if(!bClickHandled)
				{
					if(ButtonType == InputState.MouseButton.Mouse_Left)
					{
						ClearSelection();
						SetAllToViewMode();
					}
					else if(ButtonType == InputState.MouseButton.Mouse_Right)
					{
						ShowNoSelectionContextMenu();
					}
				}
			}
		}

		public virtual void CheckForKeyboardEvents()
		{
			if(!bWindowHasFocus)
			{
				return;
			}

			KeyCode RenameKeycode = KeyCode.Return;

	#if UNITY_EDITOR_WIN
			RenameKeycode = KeyCode.F2;
	#endif // UNITY_EDITOR_WIN

			if(InputState.IsKeyDown(RenameKeycode))
			{
				ListItem<ListType> SingleSelection = null;

				foreach(ListItem<ListType> CurrentItem in ListItems)
				{
					if(CurrentItem.IsSelected())
					{
						if(SingleSelection == null)
						{
							SingleSelection = CurrentItem;
						}
						else
						{
							SingleSelection = null;

							break;
						}
					}
				}

				if(SingleSelection != null && !SingleSelection.IsInEditMode())
				{
					SingleSelection.SetIsInEditMode(true);

					InputState.HandleKeyDown(RenameKeycode);
				}
			}
		}
		
		public virtual void ShowItemContextMenu()
		{
			// These are messing with the selection code so I'm turning them off for now.
	/*		GenericMenu GenericContextMenu = new GenericMenu();
			
			GenericContextMenu.AddItem(new GUIContent(GetAddButtonText()), false, AddNewItem);
			GenericContextMenu.AddItem(new GUIContent(GetRemoveButtonText()), false, RemoveSelectedItems);

			GenericContextMenu.ShowAsContext();*/
		}
		
		public virtual void ShowNoSelectionContextMenu()
		{
			// These are messing with the selection code so I'm turning them off for now.
	/*		GenericMenu GenericContextMenu = new GenericMenu();
			
			GenericContextMenu.AddItem(new GUIContent(GetAddButtonText()), false, AddNewItem);

			GenericContextMenu.ShowAsContext();*/
		}
		
		protected virtual int GetSelectionCount()
		{
			int NumSelectedItems = 0;
			
			for(int CurrentItem = 0; CurrentItem < ListItems.Count; ++CurrentItem)
			{
				if(ListItems[CurrentItem].IsSelected())
				{
					++NumSelectedItems;
				}
			}
			
			return NumSelectedItems;
		}
		
		protected virtual void RemoveSelectedItems()
		{
			for(int CurrentItem = 0; CurrentItem < ListItems.Count; ++CurrentItem)
			{
				if(ListItems[CurrentItem].IsSelected())
				{
					RemoveItemWithCompareString(ListItems[CurrentItem].GetCompareString());
					
					ListItems.RemoveAt(CurrentItem);
					
					--CurrentItem;
				}
			}
			
			RebuildList();
		}
		
		protected virtual void RemoveItemWithCompareString(string CompareString)
		{
		}

		protected virtual void DrawHeader()
		{
			EditorGUILayout.BeginHorizontal();
			
			if(GUILayout.Button(GetAddButtonText()))
			{
				AddNewItem();
			}
			
			if(GUILayout.Button(GetRemoveButtonText()))
			{
				RemoveSelectedItems();
			}
			
			EditorGUILayout.EndHorizontal();
		}
		
		protected virtual void DrawItems()
		{
			DrawHeader();

			ItemScrollPosition = EditorGUILayout.BeginScrollView(ItemScrollPosition);
			
			if(!bHasRunFirstRebuild)
			{
				RebuildList();
			}
			
			if(GetManager() != null)
			{
				bool bEven = true;

				for(int CurrentItem = 0; CurrentItem < ListItems.Count; ++CurrentItem)
				{
					ListItems[CurrentItem].UpdateLastBounds(EditorGUILayout.BeginHorizontal(ListItems[CurrentItem].GetBackgroundGUIStyle(bEven)));
					DrawItem(CurrentItem);
					EditorGUILayout.EndHorizontal();

					bEven = !bEven;
				}
			}
			
			EditorGUILayout.EndScrollView();
		}
		
		protected virtual void DrawItem(int Index)
		{
			if(ListItems[Index].IsInEditMode())
			{
				ListItems[Index].HandleNewDisplayName(GUILayout.TextField(ListItems[Index].GetDisplayText(), ListItems[Index].GetTextGUIStyle()));
				
				if(Event.current.keyCode == KeyCode.Return)
				{
					ListItems[Index].SetIsInEditMode(false);
				}
			}
			else
			{
				GUILayout.Label(ListItems[Index].GetDisplayText(), ListItems[Index].GetTextGUIStyle());
			}
		}
		
		public virtual void RebuildList()
		{
			if(GetManager() != null)
			{
				bHasRunFirstRebuild = true;
				
				List<ListItem<ListType>> NewList = new List<ListItem<ListType>>();
				List<int> ItemsToSkip = new List<int>();
				
				for(int CurrentItem = 0; CurrentItem < ListItems.Count; ++CurrentItem)
				{
					if(ListItems[CurrentItem].bIsReal())
					{
						string CurrentEditorCompareString = ListItems[CurrentItem].GetCompareString();
						
						int ExistingIndex = GetManager().EditorHasCompareString(CurrentEditorCompareString);
						
						if(ExistingIndex > -1)
						{
							ItemsToSkip.Add(ExistingIndex);
							NewList.Add(ListItems[CurrentItem]);
						}
					}
					else
					{
						NewList.Add(ListItems[CurrentItem]);
					}
				}
				
				for(int CurrentItem = 0; CurrentItem < GetManager().EditorGetListCount(); ++CurrentItem)
				{
					if(!ItemsToSkip.Contains(CurrentItem))
					{
						NewList.Add(CreateNewItem(CurrentItem));
					}
				}
				
				ListItems = NewList;
			}
			
			ConditionalRepaint();
		}
	}
}
