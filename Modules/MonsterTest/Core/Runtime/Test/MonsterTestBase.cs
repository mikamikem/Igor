#if MONSTER_TEST_RUNTIME || UNITY_EDITOR
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Xml.Serialization;
#if UNITY_EDITOR
using UnityEditor;
#endif // UNITY_EDITOR

namespace Igor
{
	[System.Serializable]
	#if UNITY_EDITOR
	[InitializeOnLoad]
	#endif // UNITY_EDITOR
	public class MonsterTestBase : LinkedEntity<MonsterTestBase> {

		public static void RegisterType()
		{
			TypeUtils.RegisterType("MonsterTestBase", CreateNewMonsterTestBase, CreateMonsterTestBaseSerializer);
		}

		public static object CreateNewMonsterTestBase()
		{
			return new MonsterTestBase();
		}
		
		public static XmlSerializer CreateMonsterTestBaseSerializer()
		{
			return new XmlSerializer(typeof(MonsterTestBase));
		}
		
	#if UNITY_EDITOR
		public override LinkedEntity<MonsterTestBase> EditorDuplicate(LinkedEntity<MonsterTestBase> DerivedDuplicateInto = null)
		{
			LinkedEntity<MonsterTestBase> DuplicateInto = DerivedDuplicateInto;
			
			if(DuplicateInto == null)
			{
				DuplicateInto = new MonsterTestBase();
			}
			
			return base.EditorDuplicate(DuplicateInto);
		}
		
		public override EntityID GenerateEntityIDForEvent()
		{
	/*		EntityID NewID = new EntityID();
			
			NewID.ConversationFilename = Filename;
			
			DialogueBase DialogueOwner = DialogueManager.GetActiveInstance().GetDialogueForConversation(this);
			
			if(DialogueOwner != null)
			{
				NewID.DialogueFilename = DialogueOwner.GetFilename();
			}
			
			if(DialogueOwner != null && DialogueManager.GetActiveInstance().GetEntityForID(NewID) != null)
			{
				EntityID DialogueID = DialogueOwner.GenerateEntityIDForEvent();
				
				if(DialogueID != null)
				{
					NewID.ChapterFilename = DialogueID.ChapterFilename;
					NewID.SceneFilename = DialogueID.SceneFilename;
					
					return NewID;
				}
			}

			if(Filename.IndexOf("_-_", 3) > 0)
			{
				NewID.ChapterFilename = Filename.Substring(3, Filename.IndexOf("_-_", 3) - 3);

				int NewFirstIndex = NewID.ChapterFilename.Length + 6;

				NewID.SceneFilename = Filename.Substring(NewFirstIndex, Filename.IndexOf("_-_", NewFirstIndex) - NewFirstIndex);

				NewFirstIndex += NewID.SceneFilename.Length + 3;

				int StartOfConvoName = NewFirstIndex;
				int LastDividerIndex = Filename.LastIndexOf('_');
				int LastLength = 0;

				while(StartOfConvoName < LastDividerIndex)
				{
					NewID.DialogueFilename = Filename.Substring(NewFirstIndex, Filename.IndexOf("_", StartOfConvoName) - NewFirstIndex);

					IGraphEvent TestEvent = NewID.GetEntity();

					if(TestEvent != null && typeof(ConversationBase).IsAssignableFrom(TestEvent.GetType()) && ((ConversationBase)TestEvent).GetFilename() == this.GetFilename())
					{
						return NewID;
					}

					StartOfConvoName += NewID.DialogueFilename.Length + 1 - LastLength;

					LastLength = NewID.DialogueFilename.Length;
				}
			}

			NewID = ChapterManager.GetInstance().GetEntityIDFullEnumeration(this);

			if(NewID.ConversationFilename != "")
			{
				return NewID;
			}

			return null;*/
			return null;
		}

		public override void SetModifiedState(ModifiedState NewState, string VariableName)
		{
			if(NewState == ModifiedState.PreModify)
			{
			}

			base.SetModifiedState(NewState, VariableName);

			if(NewState == ModifiedState.Modified)
			{
				if(MonsterTestManager.GetActiveInstance() != null)
				{
					MonsterTestManager.GetActiveInstance().SaveEntities();
				}
			}
		}
		
		public virtual bool EditorShouldExport()
		{
			return false;
		}
	#endif // UNITY_EDITOR
		
		public class MonsterTestLink : EntityLink<MonsterTestBase>
		{
			public override string GetLinkTypeName()
			{
				return "MonsterTestLink";
			}
			
			public override string GetLinkListName()
			{
				return "MonsterTestLinks";
			}
		};
		
		public override IGraphEvent GetNextEvent()
		{
			IGraphEvent NextEvent = base.GetNextEvent();
			
	/*		if(NextEvent == null)
			{
				NextEvent = DialogueManager.GetActiveInstance().GetNextEventAfterCurrentDialogue(FindNextOutputLink());
			}*/
			
			return NextEvent;
		}
		
		public override string GetEntityName()
		{
			return "MonsterTestBase";
		}

		public override string GetLinkTypeName()
		{
			return "MonsterTestLink";
		}
		
		public override string GetLinkListName()
		{
			return "LinkedMonsterTestBase";
		}
		
		public override MonsterTestBase GetLinkedEntityForFilename(string Filename)
		{
			return MonsterTestManager.GetActiveInstance().GetEntityByFileName(Filename);
		}

		public override LinkedEntityManager<MonsterTestBase> GetEntityManager()
		{
			return MonsterTestManager.GetActiveInstance();
		}
		
	#if UNITY_EDITOR
		public override IGraphEvent[] GetAllNextPossibleEvents()
		{
			IGraphEvent[] BaseEvents = base.GetAllNextPossibleEvents();
			
			if(BaseEvents.Length == 0 || BaseEvents[0] == null)
			{
				List<IGraphEvent> NextConversation = new List<IGraphEvent>();
				NextConversation.Add(GetNextEvent());
				if(BaseEvents.Length > 1)
				{
					NextConversation.AddRange(BaseEvents);
					NextConversation.RemoveAt(1);
				}
				
				return NextConversation.ToArray();
			}
			
			return BaseEvents;
		}
		
		public override string GetLocationString()
		{
			return EditorGetNodeFullLocationName();
		}

		public virtual string EditorGetNodeFullLocationName()
		{
			EntityID CurrentID = GenerateEntityIDForEvent();

			if(CurrentID != null)
			{
				return CurrentID.ChapterFilename + " - " + CurrentID.SceneFilename + " - " + CurrentID.DialogueFilename;
			}
			else
			{
				return "";
			}
		}
	#endif // UNITY_EDITOR
	}
}

#endif // MONSTER_TEST_RUNTIME || UNITY_EDITOR
