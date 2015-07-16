using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;

namespace Igor
{
	public class EntityBox<EntityType> : InspectableObject where EntityType : LinkedEntity<EntityType>, new() {
		
		public GraphWindow<EntityType> Owner;
		protected LinkedEntity<EntityType> WrappedInstance;
		protected string BoxTitle;
		protected List<Anchor<EntityType>> Inputs = new List<Anchor<EntityType>>();
		protected List<Anchor<EntityType>> Outputs = new List<Anchor<EntityType>>();
		
		protected Vector2 Position;
		protected Vector2 Size;
		
		protected bool bDirty = true;
		
		protected int AnchorBoxSize = 10;
		protected int AnchorLabelGap = 7;
		protected int AnchorSubtextGap = 3;
		protected int InputOutputGap = 5;
		protected int WindowBorder = 5;
		protected int BlurbVerticalOffset = 5;
		
		protected bool bHoveringAnchor = false;
		
		protected bool bReadOnlyTitle = false;
		protected bool bReadOnlyInputList = false;
		protected bool bReadOnlyOutputList = false;
		
		protected Color SelectionColor = Color.red;
		protected float SelectionWidth = 2.0f;
		protected float SelectionOffset = 1.0f;

		protected Texture2D OriginalBackgroundTexture = null;
		protected Texture2D ErrorBackgroundTexture = null;

		protected Texture2D ActiveOriginalBackgroundTexture = null;
		protected Texture2D ActiveErrorBackgroundTexture = null;

		protected bool bErrorInNode = false;

		public EntityBox(GraphWindow<EntityType> InOwner, LinkedEntity<EntityType> InWrappedInstance)
		{
			Owner = InOwner;
			WrappedInstance = InWrappedInstance;

			UpdateAnchors();
		}

		public virtual void InitializeNewBox()
		{
		}
		
		public override object GetInspectorInstance()
		{
			return WrappedInstance;
		}
		
		public override string GetTypeName()
		{
			return "LinkedEntity";
		}

		public virtual void ConditionalInitBackgroundTextures()
		{
			if(OriginalBackgroundTexture == null)
			{
				OriginalBackgroundTexture = CreateBackgroundTexture(GUI.skin.window.normal.background, Color.white, 0.0f);
				ActiveOriginalBackgroundTexture = CreateBackgroundTexture(GUI.skin.window.onNormal.background, Color.white, 0.0f);

				CreateErrorBackgroundTextures();
			}
		}

		public virtual void CreateErrorBackgroundTextures()
		{
			ErrorBackgroundTexture = CreateBackgroundTexture(GUI.skin.window.normal.background, Color.red, 0.3f);
			ActiveErrorBackgroundTexture = CreateBackgroundTexture(GUI.skin.window.onNormal.background, Color.red, 0.3f);
		}

		public virtual Texture2D CreateBackgroundTexture(Texture2D Original, Color ColorToBlend, float ColorOpacity)
		{
			Texture2D BackgroundTex = Original;

			if(Original != null)
			{
				RenderTexture TempRenderTexture = new RenderTexture(Original.width, Original.height, 32);

				Graphics.Blit(Original, TempRenderTexture);

				RenderTexture.active = TempRenderTexture;

				BackgroundTex = new Texture2D(Original.width, Original.height);

				BackgroundTex.ReadPixels(new Rect(0.0f, 0.0f, Original.width, Original.height), 0, 0);

				RenderTexture.active = null;

				for(int CurrentX = 0; CurrentX < Original.width; ++CurrentX)
				{
					for(int CurrentY = 0; CurrentY < Original.height; ++CurrentY)
					{
						Color OriginalColor = BackgroundTex.GetPixel(CurrentX, CurrentY);
						Color NewColor = new Color((ColorToBlend.r*ColorOpacity) + (OriginalColor.r*(1.0f-ColorOpacity)), (ColorToBlend.g*ColorOpacity) + (OriginalColor.g*(1.0f-ColorOpacity)),
						                           (ColorToBlend.b*ColorOpacity) + (OriginalColor.b*(1.0f-ColorOpacity)), OriginalColor.a);

						BackgroundTex.SetPixel(CurrentX, CurrentY, NewColor);
					}
				}

				BackgroundTex.Apply();
			}
			
			return BackgroundTex;
		}

		public virtual GUIStyle GetWindowStyle()
		{
			ConditionalInitBackgroundTextures();

			GUIStyle WindowStyle = new GUIStyle(GUI.skin.window);

			if(bErrorInNode)
			{
				WindowStyle.normal.background = ErrorBackgroundTexture;
				WindowStyle.onNormal.background = ActiveErrorBackgroundTexture;
			}
			else
			{
				WindowStyle.normal.background = OriginalBackgroundTexture;
				WindowStyle.onNormal.background = ActiveOriginalBackgroundTexture;
			}

			return WindowStyle;
		}

