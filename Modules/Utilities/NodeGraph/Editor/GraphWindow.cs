using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;

namespace Igor
{
	public class GraphWindow<EntityType> : EditorWindow where EntityType : LinkedEntity<EntityType>, new() {
		
		static GraphWindow()
		{
			EditorTypeUtils.RegisterEditorTypes();
		}
		
		public enum MoveDirection
		{
			Move_Stopped,
			Move_Left,
			Move_Up,
			Move_Right,
			Move_Down
		}
		
		protected MoveDirection CurrentMoveDirection = MoveDirection.Move_Stopped;

		protected List<BoxLink<EntityType>> Connections;
		protected List<EntityBox<EntityType>> Boxes;
		protected EntityBox<EntityType> SelectedBox;
		
		protected List<EntityBox<EntityType>> GroupSelectedBoxes = new List<EntityBox<EntityType>>();
		protected Vector2 DragSelectStartPosition;
		protected bool bInDragSelect = false;
		protected Rect LastDragArea = new Rect(0.0f, 0.0f, 0.0f, 0.0f);
		protected Vector2 LastScrollDelta;
		
		protected InputOutputBox<EntityType> StartBox;
		protected InputOutputBox<EntityType> EndBox;
		
		protected InputState CurrentState;
		
		protected Anchor<EntityType> DragSource = null;
		protected BoxLink<EntityType> DragLink = null;
		
		protected Rect WindowBounds = new Rect(0.0f, 0.0f, 1000.0f, 1000.0f);
		protected Vector2 ScrollPosition = new Vector2();
		
		protected int MinimumSafeDistance = 5;
		
		protected float EdgeMovementSpeeds = 40000.0f;
		protected float EdgeAreaToStartMovement = 0.1f;
		
		protected int RepaintDelay = 1;
		
		protected Anchor<EntityType> StartingAnchorForNewBox = null;
		
		public virtual void Initialize()
		{
			Connections = new List<BoxLink<EntityType>>();
			Boxes = new List<EntityBox<EntityType>>();
			CurrentState = new InputState();
		}
		
		protected virtual void ConditionalRepaint()
		{
			if(MouseOverBox() != null || DragLink != null || --RepaintDelay == 0)
			{
				Repaint();
				
				RepaintDelay = 5;
			}
		}
		
		public virtual void CreateInputOutputBoxes()
		{
		}

		public virtual EntityBox<EntityType> GetInputBox()
		{
			return StartBox;
		}
		
		public virtual EntityBox<EntityType> GetOutputBox()
		{
			return EndBox;
		}
		
		public virtual void AddBox(EntityBox<EntityType> NewBox)
		{
			Boxes.Add(NewBox);
		}
		
		public virtual void CleanDirtyBox(EntityBox<EntityType> DirtyBox)
		{
			DirtyBox.UpdateBoxBounds();
			ArrangeBoxes();
			DirtyBox.CleanedUp();
		}
		
		public virtual LinkedEntityManager<EntityType> GetManager()
		{
			return null;
		}
		
		/*
		 * Arranges all the boxes.
		 * 
		 * Base GraphWindow functionality just makes sure they are all at least MinimumSafeDistance away.
		 */
		public virtual void ArrangeBoxes()
		{
			ArrangeBoxes(false);
		}

		public virtual void OrganizeBoxes()
		{
			ArrangeBoxes(true);
		}

