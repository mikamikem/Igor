#if IGOR_RUNTIME || UNITY_EDITOR
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace Igor
{
	public class EntityLink<EntityType> : XMLSerializable where EntityType : LinkedEntity<EntityType>, new() {
		
		[System.NonSerialized]
		private XMLSerializable Owner;
	/*	public static XMLSerializable SerializingOwner;
		public static string SerializingListPrefix;*/
		public static XMLSerializable.LinkType SerializingLinkType = XMLSerializable.LinkType.LINK_NormalLink;
		public string Name;
		public string ListPrefix = "";
		public LinkType CurrentLinkType;
		public List<EntityLink<EntityType> > LinkedEntities = new List<EntityLink<EntityType> >();
		protected List<string> LinkNamesToResolve = new List<string>();
		protected List<string> StartLinkNamesToResolve = new List<string>();
		protected List<string> EndLinkNamesToResolve = new List<string>();
		
		public EntityLink()
		{
			Name = "";
		}
		
		public IGraphEvent GetEventAndTriggerLink()
		{
			EntityType EntityOwner = GetOwner();
			if(EntityOwner != null)
			{
				return EntityOwner.GetEventAndTriggerLink(this);
			}

			return null;
		}
		
		public EntityType GetOwner()
		{
			if(typeof(EntityType).IsAssignableFrom(Owner.GetType()))
			{
				try
				{
					return (EntityType)Owner;
				}
				catch(System.Exception e)
				{
					e.ToString();
				}
			}
			
			return null;
		}
		
		public void SetOwner(XMLSerializable NewOwner)
		{
			Owner = NewOwner;
		}
		
		public virtual EntityLink<EntityType> GetValidLink()
		{
			foreach(EntityLink<EntityType> CurrentLink in LinkedEntities)
			{
				EntityType OwnerInst = CurrentLink.GetOwner();
				
				if(OwnerInst != null)
				{
					if(OwnerInst.MeetsRequirements())
					{
						return CurrentLink;
					}
				}
			}
			
			return null;
		}
		
		public override void SerializeXML()
		{
			base.SerializeXML();
			EntityType Temp = new EntityType();
	/*		Owner = EntityLink<EntityType>.SerializingOwner;
			ListPrefix = EntityLink<EntityType>.SerializingListPrefix;*/
			CurrentLinkType = EntityLink<EntityType>.SerializingLinkType;
			SerializeBegin(Temp.GetLinkTypeName());
			SerializeString("Name", ref Name);

			SerializeStringList<EntityLink<EntityType>>(Temp.GetLinkListName(), Temp.GetLinkTypeName() + "Name", LinkToLinkName, ref LinkedEntities);
		}
		
	#if UNITY_EDITOR
		public virtual EntityLink<EntityType> EditorDuplicateLink(LinkedEntity<EntityType> NewOwner)
		{
			EntityLink<EntityType> DupedLink = new EntityLink<EntityType>();
			
			DupedLink.Name = Name;
			DupedLink.ListPrefix = ListPrefix;
			DupedLink.Owner = NewOwner;
			DupedLink.CurrentLinkType = CurrentLinkType;
			
			return DupedLink;
		}
	#endif // UNITY_EDITOR

		public virtual string GetLinkTypeName()
		{
			return "EntityLink";
		}
		
		public virtual string GetLinkListName()
		{
			return "LinkedEntities";
		}
			
		public virtual void LinkToLinkName(ref EntityLink<EntityType> CurrentLink, ref string SavedLinkName)
		{
			if(CurrentLink != null)
			{
				SavedLinkName = CurrentLink.Owner.GetFilename() + ":-:" + CurrentLink.ListPrefix + CurrentLink.Name;
			}
			else if(SavedLinkName != null)
			{
				switch(CurrentLinkType)
				{
				case XMLSerializable.LinkType.LINK_StartLink:
					StartLinkNamesToResolve.Add(SavedLinkName);
					break;
				case XMLSerializable.LinkType.LINK_EndLink:
					EndLinkNamesToResolve.Add(SavedLinkName);
					break;
				case XMLSerializable.LinkType.LINK_NormalLink:
				default:
					LinkNamesToResolve.Add(SavedLinkName);
					break;
				}
			}
		}
		
		public delegate EntityType GetLinkedEntityForFilename(string Filename);
		public delegate LinkedEntityManager<EntityType> GetLinkedEntityManager();
		
		public virtual void FixupLinks(GetLinkedEntityForFilename GetEntityFunction, GetLinkedEntityManager GetManagerFunction)
		{
			FixupLinksWithList(GetEntityFunction, GetManagerFunction, ref LinkNamesToResolve, XMLSerializable.LinkType.LINK_NormalLink);
			FixupLinksWithList(GetEntityFunction, GetManagerFunction, ref StartLinkNamesToResolve, XMLSerializable.LinkType.LINK_StartLink);
			FixupLinksWithList(GetEntityFunction, GetManagerFunction, ref EndLinkNamesToResolve, XMLSerializable.LinkType.LINK_EndLink);
		}
		
		public virtual void FixupLinksWithList(GetLinkedEntityForFilename GetEntityFunction, GetLinkedEntityManager GetManagerFunction,
											   ref List<string> LookupList, LinkType NameType)
		{
			foreach(string CurrentLinkName in LookupList)
			{
				string LinkFilename = CurrentLinkName.Substring(0, CurrentLinkName.IndexOf(":-:"));
				string LinkName = CurrentLinkName.Substring(CurrentLinkName.IndexOf(":-:") + 3);
				if(NameType == XMLSerializable.LinkType.LINK_NormalLink)
				{
					EntityType Linked = GetEntityFunction(LinkFilename);
					
					if(Linked != null)
					{
						EntityLink<EntityType> DestinationLink = ((EntityType)Linked).GetLinkByName(LinkName);
						
						if(DestinationLink != null)
						{
							EstablishLink(DestinationLink);
						}
					}
				}
				else
				{
					LinkedEntityManager<EntityType> EntityManager = GetManagerFunction();
					
					if(EntityManager != null)
					{
						EntityManager.FixupStartEndLink(this, LinkFilename, LinkName, NameType);
					}
				}
			}
		}
			
		public virtual void EstablishLink(EntityLink<EntityType> OtherLink)
		{
			bool bFound = false;
			
			foreach(EntityLink<EntityType> CurrentLink in LinkedEntities)
			{
				if(CurrentLink == OtherLink)
				{
					bFound = true;
					break;
				}
			}
			
			if(!bFound)
			{
				LinkedEntities.Add(OtherLink);
			}
		}

		public virtual void BreakLink(EntityLink<EntityType> OtherLink)
		{
			LinkedEntities.Remove(OtherLink);
		}

		public virtual void BreakAllLinks()
		{
			for(int CurrentIndex = 0; CurrentIndex < LinkedEntities.Count; ++CurrentIndex)
			{
				EntityLink<EntityType> CurrentLink = LinkedEntities[CurrentIndex];
				
				CurrentLink.BreakLink(this);
			}
			
			LinkedEntities.Clear();
		}
	}
}

#endif // IGOR_RUNTIME || UNITY_EDITOR