		public virtual int GetBoxID(int BaseIndex)
		{
			return (BaseIndex*2) + (bErrorInNode ? 1 : 0);
		}
		
		public virtual bool IsHoveringAnyAnchors()
		{
			return bHoveringAnchor;
		}
		
		public virtual bool IsInsideDragArea(Rect DragArea)
		{
			return VisualScriptingDrawing.RectOverlapsRect(DragArea, new Rect(Position.x, Position.y, Size.x, Size.y));
		}
		
		public virtual void InspectorGUIList<ListType>(string ListName, string EntryPrefix, ref List<ListType> CurrentList, bool bReadOnly = false) where ListType : EntityLink<EntityType>, new()
		{
			if(!InspectorArrayExpanded.ContainsKey(ListName))
			{
				InspectorArrayExpanded.Add(ListName, false);
			}
			
			bool bArrayExpanded = InspectorArrayExpanded[ListName];
			
			bArrayExpanded = EditorGUILayout.Foldout(bArrayExpanded, ListName);
			
			InspectorArrayExpanded[ListName] = bArrayExpanded;
			
			if(bArrayExpanded)
			{
				string PreviousString = "";
				
				EditorGUI.indentLevel += 1;
				
				int NewArrayCount = CurrentList.Count;
				int OldListCount = NewArrayCount;
				bool bCountActuallyChanged = false;
				
				if(bReadOnly)
				{
					EditorGUILayout.LabelField("Count", CurrentList.Count.ToString());
				}
				else
				{
					bCountActuallyChanged = InspectorGUIIntWaitForEnter(ListName + "Count", "Count", OldListCount, out NewArrayCount);
				}
				
				if(bCountActuallyChanged && NewArrayCount != CurrentList.Count)
				{
					bInspectorHasChangedProperty = true;
					
					if(NewArrayCount > CurrentList.Count)
					{
						for(int CurrentElement = CurrentList.Count; CurrentElement < NewArrayCount; ++CurrentElement)
						{
							CurrentList.Add(new ListType());
						}
					}
					else
					{
						for(int CurrentElement = CurrentList.Count; CurrentElement > NewArrayCount; --CurrentElement)
						{
							CurrentList.RemoveAt(CurrentElement-1);
						}
					}
				}
				
				int SmallestSize = OldListCount > NewArrayCount ? NewArrayCount : OldListCount;
				
				for(int CurrentElement = 0; CurrentElement < SmallestSize; ++CurrentElement)
				{
					if(CurrentList[CurrentElement] != null)
					{
						PreviousString = CurrentList[CurrentElement].Name;
						
						if(bReadOnly)
						{
							EditorGUILayout.LabelField(EntryPrefix + " " + CurrentElement, PreviousString);
						}
						else
						{
							CurrentList[CurrentElement].Name = EditorGUILayout.TextField(EntryPrefix + " " + CurrentElement, PreviousString);
						}
						
						if(CurrentList[CurrentElement].Name != PreviousString)
						{
							bInspectorHasChangedProperty = true;
						}
					}
					else
					{
						if(bReadOnly)
						{
							EditorGUILayout.LabelField(EntryPrefix + " " + CurrentElement, "");
						}
						else
						{
							CurrentList[CurrentElement].Name = EditorGUILayout.TextField(EntryPrefix + " " + CurrentElement, "");
						}
						
						if(CurrentList[CurrentElement].Name != "")
						{
							bInspectorHasChangedProperty = true;
						}
					}
				}
				
				EditorGUI.indentLevel -= 1;
			}
		}
		