		public virtual void ArrangeBoxes(bool bMoveBoxes)
		{
			Rect Extremes = WindowBounds;
			
			Extremes.width = 0.0f;
			Extremes.height = 0.0f;
			
			for(int CurrentBoxA = 0; CurrentBoxA < Boxes.Count; ++CurrentBoxA)
			{
				for(int CurrentBoxB = 0; CurrentBoxB < Boxes.Count; ++ CurrentBoxB)
				{
					if(CurrentBoxA == CurrentBoxB)
					{
						continue;
					}

					if(bMoveBoxes)
					{
						CheckAndResolveBoxCollision(Boxes[CurrentBoxA], Boxes[CurrentBoxB]);
					}
				}
				
				Rect BoxBounds = Boxes[CurrentBoxA].GetBoxBounds();
				if(BoxBounds.x - MinimumSafeDistance < Extremes.x)
				{
					Extremes.x = BoxBounds.x - MinimumSafeDistance;
				}
				if(BoxBounds.y - MinimumSafeDistance < Extremes.y)
				{
					Extremes.y = BoxBounds.y - MinimumSafeDistance;
				}
				if(BoxBounds.x + BoxBounds.width + (2*MinimumSafeDistance) > Extremes.width)
				{
					Extremes.width = BoxBounds.x + BoxBounds.width + (2*MinimumSafeDistance);
				}
				if(BoxBounds.y + BoxBounds.height + (2*MinimumSafeDistance) > Extremes.height)
				{
					Extremes.height = BoxBounds.y + BoxBounds.height + (2*MinimumSafeDistance);
				}
				
				WindowBounds = new Rect(Extremes.x, Extremes.y,
										Extremes.width - Extremes.x,
										Extremes.height - Extremes.y);
			}
			
			Extremes = WindowBounds;
			
			WindowBounds = new Rect(Extremes.x, Extremes.y,
									Extremes.width > position.width ? Extremes.width : position.width,
									Extremes.height > position.height ? Extremes.height : position.height);			
		}
		
		protected virtual void CheckAndResolveBoxCollision(EntityBox<EntityType> BoxA, EntityBox<EntityType> BoxB)
		{
			Rect BoxABounds = BoxA.GetBoxBounds();
			Rect BiggerBoxABounds = new Rect(BoxABounds.x-MinimumSafeDistance, BoxABounds.y-MinimumSafeDistance,
											 BoxABounds.width+(2*MinimumSafeDistance), BoxABounds.height+(2*MinimumSafeDistance));
			Rect BoxBBounds = BoxB.GetBoxBounds();
			Rect BiggerBoxBBounds = new Rect(BoxBBounds.x-MinimumSafeDistance, BoxBBounds.y-MinimumSafeDistance,
											 BoxBBounds.width+(2*MinimumSafeDistance), BoxBBounds.height+(2*MinimumSafeDistance));
			
			if(VisualScriptingDrawing.RectOverlapsRect(BiggerBoxABounds, BiggerBoxBBounds))
			{
				Rect CurrentRelativeRect = BiggerBoxABounds;
				bool bNewSpotFound = false;
				Rect NewBoxBBounds = BiggerBoxBBounds;
				
				while(!bNewSpotFound)
				{
					bool bFoundConflict = false;
					NewBoxBBounds = new Rect(CurrentRelativeRect.x + CurrentRelativeRect.width + 1,
											 CurrentRelativeRect.y,
											 BiggerBoxBBounds.width,
											 BiggerBoxBBounds.height);
					
					for(int CurrentBox = 0; CurrentBox < Boxes.Count; ++CurrentBox)
					{
						if(Boxes[CurrentBox] != BoxB)
						{
							if(VisualScriptingDrawing.RectOverlapsRect(NewBoxBBounds, Boxes[CurrentBox].GetBoxBounds()))
							{
								bFoundConflict = true;
								
								Rect NewBoxBounds = Boxes[CurrentBox].GetBoxBounds();
								CurrentRelativeRect = new Rect(NewBoxBounds.x-MinimumSafeDistance, NewBoxBounds.y-MinimumSafeDistance,
															   NewBoxBounds.width+(2*MinimumSafeDistance), NewBoxBounds.height+(2*MinimumSafeDistance));
							}
						}
					}
					
					if(!bFoundConflict)
					{
						bNewSpotFound = true;
					}
				}
				
				BoxB.MoveBoxTo(new Vector2(NewBoxBBounds.x + MinimumSafeDistance, NewBoxBBounds.y + MinimumSafeDistance));
			}
		}
		
		public virtual void ConnectInputToOutput(Anchor<EntityType> InputAnchor, Anchor<EntityType> OutputAnchor)
		{
			Connections.Add(new BoxLink<EntityType>(InputAnchor, OutputAnchor));
		}
		
		public virtual void BreakConnection(BoxLink<EntityType> ConnectionToBreak)
		{
			if(ConnectionToBreak == DragLink)
			{
				DragLink = null;
				CurrentMoveDirection = MoveDirection.Move_Stopped;
			}
			else
			{
				ConnectionToBreak.CleanupBeforeRemoval();
				Connections.Remove(ConnectionToBreak);
			}
		}
		
