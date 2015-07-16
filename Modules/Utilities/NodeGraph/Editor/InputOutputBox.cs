using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;

namespace Igor
{
	public class InputOutputBox<EntityType>  : EntityBox<EntityType> where EntityType : LinkedEntity<EntityType>, new()
	{
		public bool bIsInputBox;
		
		public InputOutputBox(GraphWindow<EntityType> Owner, EntityType EntityToWatch, string InBoxTitle, bool InbIsInputBox) : base(Owner, EntityToWatch)
		{
			bIsInputBox = InbIsInputBox;
			
			BoxTitle = InBoxTitle;
			bReadOnlyTitle = true;
			
			bReadOnlyInputList = bIsInputBox;
			bReadOnlyOutputList = !bIsInputBox;
			
			UpdateAnchors();
		}
		
		public override string GetTypeName()
		{
			return "InputOutput";
		}
		
		public override string GetBoxKey()
		{
			return "InputOutputBox" + (bIsInputBox ? "Input" : "Output");
		}

		public override bool HandleContextMenu(GenericMenu GenericContextMenu)
		{
			return true;
		}
		
		public virtual LinkedEntityManager<EntityType> GetManager()
		{
			return null;
		}

		public override void GainedFocus()
		{
			InspectorPicker.AddHandler(GetTypeName(), EntityDrawInspectorWidgets);

			InspectorPicker.SelectInstance(GetTypeName(), this);
		}
		
		public override void OnInspectorGUIClickedSaveButton()
		{
	/*		if(WrappedInstance.GetFilename().Length == 0 && WrappedInstance.Title.Length > 0)
			{
				string UniqueName = GetUniqueFilename(WrappedInstance.Title);
				WrappedInstance.EditorSetFilename(UniqueName);
			}
			
			SaveEntities();

			Owner.SerializeBoxMetadata(true);*/
		}
		
		public override void EntityDrawInspectorWidgets(object Instance)
		{
			InputOutputBox<EntityType> EntityInst = (InputOutputBox<EntityType>)Instance;
			
			OnInspectorGUIDrawSaveButton(GetClassTypeSaveString());
			
			string TempBoxTitle = EntityInst.GetBoxTitle();
			
			InspectorGUIString("Title", ref TempBoxTitle, bReadOnlyTitle);
			
			if(bIsInputBox)
			{
				InspectorDrawStartWidgets();
			}
			else
			{
				InspectorDrawEndWidgets();
			}
		}
		
		public virtual void InspectorDrawStartWidgets()
		{
		}

		public virtual void InspectorDrawEndWidgets()
		{
		}
		
		public virtual List<EntityLink<EntityType>> GetStartEntities()
		{
			return null;
		}

		public virtual List<EntityLink<EntityType>> GetEndEntities()
		{
			return null;
		}

		public override void BuildConnectionsFromSourceData()
		{
			if(bIsInputBox)
			{
				List<EntityLink<EntityType>> OutputEvents = GetStartEntities();
				foreach(EntityLink<EntityType> CurrentLink in OutputEvents)
				{
					Anchor<EntityType> LocalAnchor = GetAnchor("Output" + CurrentLink.Name);
					
					if(LocalAnchor != null)
					{
						foreach(EntityLink<EntityType> CurrentRemoteLink in CurrentLink.LinkedEntities)
						{
							if(CurrentRemoteLink.GetOwner() != null)
							{
								if(CurrentRemoteLink.GetOwner().InputEvents.Contains(CurrentRemoteLink))
								{
									EntityBox<EntityType> RemoteBox = Owner.GetEntityBoxForEntity(CurrentRemoteLink.GetOwner());
									
									if(RemoteBox != null)
									{
										Anchor<EntityType> RemoteAnchor = RemoteBox.GetAnchor("Input" + CurrentRemoteLink.Name);
										
										if(RemoteAnchor != null)
										{
											Owner.ConnectInputToOutput(LocalAnchor, RemoteAnchor);
										}
									}
								}
							}
							else
							{
								EntityBox<EntityType> EndBox = Owner.GetOutputBox();

								if(EndBox != null)
								{
									Anchor<EntityType> RemoteAnchor = EndBox.GetAnchor("Input" + CurrentRemoteLink.Name);
									
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
			else
			{
				List<EntityLink<EntityType>> InputEvents = GetEndEntities();
				foreach(EntityLink<EntityType> CurrentLink in InputEvents)
				{
					Anchor<EntityType> LocalAnchor = GetAnchor("Input" + CurrentLink.Name);
					
					if(LocalAnchor != null)
					{
						foreach(EntityLink<EntityType> CurrentRemoteLink in CurrentLink.LinkedEntities)
						{
							if(CurrentRemoteLink.GetOwner() != null)
							{
								if(CurrentRemoteLink.GetOwner().OutputEvents.Contains(CurrentRemoteLink))
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
							else
							{
								EntityBox<EntityType> StartBox = Owner.GetInputBox();

								if(StartBox != null)
								{
									Anchor<EntityType> RemoteAnchor = StartBox.GetAnchor("Input" + CurrentRemoteLink.Name);
									
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
		}

		public override void FixOneWayLinks()
		{
			List<EntityLink<EntityType>> Events = null;

			if(bIsInputBox)
			{
				Events = GetStartEntities();
			}
			else
			{
				Events = GetEndEntities();
			}

			if(Events != null)
			{
				foreach(EntityLink<EntityType> CurrentConnector in Events)
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
}
