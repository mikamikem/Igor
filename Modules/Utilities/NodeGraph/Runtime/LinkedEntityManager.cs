#if IGOR_RUNTIME || UNITY_EDITOR
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System;
using System.Xml.Serialization;

namespace Igor
{
	[System.Serializable]
	public class LinkedEntityManager<EntityType> : XMLSerializable where EntityType : LinkedEntity<EntityType>, new() {
		
		[System.NonSerialized]
		public List<EntityLink<EntityType>> StartingEntities = new List<EntityLink<EntityType>>();
		[System.NonSerialized]
		public List<EntityLink<EntityType>> EndingEntities = new List<EntityLink<EntityType>>();
		
		public LinkedEntityManager() : base()
		{
		}

		public virtual void LoadEntities()
		{
			SerializeEntityManager(false);
		}
		
		public virtual void SaveEntities()
		{
			SerializeEntityManager(true);
			SerializeAllEntities(true);
		}
		
	#if UNITY_EDITOR
		public virtual void AddEntity(EntityType NewEntity)
		{
			bool bHasFilenameAlready = false;
			
			foreach(EntityType CurrentEntity in Entities)
			{
				if(CurrentEntity.GetFilename() == NewEntity.GetFilename())
				{
					bHasFilenameAlready = true;
				}
			}
			
			if(bHasFilenameAlready)
			{
				NewEntity.EditorSetFilename(EditorGetUniqueEntityFilename(NewEntity.GetFilename()));
				
				AddEntity(NewEntity);
			}
			else
			{
				Entities.Add(NewEntity);
			}
		}

		public virtual void RemoveEntity(EntityType EntityToRemove)
		{
			for(int CurrentIndex = 0; CurrentIndex < Entities.Count; ++CurrentIndex)
			{
				EntityType CurrentEntity = Entities[CurrentIndex];

				if(CurrentEntity.GetFilename() == EntityToRemove.GetFilename())
				{
					Entities.RemoveAt(CurrentIndex);

					break;
				}
			}
		}
		
		public virtual List<EntityType> EditorGetEntityList()
		{
			return Entities;
		}
		
		public virtual string EditorGetUniqueEntityFilename(string OriginalFilename)
		{
			string BaseFilePath = GetDefaultEntityPath();
			// The XML we use for some of the metadata on the editor side doesn't like spaces so we need to swap them out
			string TestFile = OriginalFilename.Replace(" ", "_");
			int CurrentIndex = 0;
			
			while(File.Exists(BaseFilePath + TestFile + ".xml"))
			{
				TestFile = OriginalFilename + "-" + (++CurrentIndex);
			}
			
			return TestFile;
		}
	#endif // UNITY_EDITOR
		
		protected List<EntityType> Entities = new List<EntityType>();
		
		public virtual int GetEntityCount()
		{
			return Entities.Count;
		}
		
		public virtual string GetEntityListFilename()
		{
			return "Assets/Resources/EntityList.xml";
		}
		
		public virtual string GetDefaultEntityPath()
		{
			return "Assets/Resources/Entities/";
		}
		
		protected virtual string GetManagerName()
		{
			return "LinkedEntityManager";
		}
		
		protected virtual string GetEntityListName()
		{
			return "EntityList";
		}
		
		protected virtual string GetEntityElementName()
		{
			return "LinkedEntityFilename";
		}
		
		protected virtual void SerializeEntityManager(bool bSaving)
		{
			if(bSaving)
			{
				SerializeListOwnerPrefixFixup();
			}
			
			LinkedEntityManager<EntityType> Instance = new LinkedEntityManager<EntityType>();
			XMLSerializable.SerializeFromXML<LinkedEntityManager<EntityType>>(GetEntityListFilename(), ref Instance, bSaving);
		}

		public override void SerializeListOwnerPrefixFixup()
		{
			base.SerializeListOwnerPrefixFixup();
			
			ListFixup<EntityType>(ref StartingEntities, this, "Input", XMLSerializable.LinkType.LINK_StartLink);
			ListFixup<EntityType>(ref EndingEntities, this, "Output", XMLSerializable.LinkType.LINK_EndLink);
			
			for(int CurrentEntity = 0; CurrentEntity < Entities.Count; ++CurrentEntity)
			{
				Entities[CurrentEntity].SerializeListOwnerPrefixFixup();
			}
		}

		public override void SerializeXML()
		{
			base.SerializeXML();
			SerializeBegin(GetManagerName());
		
			SerializeEntityList();
			
			SerializeBeginEndNodes();
		}
		