		public virtual void BreakAllConnectionsForAnchor(Anchor<EntityType> SourceAnchor)
		{
			for(int CurrentConnection = 0; CurrentConnection < Connections.Count; ++CurrentConnection)
			{
				if(Connections[CurrentConnection].IsConnecting(SourceAnchor))
				{
					Connections[CurrentConnection].CleanupBeforeRemoval();
					Connections.RemoveAt(CurrentConnection);
					--CurrentConnection;
				}
			}
		}
		
		public virtual void BreakAllConnectionsForBox(EntityBox<EntityType> BoxToDisconnect)
		{
			List< Anchor<EntityType> > FullAnchorList = BoxToDisconnect.GetAllAnchors();
			
			foreach(Anchor<EntityType> CurrentAnchor in FullAnchorList)
			{
				BreakAllConnectionsForAnchor(CurrentAnchor);
			}
		}
		
		public virtual void RemoveBox(EntityBox<EntityType> BoxToRemove)
		{
			BreakAllConnectionsForBox(BoxToRemove);
			
			Boxes.Remove(BoxToRemove);
			
			if(SelectedBox == BoxToRemove)
			{
				SelectedBox = null;
				Selection.activeObject = null;
			}
		}
		
		public virtual void BeginDragConnection(Anchor<EntityType> DragStart)
		{
			DragSource = DragStart;
			DragLink = new BoxLink<EntityType>(DragSource);
		}
		
		public virtual void HandleHoveredAnchor(Anchor<EntityType> HoveredAnchor)
		{
			if(!InputState.IsMouseButtonDown(InputState.MouseButton.Mouse_Left))
			{
				if(DragSource != null)
				{
					if(DragSource.CanConnectTo(HoveredAnchor))
					{
						BreakConnection(DragLink);
						ConnectInputToOutput(HoveredAnchor, DragSource);
					}
					
					DragSource = null;
				}
				else if(DragLink != null)
				{
					BreakConnection(DragLink);
				}
			}
			else
			{
				if(InputState.IsModifierDown(InputState.ModifierKeys.Key_Alt))
				{
					BreakAllConnectionsForAnchor(HoveredAnchor);
				}
				else if(DragSource == null)
				{
					BeginDragConnection(HoveredAnchor);
				}
			}
			
			HighlightLinks(HoveredAnchor);
		}
		
		public virtual void HighlightLinks(Anchor<EntityType> Anchor)
		{
			foreach(BoxLink<EntityType> CurrentLink in Connections)
			{
				if(CurrentLink.IsConnecting(Anchor))
				{
					CurrentLink.SetHighlighted(true);
				}
				else
				{
					CurrentLink.SetHighlighted(false);
				}
			}
		}
		
		public virtual Vector2 GetWindowOffset()
		{
			return new Vector2(WindowBounds.x, WindowBounds.y);
		}
		