		public virtual void InspectorGUIStringList(string ListName, string EntryPrefix, ref List<string> CurrentList, bool bReadOnly = false)
		{
			if(!InspectorArrayExpanded.ContainsKey(ListName))
			{
				InspectorArrayExpanded.Add(ListName, false);
			}
			
			bool bArrayExpanded = InspectorArrayExpanded[ListName];
			
			bArrayExpanded = EditorGUILayout.Foldout(bArrayExpanded, ListName);
			
			InspectorArrayExpanded[ListName] = bArrayExpanded;
			
			if(bArrayExpanded)
			{
				string PreviousString = "";
				
				EditorGUI.indentLevel += 1;
				
				int NewArrayCount = CurrentList.Count;
				int OldListCount = NewArrayCount;
				bool bCountActuallyChanged = false;
				
				if(bReadOnly)
				{
					EditorGUILayout.LabelField("Count", CurrentList.Count.ToString());
				}
				else
				{
					bCountActuallyChanged = InspectorGUIIntWaitForEnter(ListName + "Count", "Count", OldListCount, out NewArrayCount);
				}
				
				if(bCountActuallyChanged && NewArrayCount != CurrentList.Count)
				{
					bInspectorHasChangedProperty = true;
					
					if(NewArrayCount > CurrentList.Count)
					{
						for(int CurrentElement = CurrentList.Count; CurrentElement < NewArrayCount; ++CurrentElement)
						{
							CurrentList.Add("");
						}
					}
					else
					{
						for(int CurrentElement = CurrentList.Count; CurrentElement > NewArrayCount; --CurrentElement)
						{
							CurrentList.RemoveAt(CurrentElement-1);
						}
					}
				}
				
				int SmallestSize = OldListCount > NewArrayCount ? NewArrayCount : OldListCount;
				
				for(int CurrentElement = 0; CurrentElement < SmallestSize; ++CurrentElement)
				{
					if(CurrentList[CurrentElement] != null)
					{
						PreviousString = CurrentList[CurrentElement];
						
						if(bReadOnly)
						{
							EditorGUILayout.LabelField(EntryPrefix + " " + CurrentElement, PreviousString);
						}
						else
						{
							CurrentList[CurrentElement] = EditorGUILayout.TextField(EntryPrefix + " " + CurrentElement, PreviousString);
						}
						
						if(CurrentList[CurrentElement] != PreviousString)
						{
							bInspectorHasChangedProperty = true;
						}
					}
					else
					{
						if(bReadOnly)
						{
							EditorGUILayout.LabelField(EntryPrefix + " " + CurrentElement, "");
						}
						else
						{
							CurrentList[CurrentElement] = EditorGUILayout.TextField(EntryPrefix + " " + CurrentElement, "");
						}
						
						if(CurrentList[CurrentElement] != "")
						{
							bInspectorHasChangedProperty = true;
						}
					}
				}
				
				EditorGUI.indentLevel -= 1;
			}
		}
		
		public virtual string GetClassTypeSaveString()
		{
			return "Save LinkedEntity";
		}
		
		public override void EntityDrawInspectorWidgets(object Instance)
		{
			base.EntityDrawInspectorWidgets(Instance);
			
			EntityType EntityInst = (EntityType)Instance;
			
			OnInspectorGUIDrawSaveButton(GetClassTypeSaveString());

			InspectorGUIString("Title", ref EntityInst.Title, bReadOnlyTitle);
			
			InspectorGUIListEmbedded<EntityLink<EntityType>>("Inputs", "Link", ref WrappedInstance.InputEvents, InspectEntityLink, bReadOnlyInputList, bReadOnlyInputList);
			
			InspectorGUIListEmbedded<EntityLink<EntityType>>("Outputs", "Link", ref WrappedInstance.OutputEvents, InspectEntityLink, bReadOnlyOutputList, bReadOnlyOutputList);
		}
		
		public virtual bool InspectEntityLink(ref EntityLink<EntityType> CurrentLink)
		{
			InspectorGUIString("Name", ref CurrentLink.Name);
			
			InspectorGUIOrderedListEmbedded<EntityLink<EntityType>>("Linked Elements", "Link", ref CurrentLink.LinkedEntities, InspectEntityPerLinkDetails);
			
			return false;
		}
		
		public virtual bool InspectEntityPerLinkDetails(ref EntityLink<EntityType> CurrentLink)
		{
			InspectorGUIString("Name", ref CurrentLink.Name, true);
			
			return false;
		}
		
		public override void EntityPostDrawInspectorWidgets(object Instance)
		{
			base.EntityPostDrawInspectorWidgets(Instance);
			
			if(bInspectorHasChangedProperty)
			{
				UpdateAnchors();
				
				Owner.Repaint();
			}
		}

		public virtual bool IsDirty()
		{
			return bDirty;
		}
		
		public virtual void CleanedUp()
		{
			bDirty = false;
		}
		
		public virtual string GetBoxTitle()
		{
			return BoxTitle;
		}
		
		public virtual string GetBoxKey()
		{
			return WrappedInstance.GetFilename();
		}
		
		public virtual void MoveBoxTo(Vector2 NewPosition)
		{
			Position = NewPosition;
		}
		
		public virtual void HandleDrag(Rect NewRect)
		{
			if(Position.x != NewRect.x || Position.y != NewRect.y)
			{
				Position.x = NewRect.x;
				Position.y = NewRect.y;
				bDirty = true;
			}
		}
		