		public virtual void SerializeEntityList()
		{
			SerializeStringList<EntityType>(GetEntityListName(), GetEntityElementName(), EntityToFilename, ref Entities);
		}
		
		public virtual void SerializeBeginEndNodes()
		{
			EntityLink<EntityType>.SerializingLinkType = XMLSerializable.LinkType.LINK_StartLink;
			SerializeListEmbedded<EntityLink<EntityType>>("StartNodes", "StartNode", ref StartingEntities);
			EntityLink<EntityType>.SerializingLinkType = XMLSerializable.LinkType.LINK_EndLink;
			SerializeListEmbedded<EntityLink<EntityType>>("EndNodes", "EndNode", ref EndingEntities);
			EntityLink<EntityType>.SerializingLinkType = XMLSerializable.LinkType.LINK_NormalLink;
		}
		
		public virtual void CreateAndAddNewStartEndNodeConnection(string ConnectionName, XMLSerializable.LinkType NodeType)
		{
			if(NodeType == XMLSerializable.LinkType.LINK_StartLink)
			{
				EntityLink<EntityType> StartLink = new EntityLink<EntityType>();
				StartLink.CurrentLinkType = XMLSerializable.LinkType.LINK_StartLink;
				StartLink.Name = ConnectionName;
				StartingEntities.Add(StartLink);
			}
			else if(NodeType == XMLSerializable.LinkType.LINK_EndLink)
			{
				EntityLink<EntityType> EndLink = new EntityLink<EntityType>();
				EndLink.CurrentLinkType = XMLSerializable.LinkType.LINK_EndLink;
				EndLink.Name = ConnectionName;
				EndingEntities.Add(EndLink);
			}
		}
		
		public virtual void ReplicateList<OtherType>(ref List<EntityLink<OtherType>> Source, ref List<EntityLink<EntityType>> Destination, XMLSerializable.LinkType ListType)
																															where OtherType : LinkedEntity<OtherType>, new()
		{
			List<EntityLink<EntityType>> NodesToProcess = new List<EntityLink<EntityType>>();
			
			for(int CurrentLocalNode = 0; CurrentLocalNode < Destination.Count; ++CurrentLocalNode)
			{
				bool bFound = false;
				
				for(int CurrentNode = 0; CurrentNode < Source.Count; ++CurrentNode)
				{
					if(Source[CurrentNode].Name == Destination[CurrentLocalNode].Name)
					{
						bFound = true;
						break;
					}
				}
				
				if(!bFound)
				{
					NodesToProcess.Add(Destination[CurrentLocalNode]);
					Destination.RemoveAt(CurrentLocalNode);
					--CurrentLocalNode;
				}
			}
			
			for(int CurrentNode = 0; CurrentNode < NodesToProcess.Count; ++CurrentNode)
			{
				BreakLinksToLink(NodesToProcess[CurrentNode]);
			}

			for(int CurrentNode = 0; CurrentNode < Source.Count; ++CurrentNode)
			{
				bool bFound = false;
				
				for(int CurrentLocalNode = 0; CurrentLocalNode < Destination.Count; ++CurrentLocalNode)
				{
					if(Destination[CurrentLocalNode].Name == Source[CurrentNode].Name)
					{
						bFound = true;
						break;
					}
				}
				
				if(!bFound)
				{
					CreateAndAddNewStartEndNodeConnection(Source[CurrentNode].Name, ListType);
				}
			}
		}
		
		public virtual void BreakLinksToLink(EntityLink<EntityType> SourceLink)
		{
			for(int CurrentRemoteLink = 0; CurrentRemoteLink < SourceLink.LinkedEntities.Count; ++CurrentRemoteLink)
			{
				SourceLink.LinkedEntities[CurrentRemoteLink].BreakLink(SourceLink);
			}
		}
		
		public override void CreateStaticNodesIfNotPresent()
		{
			base.CreateStaticNodesIfNotPresent();
		}
		
		public virtual void EntityToFilename(ref EntityType SourceEntity, ref string SavedFilename)
		{
			if(SourceEntity != null)
			{
				SavedFilename = SourceEntity.GetEntityName() + ":=:" + SourceEntity.GetFilename();
			}
			else if(SavedFilename != null)
			{
				string[] Delimiters = { ":=:" };
				string[] NameTypeString = SavedFilename.Split(Delimiters, System.StringSplitOptions.None);
				string TypeString = NameTypeString[0];
				string NameString = NameTypeString[1];
				
				CreateOrUpdateEntityFromFilename(NameString, ref Entities, TypeUtils.GetNewObjectOfTypeString(TypeString), TypeUtils.GetXMLSerializerForTypeString(TypeString));
			}
		}
		