		public virtual void UpdateWindow()
		{
			if(Boxes == null)
			{
				Close();
				
				return;
			}
			
			InputState.Update(this, ScrollPosition);
			
			bInDragSelect = false;
			
			bool bUpdateDragArea = false;
			
			if(Event.current.type == EventType.ContextClick)
			{
				HandleContextClick();
			}
			else if(Event.current.type == EventType.MouseDown)
			{
				if(InputState.IsMouseButtonDown(InputState.MouseButton.Mouse_Middle))
				{
					SelectedBox = null;
					Selection.activeObject = null;
					bInDragSelect = true;
					
					DragSelectStartPosition = InputState.GetLocalMousePosition(this, Vector2.zero);
				}
				else
				{
					EntityBox<EntityType> NewSelectedBox = MouseOverBox();
					
					if(NewSelectedBox == null)
					{
						Selection.activeObject = null;
						GroupSelectedBoxes.Clear();
					}
					else if(NewSelectedBox != SelectedBox)
					{
						if(GroupSelectedBoxes.Count == 0 || !GroupSelectedBoxes.Contains(NewSelectedBox))
						{
							GroupSelectedBoxes.Clear();
							NewSelectedBox.GainedFocus();
						}
					}
					
					SelectedBox = NewSelectedBox;
				}
			}
			else if(Event.current.type == EventType.MouseDrag)
			{
				bool bShouldCheckDrag = true;
				
				if(InputState.IsMouseButtonDown(InputState.MouseButton.Mouse_Middle) && !InputState.IsMouseButtonDown(InputState.MouseButton.Mouse_Left))
				{
					bShouldCheckDrag = false; 
					
					if(InputState.IsModifierDown(InputState.ModifierKeys.Key_Control))
					{
						bShouldCheckDrag = true;
						bInDragSelect = true;
						
						bUpdateDragArea = true;
					}
					else
					{
						ScrollPosition -= Event.current.delta;
					}
				}
				else
				{
					bInDragSelect = false;
					
					bShouldCheckDrag = DragLink != null || GroupSelectedBoxes.Count > 0;
				}
				
				if(bShouldCheckDrag)
				{
					Vector2 CurrentMousePosition = InputState.GetLocalMousePosition(this, Vector2.zero);
					
					CurrentMousePosition -= ScrollPosition;
					
					if(CurrentMousePosition.x < position.width * EdgeAreaToStartMovement)
					{
						CurrentMoveDirection = MoveDirection.Move_Left;
					}
					else if(CurrentMousePosition.x > position.width * (1.0f - EdgeAreaToStartMovement))
					{
						CurrentMoveDirection = MoveDirection.Move_Right;
					}
					else if(CurrentMousePosition.y < position.height * EdgeAreaToStartMovement)
					{
						CurrentMoveDirection = MoveDirection.Move_Up;
					}
					else if(CurrentMousePosition.y > position.height * (1.0f - EdgeAreaToStartMovement))
					{
						CurrentMoveDirection = MoveDirection.Move_Down;
					}
					else
					{
						CurrentMoveDirection = MoveDirection.Move_Stopped;
					}
				}
			}
			else if(Event.current.type == EventType.MouseUp)
			{
				if(!InputState.IsMouseButtonDown(InputState.MouseButton.Mouse_Middle))
				{
					LastDragArea = new Rect(0.0f, 0.0f, 0.0f, 0.0f);
					bInDragSelect = false;
					CurrentMoveDirection = MoveDirection.Move_Stopped;
				}
			}
			else if(!InputState.IsMouseButtonDown(InputState.MouseButton.Mouse_Left) &&
					!InputState.WasLastMouseUpHandled(this, InputState.MouseButton.Mouse_Left))
			{
				EntityBox<EntityType> NewSelectedBox = MouseOverBox();
				
				if(NewSelectedBox != null)
				{
					InputState.HandledMouseUp(InputState.MouseButton.Mouse_Left);
					
					if(InputState.WasLastClickDoubleClick(InputState.MouseButton.Mouse_Left))
					{
						InputState.HandledDoubleClick(InputState.MouseButton.Mouse_Left);

						if(GroupSelectedBoxes.Count == 0 || !GroupSelectedBoxes.Contains(NewSelectedBox))
						{
							GroupSelectedBoxes.Clear();
							NewSelectedBox.HandleDoubleClick();
						}
					}
				}
			}
			
			LastScrollDelta = new Vector2(0.0f, 0.0f);
			
			switch(CurrentMoveDirection)
			{
			case MoveDirection.Move_Left:
				LastScrollDelta = new Vector2(-EdgeMovementSpeeds * Time.deltaTime, 0.0f);
				bUpdateDragArea = bInDragSelect;
				break;
			case MoveDirection.Move_Right:
				LastScrollDelta = new Vector2(EdgeMovementSpeeds * Time.deltaTime, 0.0f);
				bUpdateDragArea = bInDragSelect;
				break;
			case MoveDirection.Move_Up:
				LastScrollDelta = new Vector2(0.0f, -EdgeMovementSpeeds * Time.deltaTime);
				bUpdateDragArea = bInDragSelect;
				break;
			case MoveDirection.Move_Down:
				LastScrollDelta = new Vector2(0.0f, EdgeMovementSpeeds * Time.deltaTime);
				bUpdateDragArea = bInDragSelect;
				break;
			default:
				break;
			}
			
			ScrollPosition += LastScrollDelta;
			
			if(bUpdateDragArea)
			{
				Vector2 CurrentPos = InputState.GetLocalMousePosition(this, Vector2.zero);
				
				Rect DragArea = new Rect(0.0f, 0.0f, 0.0f, 0.0f);
				
				if(DragSelectStartPosition.x < CurrentPos.x)
				{
					DragArea.x = DragSelectStartPosition.x;
					DragArea.width = CurrentPos.x - DragSelectStartPosition.x;
				}
				else
				{
					DragArea.x = CurrentPos.x;
					DragArea.width = DragSelectStartPosition.x - CurrentPos.x;
				}

				if(DragSelectStartPosition.y < CurrentPos.y)
				{
					DragArea.y = DragSelectStartPosition.y;
					DragArea.height = CurrentPos.y - DragSelectStartPosition.y;
				}
				else
				{
					DragArea.y = CurrentPos.y;
					DragArea.height = DragSelectStartPosition.y - CurrentPos.y;
				}
				
				LastDragArea = new Rect(DragArea.x - ScrollPosition.x, DragArea.y - ScrollPosition.y, DragArea.width, DragArea.height);
				
				GroupSelectAllBoxesInArea(DragArea);
			}
						
			CheckForDirtyBoxes();
		}
		