		public virtual void HandleDoubleClick()
		{
		}
		
		public virtual void CanConnectNodes(Anchor<EntityType> InputNode, Anchor<EntityType> OutputNode)
		{
		}
		
		public virtual void UpdateBoxBounds()
		{
			if(Size == Vector2.zero)
			{
				Size = new Vector2(100.0f, 100.0f);
			}
		}
		
		public virtual void DrawBox()
		{
			DrawAnchors(new Vector2(0.0f, 20.0f));
			
			if(!bHoveringAnchor)
			{
				GUI.DragWindow();
			}
		}
		
		public virtual void DrawSelectedOutline()
		{
			Rect BoxBounds = GetBoxBounds();

			// Top line
			VisualScriptingDrawing.DrawLine(new Vector2(-SelectionOffset - (BoxBounds.width * 0.5f), -SelectionOffset), new Vector2(SelectionOffset + (BoxBounds.width * 0.5f), -SelectionOffset), SelectionColor, SelectionWidth, false);
			// Right line
			VisualScriptingDrawing.DrawLine(new Vector2(SelectionOffset + BoxBounds.width, -SelectionOffset), new Vector2(SelectionOffset + BoxBounds.width, SelectionOffset + BoxBounds.height), SelectionColor, SelectionWidth, false);
			// Bottom line
			VisualScriptingDrawing.DrawLine(new Vector2(-SelectionOffset - (BoxBounds.width * 0.5f), SelectionOffset + BoxBounds.height), new Vector2(SelectionOffset + (BoxBounds.width * 0.5f), SelectionOffset + BoxBounds.height), SelectionColor, SelectionWidth, false);
			// Left line
			VisualScriptingDrawing.DrawLine(new Vector2(-SelectionOffset, -SelectionOffset), new Vector2(-SelectionOffset, SelectionOffset + BoxBounds.height), SelectionColor, SelectionWidth, false);
		}
		
		public enum TypeOfAnchor
		{
			Anchor_Normal
		}
		
		public virtual TypeOfAnchor GetAnchorTypeForIndex(bool bIsInputList, int Index)
		{
			return TypeOfAnchor.Anchor_Normal;
		}
		
		public virtual void DrawAnchorContent(Anchor<EntityType> CurrentAnchor, int CurrentAnchorIndex, ref Vector2 Origin, ref float WidestLabel, ref Rect WholeAnchorRect, bool bIsHovered, bool bJustCalculateSize, TypeOfAnchor AnchorType, bool bLeftAlign)
		{
			string LabelTitle = CurrentAnchor.GetLabelDescription();
			Vector2 LabelSize = GetLabelSize(LabelTitle);
			Rect LabelRect = new Rect(Origin.x + WidestLabel - AnchorBoxSize - LabelSize.x, Origin.y, LabelSize.x, LabelSize.y);
			WholeAnchorRect = new Rect(LabelRect.x, LabelRect.y, LabelSize.x + (2*AnchorBoxSize), LabelSize.y);
			
			if(bLeftAlign)
			{
				LabelRect = new Rect(Origin.x + AnchorBoxSize, Origin.y, LabelSize.x, LabelSize.y);
				WholeAnchorRect = new Rect(Origin.x, Origin.y, LabelSize.x + (2*AnchorBoxSize), LabelSize.y);
			}
			
			if(!bJustCalculateSize)
			{
				if(bIsHovered)
				{
					GUI.color = Color.red;
					Owner.HandleHoveredAnchor(CurrentAnchor);
				}
				else
				{
					GUI.color = Color.white;
				}
				
				GUI.Label(LabelRect, LabelTitle);
				GUI.color = Color.white;

				Origin.y += WholeAnchorRect.height;
			}
			
			WidestLabel = WholeAnchorRect.width > WidestLabel ? WholeAnchorRect.width : WidestLabel;
		}
		
