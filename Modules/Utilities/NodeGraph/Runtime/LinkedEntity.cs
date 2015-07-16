#if IGOR_RUNTIME || UNITY_EDITOR
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace Igor
{
	public class LinkedEntity<EntityType> : XMLSerializable, IGraphEvent
	//														where EntityLinkType : EntityLink<EntityType>, new()
															where EntityType : LinkedEntity<EntityType>, new() {

		public string Title = "";
		
		[System.NonSerialized]
		public List<EntityLink<EntityType>> InputEvents = new List<EntityLink<EntityType>>();
		[System.NonSerialized]
		public List<EntityLink<EntityType>> OutputEvents = new List<EntityLink<EntityType>>();
		
	#if UNITY_EDITOR
		public virtual void EditorUpdateDataFrom(LinkedEntity<EntityType> Other)
		{
			Title = Other.Title;
			Filename = Other.Filename;
			InputEvents = Other.InputEvents;
			OutputEvents = Other.OutputEvents;
		}
		
		public virtual LinkedEntity<EntityType> EditorDuplicate(LinkedEntity<EntityType> DerivedDuplicateInto = null)
		{
			LinkedEntity<EntityType> DuplicateInto = DerivedDuplicateInto;
			
			if(DuplicateInto == null)
			{
				DuplicateInto = new LinkedEntity<EntityType>();
			}
			
			DuplicateInto.Title = Title;
			DuplicateInto.Filename = "";
			
			DuplicateInto.InputEvents = new List<EntityLink<EntityType>>();
			
			foreach(EntityLink<EntityType> CurrentLink in InputEvents)
			{
				EntityLink<EntityType> DuplicatedLink = CurrentLink.EditorDuplicateLink(DuplicateInto);
				
				DuplicateInto.InputEvents.Add(DuplicatedLink);
			}
			
			DuplicateInto.OutputEvents = new List<EntityLink<EntityType>>();
			
			foreach(EntityLink<EntityType> CurrentLink in OutputEvents)
			{
				EntityLink<EntityType> DuplicatedLink = CurrentLink.EditorDuplicateLink(DuplicateInto);
				
				DuplicateInto.OutputEvents.Add(DuplicatedLink);
			}
			
			return DuplicateInto;
		}
		
		public virtual List<EntityLink<EntityType>> EditorGetInputEvents()
		{
			return InputEvents;
		}
		
		public virtual EntityID GenerateEntityIDForEvent()
		{
			return new EntityID();
		}
	#endif // UNITY_EDITOR
		
		public virtual bool ExecuteEvent()
		{
			return false;
		}
		
		public virtual bool IsNextEventImmediate()
		{
			if(FindNextValidOutputLink() != null &&
			   FindNextValidOutputLink().GetOwner() != null)
			{
				return FindNextValidOutputLink().GetOwner().IsEventImmediate();
			}
			
			return false;
		}
		
		public virtual bool IsEventImmediate()
		{
			return false;
		}
		
		public virtual void ResetEvent()
		{
		}
		
	#if UNITY_EDITOR
		public virtual IGraphEvent[] GetAllNextPossibleEvents()
		{
			List<IGraphEvent> OutputGameEvents = new List<IGraphEvent>();
			
			for(int CurrentOutput = 0; CurrentOutput < OutputEvents.Count; ++CurrentOutput)
			{
				for(int CurrentLink = 0; CurrentLink < OutputEvents[CurrentOutput].LinkedEntities.Count; ++CurrentLink)
				{
					OutputGameEvents.Add(OutputEvents[CurrentOutput].LinkedEntities[CurrentLink].GetEventAndTriggerLink());
				}
			}
			
			return OutputGameEvents.ToArray();
		}
		
		public virtual void SetModifiedState(ModifiedState NewState, string VariableName)
		{
			if(NewState != ModifiedState.Unmodified && NewState != ModifiedState.PreModify)
			{
				Debug.Log("We've " + NewState.ToString() + " " + Title + "." + VariableName);
			}
		}

		public virtual string GetLocationString()
		{
			return "";
		}
	#endif // UNITY_EDITOR
		
		public virtual IGraphEvent GetNextEvent()
		{
			IGraphEvent NextEvent = null;
			EntityLink<EntityType> NextLink = FindNextValidOutputLink();
			
			if(NextLink != null)
			{
				NextEvent = NextLink.GetEventAndTriggerLink();
			}
			
			return NextEvent;
		}
		
		public virtual IGraphEvent GetEventAndTriggerLink(EntityLink<EntityType> InputLink)
		{
			TriggerLink(InputLink);
			
			return this;
		}
		
		public virtual IGraphEvent GetDefaultTriggeredEvent()
		{
			if(InputEvents.Count > 0)
			{
				return GetEventAndTriggerLink(InputEvents[0]);
			}
			
			return null;
		}
		
		public virtual void TriggerLink(EntityLink<EntityType> InputLink)
		{
		}
		
		public virtual EntityLink<EntityType> FindNextValidOutputLink()
		{
			for(int CurrentOutput = 0; CurrentOutput < OutputEvents.Count; ++CurrentOutput)
			{
				if(OutputEvents[CurrentOutput].LinkedEntities.Count > 0)
				{
					return OutputEvents[CurrentOutput].GetValidLink();
				}
			}
			
			return null;
		}
		
		public virtual EntityLink<EntityType> FindNextOutputLink()
		{
			for(int CurrentOutput = 0; CurrentOutput < OutputEvents.Count; ++CurrentOutput)
			{
				if(OutputEvents[CurrentOutput].LinkedEntities.Count > 0)
				{
					return OutputEvents[CurrentOutput].LinkedEntities[0];
				}
			}
			
			return null;
		}
		
		public virtual bool MeetsRequirements()
		{
			return true;
		}
		
		public override void SerializeListOwnerPrefixFixup()
		{
			base.SerializeListOwnerPrefixFixup();
			
			ListFixup<EntityType>(ref InputEvents, (EntityType)this, "Input", XMLSerializable.LinkType.LINK_NormalLink);
			ListFixup<EntityType>(ref OutputEvents, (EntityType)this, "Output", XMLSerializable.LinkType.LINK_NormalLink);
		}

		public override void SerializeXML()
		{
			base.SerializeXML();
			SerializeBegin(GetEntityName());
			
			SerializeString("Title", ref Title);
			SerializeString("Filename", ref Filename);
			
			SerializeLinkLists();
		}
		
		public virtual string GetEntityName()
		{
			return "LinkedEntity";
		}
		
		public virtual string GetLinkTypeName()
		{
			return "EntityLink";
		}
		
		public virtual string GetLinkListName()
		{
			return "LinkedEntities";
		}
		
		public virtual void SerializeLinkLists()
		{
			SerializeListEmbedded<EntityLink<EntityType>>("InputEvents", GetLinkTypeName(), ref InputEvents);
			SerializeListEmbedded<EntityLink<EntityType>>("OutputEvents", GetLinkTypeName(), ref OutputEvents);
		}
			
		public virtual EntityLink<EntityType> GetLinkByName(string LinkName)
		{
			if(LinkName.StartsWith("Input"))
			{
				string LookupName = LinkName.Substring(5);
				foreach(EntityLink<EntityType> CurrentLink in InputEvents)
				{
					if(CurrentLink.Name == LookupName)
					{
						return CurrentLink;
					}
				}
			}
			else if(LinkName.StartsWith("Output"))
			{
				string LookupName = LinkName.Substring(6);
				foreach(EntityLink<EntityType> CurrentLink in OutputEvents)
				{
					if(CurrentLink.Name == LookupName)
					{
						return CurrentLink;
					}
				}
			}
			
			return null;
		}
		
		public virtual EntityType GetLinkedEntityForFilename(string Filename)
		{
			return null;
		}
		
		public virtual LinkedEntityManager<EntityType> GetEntityManager()
		{
			return null;
		}
		
		public virtual void FixupLinks()
		{
			foreach(EntityLink<EntityType> CurrentLink in InputEvents)
			{
				CurrentLink.FixupLinks(GetLinkedEntityForFilename, GetEntityManager);
			}
			foreach(EntityLink<EntityType> CurrentLink in OutputEvents)
			{
				CurrentLink.FixupLinks(GetLinkedEntityForFilename, GetEntityManager);
			}
		}
		
	}
}

#endif // IGOR_RUNTIME || UNITY_EDITOR