		public virtual void GroupSelectAllBoxesInArea(Rect DragArea)
		{
			GroupSelectedBoxes.Clear();
			
			foreach(EntityBox<EntityType> CurrentBox in Boxes)
			{
				if(CurrentBox.IsInsideDragArea(DragArea))
				{
					GroupSelectedBoxes.Add(CurrentBox);
				}
			}
		}
		
		public virtual void HandleContextClick()
		{
			EntityBox<EntityType> HoveredBox = MouseOverBox();
			if(HoveredBox == null)
			{
				if(HandleContextMenu())
				{
					Event.current.Use();
				}
			}
			else if(GroupSelectedBoxes.Count == 0)
			{
				GenericMenu GenericContextMenu = new GenericMenu();

				if(HoveredBox.HandleContextMenu(GenericContextMenu))
				{
					GenericContextMenu.ShowAsContext();
					
					Event.current.Use();
				}
			}
			else
			{
				if(HandleGroupSelectContextMenu())
				{
					Event.current.Use();
				}
			}
		}
		
		public virtual EntityBox<EntityType> MouseOverBox()
		{
			foreach(EntityBox<EntityType> CurrentBox in Boxes)
			{
				if(CurrentBox.GetBoxBounds().Contains(InputState.GetLocalMousePosition(this, -GetWindowOffset())))
				{
					return CurrentBox;
				}
			}
			
			return null;
		}
		
		public virtual void CheckForDirtyBoxes()
		{
			// We wait for the user to finish dirtying up the boxes to clean up after them
			if(!InputState.IsMouseButtonDown(InputState.MouseButton.Mouse_Left))
			{
				foreach(EntityBox<EntityType> CurrentBox in Boxes)
				{
					if(CurrentBox.IsDirty())
					{
						CleanDirtyBox(CurrentBox);
					}
				}
			}
		}
		
		protected virtual void OnDestroy()
		{
			PromptForSave("before closing this tab?");
			WindowClosed();
		}
		
		public virtual string[] GetSaveDialogText()
		{
			string[] DialogText = { "Save all entities?", "Would you like to save all entities", "Yes", "No" };
			
			return DialogText;
		}
		
		public virtual void SaveRequested()
		{
		}
		
		public virtual void PromptForSave(string Reason)
		{
			string[] SaveDialogText = GetSaveDialogText();
			
			if(SaveDialogText.Length == 4)
			{
				if(EditorUtility.DisplayDialog(SaveDialogText[0], SaveDialogText[1] + " " + Reason, SaveDialogText[2], SaveDialogText[3]))
				{
					SaveRequested();
				}
			}
		}
		
		public virtual void WindowClosed()
		{
		}
		
		protected virtual void Update()
		{
			if(Boxes == null)
			{
				Close();
				
				return;
			}
			
			ConditionalRepaint();
		}
		