		public virtual void DrawAnchor(Anchor<EntityType> CurrentAnchor, int CurrentAnchorIndex, ref Vector2 Origin, ref bool bAnyAnchorsHovered, ref float WidestAnchor, bool bJustCalculateSize, TypeOfAnchor AnchorType, bool bLeftAlign)
		{
			Vector2 WindowOffset = Owner.GetWindowOffset();
			Rect WholeAnchorRect = new Rect(0.0f, 0.0f, 0.0f, 0.0f);
			bool bIsHovered = false;
			
			DrawAnchorContent(CurrentAnchor, CurrentAnchorIndex, ref Origin, ref WidestAnchor, ref WholeAnchorRect, bIsHovered, true, AnchorType, bLeftAlign);
			
			if(!bJustCalculateSize)
			{
				bIsHovered = WholeAnchorRect.Contains(InputState.GetLocalMousePosition(Owner, new Vector2(Position.x - Owner.GetWindowOffset().x, Position.y+10.0f - Owner.GetWindowOffset().y)));

				if(bIsHovered)
				{
					bAnyAnchorsHovered = true;
				}
				
				DrawAnchorContent(CurrentAnchor, CurrentAnchorIndex, ref Origin, ref WidestAnchor, ref WholeAnchorRect, bIsHovered, false, AnchorType, bLeftAlign);
				
				CurrentAnchor.LastRect = new Rect(Position.x + WholeAnchorRect.x - WindowOffset.x, Position.y + WholeAnchorRect.y - WindowOffset.y + 5.0f,
												 WholeAnchorRect.width, WholeAnchorRect.height);
				
				Origin.y += AnchorLabelGap;
			}
		}
		
		public virtual void DrawAnchors(Vector2 Origin)
		{
			float InputMinWidth = 0.0f;
			float OutputMinWidth = 0.0f;
			
			DrawAnchors(ref Origin, ref InputMinWidth, ref OutputMinWidth, false);
		}
		
		public virtual void DrawAnchors(ref Vector2 Origin, ref float InputMinWidth, ref float OutputMinWidth, bool bJustCalculateSize, float MinHeight = 0.0f)
		{
			Vector2 OriginalOrigin = Origin;
			float TallestColumn = 0.0f;
			bool bAnyAnchorsHovered = false;
			int CurrentAnchorIndex = 0;

			Vector2 BlurbSize = DrawBlurb(ref Origin, bJustCalculateSize);

			for(CurrentAnchorIndex = 0; CurrentAnchorIndex < Inputs.Count; ++CurrentAnchorIndex)
			{
				DrawAnchor(Inputs[CurrentAnchorIndex], CurrentAnchorIndex, ref Origin, ref bAnyAnchorsHovered, ref InputMinWidth, bJustCalculateSize, GetAnchorTypeForIndex(true, CurrentAnchorIndex), true);
			}
			
			TallestColumn = Origin.y;
			
			if(!bJustCalculateSize)
			{
				Origin = new Vector2(InputMinWidth + InputOutputGap, OriginalOrigin.y + BlurbSize.y);
			}
			
			CurrentAnchorIndex = 0;
			
			for(CurrentAnchorIndex = 0; CurrentAnchorIndex < Outputs.Count; ++CurrentAnchorIndex)
			{
				DrawAnchor(Outputs[CurrentAnchorIndex], CurrentAnchorIndex, ref Origin, ref bAnyAnchorsHovered, ref OutputMinWidth, true, GetAnchorTypeForIndex(false, CurrentAnchorIndex), false);
			}
			
			if(!bJustCalculateSize)
			{
				CurrentAnchorIndex = 0;
				
				for(CurrentAnchorIndex = 0; CurrentAnchorIndex < Outputs.Count; ++CurrentAnchorIndex)
				{
					DrawAnchor(Outputs[CurrentAnchorIndex], CurrentAnchorIndex, ref Origin, ref bAnyAnchorsHovered, ref OutputMinWidth, false, GetAnchorTypeForIndex(false, CurrentAnchorIndex), false);
				}
			}
			
			TallestColumn = TallestColumn > Origin.y ? TallestColumn : Origin.y;

			Vector2 NewSize = new Vector2(InputMinWidth + InputOutputGap + OutputMinWidth + (2*WindowBorder), TallestColumn + (2*WindowBorder));
			
			Vector2 TitleLabelSize = Vector2.zero;
			
			if(WrappedInstance != null)
			{
				TitleLabelSize = GetLabelSize(WrappedInstance.Title);
			}
			
			float MinWidth = TitleLabelSize.x > 50.0f ? TitleLabelSize.x : 50.0f;
			
			if(NewSize.x < MinWidth)
			{
				NewSize.x = MinWidth;
			}

			if(NewSize.x < BlurbSize.x)
			{
				NewSize.x = BlurbSize.x;
			}

			MinHeight = MinHeight > 50.0f ? MinHeight : 50.0f;
			
			if(NewSize.y < MinHeight)
			{
				NewSize.y = MinHeight;
			}
			
			if(NewSize != Size)
			{
				Size = NewSize;
				bDirty = true;
			}
			
			bHoveringAnchor = bAnyAnchorsHovered;
		}
		
		public virtual void DrawAnchorBox(Vector2 Origin, bool bHovering)
		{
		}

