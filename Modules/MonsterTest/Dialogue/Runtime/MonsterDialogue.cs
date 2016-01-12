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
	public class MonsterDialogue : MonsterTestState {
		
		public static void RegisterType()
		{
			TypeUtils.RegisterType("MonsterDialogue", CreateNewMonsterDialogue, CreateMonsterDialogueSerializer);
		}
		
		public static object CreateNewMonsterDialogue()
		{
			return new MonsterDialogue();
		}
		
		public static XmlSerializer CreateMonsterDialogueSerializer()
		{
			return new XmlSerializer(typeof(MonsterDialogue));
		}

		public static MonsterDialogue DialogueInst = null;

		public bool bPreferExitOptions = false;
		public bool bAvoidExitOptions = false;

		public float WaitTimeLeft = -1.0f;

		public bool bGameFinished = false;
		public bool bProblemEncountered = false;
		public bool bNavigationRequired = false;

	#if UNITY_EDITOR
		public override LinkedEntity<MonsterTestBase> EditorDuplicate(LinkedEntity<MonsterTestBase> DerivedDuplicateInto = null)
		{
			MonsterDialogue DuplicateInto = (MonsterDialogue)DerivedDuplicateInto;
			
			if(DuplicateInto == null)
			{
				DuplicateInto = new MonsterDialogue();
			}
			
			return base.EditorDuplicate(DuplicateInto);
		}
	#endif // UNITY_EDITOR
		
		public override void CreateStaticNodesIfNotPresent()
		{
			base.CreateStaticNodesIfNotPresent();
			
			if(InputEvents.Count != 1)
			{
				InputEvents.Clear();

				EntityLink<MonsterTestBase> InputLink = new EntityLink<MonsterTestBase>();
				InputLink.SetOwner(this);
				InputLink.Name = "Start Dialogue";
				InputEvents.Add(InputLink);
			}
			if(OutputEvents.Count != 3)
			{
				EntityLink<MonsterTestBase> OutputLink = new EntityLink<MonsterTestBase>();
				OutputLink.SetOwner(this);
				OutputLink.Name = "Game Finished";
				OutputEvents.Add(OutputLink);

				OutputLink = new EntityLink<MonsterTestBase>();
				OutputLink.SetOwner(this);
				OutputLink.Name = "Problem Encountered";
				OutputEvents.Add(OutputLink);

				OutputLink = new EntityLink<MonsterTestBase>();
				OutputLink.SetOwner(this);
				OutputLink.Name = "Navigation Required";
				OutputEvents.Add(OutputLink);
			}
		}
		
		public override void TriggerLink(EntityLink<MonsterTestBase> InputLink)
		{
			base.TriggerLink(InputLink);
		}
		
		public override string GetEntityName()
		{
			return "MonsterDialogue";
		}

		public override string GetLinkListName()
		{
			return "LinkedMonsterDialogue";
		}
		
		public override bool ExecuteEvent()
		{
			base.ExecuteEvent();

			DialogueInst = this;

			WaitTimeLeft -= Time.deltaTime;

			return !bGameFinished && !bProblemEncountered && !bNavigationRequired;
		}

		public virtual int GetOutputIndex()
		{
			if(bGameFinished)
			{
				return 0;
			}
			else if(bProblemEncountered)
			{
				return 1;
			}
			else if(bNavigationRequired)
			{
				return 2;
			}

			return -1;
		}

		public override IGraphEvent GetNextEvent()
		{
			IGraphEvent NextEvent = null;

			int OutputIndex = GetOutputIndex();
			
			if(OutputIndex != -1 && OutputEvents.Count > OutputIndex && OutputEvents[OutputIndex].LinkedEntities.Count > 0)
			{
				EntityLink<MonsterTestBase> ValidLink = OutputEvents[OutputIndex].GetValidLink();
				
				if(ValidLink != null)
				{
					NextEvent = ValidLink.GetEventAndTriggerLink();
				}
			}
			
			if(NextEvent == null)
			{
				NextEvent = base.GetNextEvent();
			}

			return NextEvent;
		}
		
		public override EntityLink<MonsterTestBase> FindNextValidOutputLink()
		{
			int OutputIndex = GetOutputIndex();
			
			if(OutputEvents[OutputIndex].LinkedEntities.Count > 0)
			{
				return OutputEvents[OutputIndex].GetValidLink();
			}
			
			return base.FindNextValidOutputLink();
		}
		
		public override EntityLink<MonsterTestBase> FindNextOutputLink()
		{
			int OutputIndex = GetOutputIndex();
			
			if(OutputEvents[OutputIndex].LinkedEntities.Count > 0)
			{
				return OutputEvents[OutputIndex].LinkedEntities[0];
			}
			
			return base.FindNextOutputLink();
		}
		
		public override void SerializeXML()
		{
			base.SerializeXML();

			SerializeBool("bPreferExitOptions", ref bPreferExitOptions);
			SerializeBool("bAvoidExitOptions", ref bAvoidExitOptions);
		}

		public override bool IsEventImmediate()
		{
			return false;
		}

		public override void ResetEvent()
		{
			base.ResetEvent();

			DialogueInst = null;

			bGameFinished = false;
			bProblemEncountered = false;
			bNavigationRequired = false;
		}

		public virtual void NewLine(string LineID)
		{
			MonsterDebug.Log("New line \"" + LineID + "\"");

			WaitTimeLeft = Random.Range(1.0f, 2.0f);
		}

		public virtual void BrokenConnection()
		{
			MonsterDebug.LogError("There was a broken connection in the dialogue!");

			bProblemEncountered = true;
		}

		public virtual void NavigationRequired()
		{
			bNavigationRequired = true;
		}
	}
}

#endif // MONSTER_TEST_RUNTIME || UNITY_EDITOR