		protected virtual void OnGUI()
		{
			try
			{
				if(Boxes == null)
				{
					Close();
					
					return;
				}
				
				UpdateWindow();
				DrawWindow();
				PostDrawUpdateWindow();
			}
			catch(System.Exception e)
			{
				Debug.Log("GraphWindow threw an exception in OnGUI() - " + e.ToString());
			}
		}
		
		public virtual void DrawWindow()
		{
			if(Boxes == null)
			{
				Close();
				
				return;
			}
			
			DrawGroupSelect();
			
			DrawLines(ScrollPosition);
			
			ScrollPosition = GUI.BeginScrollView (
	            new Rect (0, 0, position.width, position.height), 
	            ScrollPosition,
	            WindowBounds
	        	);
			
			bool bIsHoveringAnyAnchors = false;
			
			BeginWindows();
			for(int CurrentBox = 0; CurrentBox < Boxes.Count; ++CurrentBox)
			{
				HandleDrag(CurrentBox, GUI.Window(Boxes[CurrentBox].GetBoxID(CurrentBox), Boxes[CurrentBox].GetBoxBounds(), DrawBox, Boxes[CurrentBox].GetBoxTitle(), Boxes[CurrentBox].GetWindowStyle()));
				bIsHoveringAnyAnchors |= Boxes[CurrentBox].IsHoveringAnyAnchors();
			}
			EndWindows();
			
			if(!bIsHoveringAnyAnchors)
			{
				HighlightLinks(null);
			}
			
			GUI.EndScrollView();
		}
		
		public virtual void HandleDrag(int CurrentBoxIndex, Rect NewPosition)
		{
			if(GroupSelectedBoxes.Contains(Boxes[CurrentBoxIndex]))
			{
				Vector2 BoxPosition = Boxes[CurrentBoxIndex].GetPosition();
				
				if(BoxPosition.x != NewPosition.x || BoxPosition.y != NewPosition.y)
				{
					Vector2 Delta = new Vector2(NewPosition.x, NewPosition.y) - BoxPosition - LastScrollDelta;
					
					foreach(EntityBox<EntityType> CurrentSelectedBox in GroupSelectedBoxes)
					{
						Vector2 NewBoxPos = CurrentSelectedBox.GetPosition() + Delta;
						
						CurrentSelectedBox.SetPosition(NewBoxPos);
					}
				}
			}
			else
			{
				Boxes[CurrentBoxIndex].HandleDrag(NewPosition);
			}
		}
		
		public virtual void DrawGroupSelect()
		{
			if(LastDragArea.width > 0.0f)
			{
				EditorGUI.DrawRect(LastDragArea, Color.cyan);
			}
		}
		
		public virtual void PostDrawUpdateWindow()
		{
			if(!InputState.IsMouseButtonDown(InputState.MouseButton.Mouse_Left))
			{
				if(DragLink != null)
				{
					BreakConnection(DragLink);
					DragSource = null;
				}
			}
			
		}
		
		protected virtual void DrawBox(int Index)
		{
			Index = Mathf.RoundToInt((((float)Index) * 0.5f) - 0.1f);

			Boxes[Index].DrawBox();
			
			if(GroupSelectedBoxes.Contains(Boxes[Index]))
			{
				Boxes[Index].DrawSelectedOutline();
			}
		}
		
		public virtual void DrawLines(Vector2 Offset)
		{
			if(DragLink != null)
			{
				DragLink.DrawLink(Offset);
			}
			
			foreach(BoxLink<EntityType> CurrentLink in Connections)
			{
				CurrentLink.DrawLink(Offset);
			}
		}
		
		public virtual bool HandleGroupSelectContextMenu()
		{
			GenericMenu GroupSelectedContextMenu = new GenericMenu();
			
			AddGroupSelectedContextMenuEntries(GroupSelectedContextMenu);

			GroupSelectedContextMenu.AddItem(new GUIContent("Duplicate Selected"), false, DuplicateSelected);
			GroupSelectedContextMenu.AddItem(new GUIContent("Remove Selected"), false, RemoveSelected);
			
			GroupSelectedContextMenu.ShowAsContext();
			
			return true;
		}
		
		public virtual void AddGroupSelectedContextMenuEntries(GenericMenu MenuToAddTo)
		{
		}
		