		public virtual string GetBlurb()
		{
			return "";
		}

		public virtual string MakeMultiline(string OriginalText, int CharacterMax)
		{
			string NewText = OriginalText;

			if(NewText != null)
			{
				for(int CurrentChar = CharacterMax; CurrentChar < OriginalText.Length && CurrentChar > 0; )
				{
					if(NewText[CurrentChar] == ' ')
					{
						NewText = NewText.Substring(0, CurrentChar) + "\n" + NewText.Substring(CurrentChar+1);

						CurrentChar += CharacterMax;
					}
					else
					{
						--CurrentChar;
					}
				}
			}

			return NewText;
		}

		public virtual Vector2 DrawBlurb(ref Vector2 Origin, bool bJustCalculateSize)
		{
			string BlurbString = MakeMultiline(GetBlurb(), 30);
			Vector2 BlurbSize = Vector2.zero;

			if(BlurbString != "")
			{
				BlurbSize = GetLabelSize(BlurbString);

				Rect BlurbRect = new Rect(Origin.x, Origin.y, BlurbSize.x, BlurbSize.y);

				BlurbSize.y += BlurbVerticalOffset;

				if(!bJustCalculateSize)
				{
					Origin.y += BlurbSize.y;

					GUI.Label(BlurbRect, BlurbString);
				}
	        }
			
			return BlurbSize;
		}
		
		public virtual Vector2 GetLabelSize(string Label)
		{
			GUIContent TempLabel = new GUIContent(Label);
			return GUI.skin.label.CalcSize(TempLabel);
		}
		
		public virtual Rect GetBoxBounds()
		{
			return new Rect(Position.x, Position.y, Size.x, Size.y);
		}
		
		public virtual Vector2 GetPosition()
		{
			return Position;
		}
		
		public virtual void SetPosition(Vector2 NewPosition)
		{
			Position = NewPosition;
		}
		
		public virtual bool HandleContextMenu(GenericMenu GenericContextMenu)
		{
			GenericContextMenu.AddItem(new GUIContent("Duplicate " + GetTypeName()), false, DuplicateBox);
			GenericContextMenu.AddItem(new GUIContent("Remove " + GetTypeName()), false, RemoveBox);
			
			GenericContextMenu.AddSeparator("");
			
			GenericContextMenu.AddItem(new GUIContent("Create save at node"), false, CreateSaveAtNode);
			GenericContextMenu.AddItem(new GUIContent("Play from node"), false, PlayFromNode);
			
			GenericContextMenu.AddSeparator("");
			
			return true;
		}
		
		public virtual void CreateSaveAtNode()
		{
	/*		EntityID CurrentNodeID = WrappedInstance.GenerateEntityIDForEvent();
			
			GameSaveManager.SetCurrentSaveSlot(0);
			
			GameSave FirstSave = GameSaveManager.GetActiveSave();

			FirstSave.Checkpoints = new List<GameSave.CheckpointData>();

			GameSave.CheckpointData NewCheckpoint = new GameSave.CheckpointData();
			NewCheckpoint.LastEntity = CurrentNodeID;

			FirstSave.Checkpoints.Add(NewCheckpoint);
			
			GameSaveManager.SaveAllGameSaves();*/
		}
		
		public virtual void PlayFromNode()
		{
			CreateSaveAtNode();
			
			EditorApplication.isPlaying = true;
		}
		
		public virtual void DuplicateBox()
		{
		}
		
		public virtual void RemoveBox()
		{
			Owner.RemoveBox(this);
			RemoveEntity();
		}

		public virtual void RemoveEntity()
		{
		}

		public virtual string GetAnchorText(EntityLink<EntityType> Link)
		{
			return Link.Name;
		}
		
