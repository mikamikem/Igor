using UnityEngine;
using UnityEditor;
using System.Collections;

namespace Igor
{
	public class ListItem<ListType> : InspectableObject where ListType : XMLSerializable, new() {
		
		protected ListWindow<ManagerList<ListType>, ListType> Owner;
		
		protected bool bInEditMode = false;
		protected bool bIsSelected = false;
		protected Rect ItemBounds = new Rect();
		
		protected string EditModeName = "Untitled";
		protected string CompareString = "";
		protected int IndexToWatch;
		
		public ListItem(ListWindow<ManagerList<ListType>, ListType> NewOwner, int NewIndexToWatch = -1)
		{
			Owner = NewOwner;
			IndexToWatch = NewIndexToWatch;
			bInEditMode = IndexToWatch == -1;
		}
		
		protected virtual void HandeNewDisplayName(string NewDisplayName)
		{
		}
		
		public virtual void UpdateLastBounds(Rect NewBounds)
		{
			// Workaround for some inconsistent behavior.
			if(NewBounds.width != 0.0f && NewBounds.height != 0.0f)
			{
				ItemBounds = NewBounds;
			}
		}
		
		public virtual bool IsWithinBounds(Vector2 PointToCheck)
		{
			return ItemBounds.Contains(PointToCheck);
		}
		
		public virtual bool IsSelected()
		{
			return bIsSelected;
		}
		
		public virtual void ManagerSetSelected(bool bShouldBeSelected)
		{
			bIsSelected = bShouldBeSelected;
			
			if(bIsSelected)
			{
				GainedFocus();
			}
		}
		
		public virtual void SetSelected(bool bShouldBeSelected, bool bIsMultiSelect)
		{
			if(bIsMultiSelect)
			{
				Owner.UpdateMultiSelect(this, bShouldBeSelected);
			}
			else
			{
				Owner.UpdateSingleSelect(this, bShouldBeSelected);
			}
		}
		
		public virtual void SetIsInEditMode(bool bIsNowInEditMode)
		{
			if(!bIsNowInEditMode && IndexToWatch == -1 && EditModeName != "Untitled")
			{
				CreateNewSourceAsset();
			}
			
			bInEditMode = bIsNowInEditMode;
		}
		
		public virtual bool IsInEditMode()
		{
			return bInEditMode;
		}
		
		public virtual void HandleNewDisplayName(string NewName)
		{
			if(IndexToWatch == -1)
			{
				EditModeName = NewName;
			}
			else
			{
				Owner.GetManager().EditorSetDisplayName(IndexToWatch, NewName);
			}
		}
		
		public virtual void CreateNewSourceAsset()
		{
		}
		
		public virtual string GetCompareString()
		{
			return CompareString;
		}

		public virtual GUIStyle GetBackgroundGUIStyle(bool bEven)
		{
			GUIStyle CustomStyle = new GUIStyle();
			
			if(bIsSelected && Owner.HasFocus())
			{
				CustomStyle.normal.background = VisualScriptingDrawing.GenerateTexture2DWithColor(new Color(61.0f/255.0f, 96.0f/255.0f, 145.0f/255.0f, 1.0f));
			}
			else
			{
				if(bEven)
				{
					CustomStyle.normal.background = VisualScriptingDrawing.GenerateTexture2DWithColor(new Color(72.0f/255.0f, 72.0f/255.0f, 72.0f/255.0f, 1.0f));
				}
				else
				{
					CustomStyle.normal.background = VisualScriptingDrawing.GenerateTexture2DWithColor(new Color(50.0f/255.0f, 50.0f/255.0f, 50.0f/255.0f, 1.0f));
				}
			}
			
			return CustomStyle;
		}
		
		public virtual GUIStyle GetTextGUIStyle()
		{
			GUIStyle CustomStyle = new GUIStyle();
			
			CustomStyle.normal.textColor = Color.white;

			return CustomStyle;
		}
		
		public virtual string GetDisplayText()
		{
			if(Owner.GetManager() != null && IndexToWatch > -1 && IndexToWatch < Owner.GetManager().EditorGetListCount())
			{
				return Owner.GetManager().EditorGetDisplayName(IndexToWatch);
			}
			
			return EditModeName;
		}
		
		public virtual string GetDragAndDropText()
		{
			if(Owner.GetManager() != null && IndexToWatch > -1 && IndexToWatch < Owner.GetManager().EditorGetListCount())
			{
				return Owner.GetManager().EditorGetDragAndDropText(IndexToWatch);
			}
			
			return "";
		}
		
		public virtual bool bIsReal()
		{
			return IndexToWatch != -1;
		}

		public virtual void HandleDoubleClick()
		{
		}
		
		public virtual bool HandleMouseClick(Vector2 ClickPosition, InputState.MouseButton ButtonType, bool bCtrlClick, bool bShiftClick, bool bDoubleClick)
		{
			if(ItemBounds.Contains(ClickPosition))
			{
				if(bInEditMode)
				{
					// If we're in edit mode, don't handle the click event so it goes through
					// to the text control.
					return false;
				}
				else
				{
					if(ButtonType == InputState.MouseButton.Mouse_Left)
					{
						if(bDoubleClick)
						{
							HandleDoubleClick();
							
							return true;
						}
						else
						{
							if(bCtrlClick)
							{
								SetSelected(!bIsSelected, true);
								
								return true;
							}
							else if(bShiftClick)
							{
								// Handle shift selection
								
								return false;
							}
							else
							{
								SetSelected(true, false);
								
								return true;
							}
						}
					}
					else if(ButtonType == InputState.MouseButton.Mouse_Right)
					{
						Owner.ShowItemContextMenu();
						
						return true;
					}
				}
			}
			
			return false;
		}
	}
}