		public virtual void DuplicateSelected()
		{
			foreach(EntityBox<EntityType> CurrentBox in GroupSelectedBoxes)
			{
				CurrentBox.DuplicateBox();
			}
		}
		
		public virtual void RemoveSelected()
		{
			foreach(EntityBox<EntityType> CurrentBox in GroupSelectedBoxes)
			{
				CurrentBox.RemoveBox();
			}
		}
		
		public virtual bool HandleChainBoxContextMenu(Anchor<EntityType> StartingAnchor)
		{
			StartingAnchorForNewBox = StartingAnchor;
			
			GenericMenu NewBoxContextMenu = new GenericMenu();
			
			AddNewBoxContextMenuEntries(NewBoxContextMenu);
			
			NewBoxContextMenu.ShowAsContext();
			
			return true;
		}
		
		public virtual bool HandleContextMenu()
		{
			GenericMenu NoBoxSelectedContextMenu = new GenericMenu();
			
			AddNoBoxContextMenuEntries(NoBoxSelectedContextMenu);
			
			NoBoxSelectedContextMenu.ShowAsContext();
			
			return true;
		}
		
		public virtual void AddNoBoxContextMenuEntries(GenericMenu MenuToAddTo)
		{
		}
		
		public virtual void AddNewBoxContextMenuEntries(GenericMenu MenuToAddTo)
		{
		}
		
		public virtual void ClearAllBoxesAndLinks()
		{
			for(int CurrentLink = 0; CurrentLink < Connections.Count;)
			{
				BreakConnection(Connections[CurrentLink]);
			}
			
			Connections.Clear();
			
			foreach(EntityBox<EntityType> CurrentBox in Boxes)
			{
				CurrentBox.CleanUpBeforeRemoval();
			}
			
			Boxes.Clear();
		}
		
		public virtual string GetBoxMetadataFile()
		{
			return "Assets/Editor/Resources/Boxes.xml";
		}
		
		public class BoxMetaDataSerializer<BoxDataEntityType> : XMLSerializable where BoxDataEntityType : LinkedEntity<BoxDataEntityType>, new()
		{
			public static GraphWindow<BoxDataEntityType> WindowToSerialize = null;
			
			public override void SerializeXML()
			{
				base.SerializeXML();
				
				SerializeBegin("BoxMetaDataSerializer", typeof(BoxDataEntityType).ToString(), typeof(BoxDataEntityType).ToString());
				
				foreach(EntityBox<BoxDataEntityType> CurrentBox in WindowToSerialize.Boxes)
				{
					Vector2 CurrentBoxPosition = CurrentBox.GetPosition();
					SerializeVector2(CurrentBox.GetBoxKey(), ref CurrentBoxPosition);
					CurrentBox.SetPosition(CurrentBoxPosition);
				}
			}
		};
		
		public virtual void SerializeBoxMetadata(bool bSaving)
		{
			GraphWindow<EntityType>.BoxMetaDataSerializer<EntityType> TempSerializer = new GraphWindow<EntityType>.BoxMetaDataSerializer<EntityType>();
			GraphWindow<EntityType>.BoxMetaDataSerializer<EntityType>.WindowToSerialize = this;
			XMLSerializable.SerializeFromXML<GraphWindow<EntityType>.BoxMetaDataSerializer<EntityType>>(GetBoxMetadataFile(), ref TempSerializer, bSaving);
		}
		
		public virtual void PreInit()
		{
			CreateStaticNodesIfNotPresent();
			
			CreateInputOutputBoxes();
		}
		
		public virtual void PostInit()
		{
			foreach(EntityBox<EntityType> CurrentBox in Boxes)
			{
				CurrentBox.BuildConnectionsFromSourceData();
			}

			SerializeBoxMetadata(false);

			ConditionalRepaint();
			
			Selection.activeGameObject = null;
		}
		
		public virtual void CreateStaticNodesIfNotPresent()
		{
		}
		
		public EntityBox<EntityType> GetEntityBoxForEntity(LinkedEntity<EntityType> Inst)
		{
			foreach(EntityBox<EntityType> CurrentBox in Boxes)
			{
				if(CurrentBox.WrapsInstance(Inst))
				{
					return CurrentBox;
				}
			}
			
			return null;
		}

	}
}