		public virtual void UpdateAnchors()
		{
			if(WrappedInstance != null)
			{
				WrappedInstance.CreateStaticNodesIfNotPresent();
			}
			
			List<Anchor<EntityType>> DeletedList = new List<Anchor<EntityType>>();
			DeletedList.AddRange(Inputs);
			
			foreach(EntityLink<EntityType> InputLink in GetInputEvents())
			{
				bool bHasAnchor = false;
				foreach(Anchor<EntityType> CurrentAnchor in Inputs)
				{
					if(CurrentAnchor.GetLabelText() == InputLink.Name)
					{
						bHasAnchor = true;
						DeletedList.Remove(CurrentAnchor);
					}
				}
				if(!bHasAnchor)
				{
					Anchor<EntityType> InputAnchor = new Anchor<EntityType>(InputLink.Name, GetAnchorText(InputLink), new Anchor<EntityType>.AnchorType(true), this);
					Inputs.Add(InputAnchor);
				}
			}
			
			foreach(Anchor<EntityType> DeletedItem in DeletedList)
			{
				DeletedItem.CleanupBeforeRemoval();
				Owner.BreakAllConnectionsForAnchor(DeletedItem);
				Inputs.Remove(DeletedItem);
			}
			
			DeletedList.Clear();
			DeletedList.AddRange(Outputs);
			
			foreach(EntityLink<EntityType> OutputLink in GetOutputEvents())
			{
				bool bHasAnchor = false;
				foreach(Anchor<EntityType> CurrentAnchor in Outputs)
				{
					if(CurrentAnchor.GetLabelText() == OutputLink.Name)
					{
						bHasAnchor = true;
						DeletedList.Remove(CurrentAnchor);
					}
				}
				if(!bHasAnchor)
				{
					Anchor<EntityType> OutputAnchor = new Anchor<EntityType>(OutputLink.Name, GetAnchorText(OutputLink), new Anchor<EntityType>.AnchorType(false), this);
					Outputs.Add(OutputAnchor);
				}
			}
			
			foreach(Anchor<EntityType> DeletedItem in DeletedList)
			{
				DeletedItem.CleanupBeforeRemoval();
				Owner.BreakAllConnectionsForAnchor(DeletedItem);
				Outputs.Remove(DeletedItem);
			}
			
			DeletedList.Clear();
		}
		
		public virtual void OnConnectedAnchors(Anchor<EntityType> LocalAnchor, Anchor<EntityType> RemoteAnchor)
		{
			EntityBox<EntityType> RemoteBox = RemoteAnchor.Owner;
			string LocalAnchorName = "";
			string RemoteAnchorName = "";
			
			if(Inputs.Contains(LocalAnchor))
			{
				LocalAnchorName = "Input";
			}
			else
			{
				LocalAnchorName = "Output";
			}
			
			LocalAnchorName += LocalAnchor.GetLabelText();
			
			if(RemoteBox.Inputs.Contains(RemoteAnchor))
			{
				RemoteAnchorName = "Input";
			}
			else
			{
				RemoteAnchorName = "Output";
			}
			
			RemoteAnchorName += RemoteAnchor.GetLabelText();
			
			EntityLink<EntityType> LocalLink = GetLinkByName(LocalAnchorName);
			EntityLink<EntityType> RemoteLink = RemoteBox.GetLinkByName(RemoteAnchorName);
			
			if(LocalLink != null && RemoteLink != null)
			{
				LocalLink.EstablishLink(RemoteLink);
			}
		}
		
		public virtual void OnDisconnectedAnchors(Anchor<EntityType> LocalAnchor, Anchor<EntityType> RemoteAnchor)
		{
			EntityBox<EntityType> RemoteBox = RemoteAnchor.Owner;
			string LocalAnchorName = "";
			string RemoteAnchorName = "";
			
			if(Inputs.Contains(LocalAnchor))
			{
				LocalAnchorName = "Input";
			}
			else
			{
				LocalAnchorName = "Output";
			}
			
			LocalAnchorName += LocalAnchor.GetLabelText();
			
			if(RemoteBox.Inputs.Contains(RemoteAnchor))
			{
				RemoteAnchorName = "Input";
			}
			else
			{
				RemoteAnchorName = "Output";
			}
			
			RemoteAnchorName += RemoteAnchor.GetLabelText();
			
			EntityLink<EntityType> LocalLink = GetLinkByName(LocalAnchorName);
			EntityLink<EntityType> RemoteLink = RemoteBox.GetLinkByName(RemoteAnchorName);
			
			if(LocalLink != null && RemoteLink != null)
			{
				LocalLink.BreakLink(RemoteLink);
			}
		}
		
		public virtual EntityLink<EntityType> GetLinkByName(string AnchorName)
		{
			return WrappedInstance.GetLinkByName(AnchorName);
		}
		
		public override void OnInspectorGUIClickedSaveButton()
		{
			base.OnInspectorGUIClickedSaveButton();
			
			if(WrappedInstance.GetFilename().Length == 0 && WrappedInstance.Title.Length > 0)
			{
				string UniqueName = GetUniqueFilename(WrappedInstance.Title);
				WrappedInstance.EditorSetFilename(UniqueName);
			}
			
			SaveEntities();

			Owner.SerializeBoxMetadata(true);
		}
		
		public virtual string GetUniqueFilename(string OriginalFilename)
		{
			return OriginalFilename;
		}
		
