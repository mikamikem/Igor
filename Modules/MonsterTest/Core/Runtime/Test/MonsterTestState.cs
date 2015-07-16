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
	public class MonsterTestState : MonsterTestBase {
		
		public static void RegisterType()
		{
			TypeUtils.RegisterType("MonsterTestState", CreateNewMonsterTestState, CreateMonsterTestStateSerializer);
		}
		
		public static object CreateNewMonsterTestState()
		{
			return new MonsterTestState();
		}
		
		public static XmlSerializer CreateMonsterTestStateSerializer()
		{
			return new XmlSerializer(typeof(MonsterTestState));
		}
		
	#if UNITY_EDITOR
		public override LinkedEntity<MonsterTestBase> EditorDuplicate(LinkedEntity<MonsterTestBase> DerivedDuplicateInto = null)
		{
			MonsterTestState DuplicateInto = (MonsterTestState)DerivedDuplicateInto;
			
			if(DuplicateInto == null)
			{
				DuplicateInto = new MonsterTestState();
			}
			
			return base.EditorDuplicate(DuplicateInto);
		}
	#endif // UNITY_EDITOR
		
		public override void CreateStaticNodesIfNotPresent()
		{
			base.CreateStaticNodesIfNotPresent();
			
			if(InputEvents.Count == 0)
			{
				EntityLink<MonsterTestBase> InputLink = new EntityLink<MonsterTestBase>();
				InputLink.SetOwner(this);
				InputLink.Name = "Input";
				InputEvents.Add(InputLink);
			}
	/*		if(OutputEvents.Count != NumberOfOutputs)
			{
				for(int CurrentLink = OutputEvents.Count; CurrentLink < NumberOfOutputs; ++CurrentLink)
				{
					EntityLink<MonsterTestBase> OutputLink = new EntityLink<MonsterTestBase>();
					OutputLink.SetOwner(this);
					OutputLink.Name = "Option " + CurrentLink;
					OutputEvents.Add(OutputLink);
				}
				
				for(int CurrentLink = NumberOfOutputs; CurrentLink < OutputEvents.Count;)
				{
					OutputEvents[CurrentLink].BreakAllLinks();
					OutputEvents.RemoveAt(CurrentLink);
				}
			}*/
		}
		
		public override string GetEntityName()
		{
			return "MonsterTestState";
		}

		public override string GetLinkListName()
		{
			return "LinkedMonsterTestState";
		}
		
		public override bool ExecuteEvent()
		{
			base.ExecuteEvent();
			
			return false;
		}

		public override IGraphEvent GetNextEvent()
		{
			IGraphEvent NextEvent = null;
			
	/*		if(RandomlyChosenOutput != -1 && OutputEvents.Count > RandomlyChosenOutput && OutputEvents[RandomlyChosenOutput].LinkedEntities.Count > 0)
			{
				EntityLink<ConversationBase> ValidLink = OutputEvents[RandomlyChosenOutput].GetValidLink();
				
				if(ValidLink != null)
				{
					NextEvent = ValidLink.GetEventAndTriggerLink();
				}
			}
			
			if(NextEvent == null)
			{
				NextEvent = base.GetNextEvent();
			}*/
			
			return NextEvent;
		}
		
		public override EntityLink<MonsterTestBase> FindNextValidOutputLink()
		{
	/*		if(OutputEvents[RandomlyChosenOutput].LinkedEntities.Count > 0)
			{
				return OutputEvents[RandomlyChosenOutput].GetValidLink();
			}*/
			
			return base.FindNextValidOutputLink();
		}
		
		public override EntityLink<MonsterTestBase> FindNextOutputLink()
		{
	/*		if(OutputEvents[RandomlyChosenOutput].LinkedEntities.Count > 0)
			{
				return OutputEvents[RandomlyChosenOutput].LinkedEntities[0];
			}*/
			
			return base.FindNextOutputLink();
		}
		
		public override void SerializeXML()
		{
			base.SerializeXML();
		}

		public override bool IsEventImmediate()
		{
			return false;
		}
	}
}

#endif // MONSTER_TEST_RUNTIME || UNITY_EDITOR