		protected virtual void CreateOrUpdateEntityFromFilename(string EntityFilename, ref List<EntityType> ListToConvert, object NewObject, XmlSerializer NewSerializer)
		{
			for(int CurrentEntityIndex = 0; CurrentEntityIndex < ListToConvert.Count; ++CurrentEntityIndex)
			{
				EntityType CurrentEntity = ListToConvert[CurrentEntityIndex];
				if(CurrentEntity.GetFilename() == EntityFilename)
				{
					EntityType SerializedEntity = (EntityType)NewObject;
					
					if(SerializedEntity != null)
					{
						SerializeEntity(EntityFilename, ref SerializedEntity, false, NewSerializer);
	#if UNITY_EDITOR
						ListToConvert[CurrentEntityIndex].EditorUpdateDataFrom(SerializedEntity);
	#endif // UNITY_EDITOR
					}
					
					return;
				}
			}
			
			EntityType NewEntity = (EntityType)NewObject;
			
			if(NewEntity != null)
			{
				SerializeEntity(EntityFilename, ref NewEntity, false, NewSerializer);
				ListToConvert.Add(NewEntity);
			}
		}
		
		protected virtual void SerializeAllEntities(bool bSaving)
		{
			if(bSaving)
			{
				for(int CurrentEntity = 0; CurrentEntity < Entities.Count; ++CurrentEntity)
				{
					EntityType CurrentCopy = Entities[CurrentEntity];
					SerializeEntity(CurrentCopy.GetFilename(), ref CurrentCopy, true, TypeUtils.GetXMLSerializerForTypeString(CurrentCopy.GetEntityName()));
				}
			}
		}
		
		protected virtual void SerializeEntity(string EntityFilename, ref EntityType ToSerialize, bool bSaving, XmlSerializer Serializer)
		{
			XMLSerializable.SerializeFromXML<EntityType>(GetDefaultEntityPath() + EntityFilename + ".xml", ref ToSerialize, bSaving, Serializer);
		}
		
		public virtual EntityType GetEntityByFileName(string FileName)
		{
			foreach(EntityType CurrentEntity in Entities)
			{
				if(CurrentEntity.GetFilename() == FileName)
				{
					return CurrentEntity;
				}
			}
			
			return null;
		}
		
		public override void PostSerialize()
		{
			base.PostSerialize();
			
			if(bReading)
			{
				SerializeListOwnerPrefixFixup();
				
				foreach(EntityType CurrentEntity in Entities)
				{
					CurrentEntity.FixupLinks();
				}
			}
		}
		
		public virtual void FixupStartEndLink(EntityLink<EntityType> SourceLink, string DestLinkFilename, string DestLinkName, XMLSerializable.LinkType TypeName)
		{
			EntityType DestEntity = GetEntityByFileName(DestLinkFilename);
			
			if(DestEntity != null)
			{
				EntityLink<EntityType> DestinationLink = ((EntityType)DestEntity).GetLinkByName(DestLinkName);
				
				if(DestinationLink != null)
				{
					SourceLink.EstablishLink(DestinationLink);
					DestinationLink.EstablishLink(SourceLink);
				}
			}
			else
			{
				string InvertLinkType = (DestLinkName.StartsWith("Input") ? DestLinkName.Replace("Input", "Output") : DestLinkName.Replace("Output", "Input"));

				EntityLink<EntityType> DestinationLink = GetLinkByName(InvertLinkType);

				if(DestinationLink != null)
				{
					SourceLink.EstablishLink(DestinationLink);
					DestinationLink.EstablishLink(SourceLink);
				}
			}
		}
		
		public virtual EntityLink<EntityType> GetLinkByName(string LinkName)
		{
			if(LinkName.StartsWith("Input"))
			{
				string LookupName = LinkName.Substring(5);
				foreach(EntityLink<EntityType> CurrentLink in EndingEntities)
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
				foreach(EntityLink<EntityType> CurrentLink in StartingEntities)
				{
					if(CurrentLink.Name == LookupName)
					{
						return CurrentLink;
					}
				}
			}
			
			return null;
		}
	}
}

#endif // IGOR_RUNTIME || UNITY_EDITOR