		public virtual void SaveEntities()
		{
		}
		
		public virtual void CleanUpBeforeRemoval()
		{
		}
		
		public virtual List<EntityLink<EntityType>> GetInputEvents()
		{
			return WrappedInstance.InputEvents;
		}
		
		public virtual List<EntityLink<EntityType>> GetOutputEvents()
		{
			return WrappedInstance.OutputEvents;
		}
		
		public virtual void BuildConnectionsFromSourceData()
		{
			List<EntityLink<EntityType>> InputEvents = GetInputEvents();
			foreach(EntityLink<EntityType> CurrentLink in InputEvents)
			{
				Anchor<EntityType> LocalAnchor = GetAnchor("Input" + CurrentLink.Name);
				
				if(LocalAnchor != null)
				{
					foreach(EntityLink<EntityType> CurrentRemoteLink in CurrentLink.LinkedEntities)
					{
						if(CurrentRemoteLink.GetOwner() != null && CurrentRemoteLink.GetOwner().OutputEvents.Contains(CurrentRemoteLink))
						{
							EntityBox<EntityType> RemoteBox = Owner.GetEntityBoxForEntity(CurrentRemoteLink.GetOwner());
							
							if(RemoteBox != null)
							{
								Anchor<EntityType> RemoteAnchor = RemoteBox.GetAnchor("Output" + CurrentRemoteLink.Name);
								
								if(RemoteAnchor != null)
								{
									Owner.ConnectInputToOutput(LocalAnchor, RemoteAnchor);
								}
							}
						}
					}
				}
			}
		}

		public virtual bool WrapsInstance(LinkedEntity<EntityType> InstToCheck)
		{
			return WrappedInstance.GetFilename() == InstToCheck.GetFilename();
		}
		
		public Anchor<EntityType> GetAnchor(string LinkToMatch)
		{
			if(LinkToMatch.StartsWith("Input"))
			{
				string LinkName = LinkToMatch.Substring(5);
				
				foreach(Anchor<EntityType> CurrentAnchor in Inputs)
				{
					if(CurrentAnchor.GetLabelText() == LinkName)
					{
						return CurrentAnchor;
					}
				}
			}
			else if(LinkToMatch.StartsWith("Output"))
			{
				string LinkName = LinkToMatch.Substring(6);
				
				foreach(Anchor<EntityType> CurrentAnchor in Outputs)
				{
					if(CurrentAnchor.GetLabelText() == LinkName)
					{
						return CurrentAnchor;
					}
				}
			}
			
			return null;
		}
		
		public virtual List< Anchor<EntityType> > GetAllAnchors()
		{
			List< Anchor<EntityType> > FullList = new List< Anchor<EntityType> >();
			
			FullList.AddRange(Inputs);
			FullList.AddRange(Outputs);
			
			return FullList;
		}

		public virtual void RunChecks(EditorTypeUtils.EntityTestType TestType)
		{
			bErrorInNode = false;

			switch(TestType)
			{
			case EditorTypeUtils.EntityTestType.FixOneWayLinks:
				FixOneWayLinks();
				break;
			}
		}

		public virtual void FixOneWayLinks()
		{
			if(WrappedInstance == null)
			{
				Debug.LogError("Somehow we are running this check on objects that don't exist?");
				
				return;
			}

			foreach(EntityLink<EntityType> CurrentConnector in WrappedInstance.InputEvents)
			{
				foreach(EntityLink<EntityType> RemoteConnector in CurrentConnector.LinkedEntities)
				{
					bool bFound = false;

					foreach(EntityLink<EntityType> RemoteRemoteConnector in RemoteConnector.LinkedEntities)
					{
						if(RemoteRemoteConnector.GetOwner() == CurrentConnector.GetOwner() && RemoteRemoteConnector.Name == CurrentConnector.Name)
						{
							bFound = true;

							break;
						}
					}

					if(!bFound)
					{
						bErrorInNode = true;
					}
				}
			}

			foreach(EntityLink<EntityType> CurrentConnector in WrappedInstance.OutputEvents)
			{
				foreach(EntityLink<EntityType> RemoteConnector in CurrentConnector.LinkedEntities)
				{
					bool bFound = false;

					foreach(EntityLink<EntityType> RemoteRemoteConnector in RemoteConnector.LinkedEntities)
					{
						if(RemoteRemoteConnector.GetOwner() == CurrentConnector.GetOwner() && RemoteRemoteConnector.Name == CurrentConnector.Name)
						{
							bFound = true;

							break;
						}
					}

					if(!bFound)
					{
						bErrorInNode = true;
					}
				}
			}
		}
	}
}
