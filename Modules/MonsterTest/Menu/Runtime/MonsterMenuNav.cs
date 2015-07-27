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
	public class MonsterMenuNav : MonsterTestState {
		
		public static void RegisterType()
		{
			TypeUtils.RegisterType("MonsterMenuNav", CreateNewMonsterMenuNav, CreateMonsterMenuNavSerializer);
		}
		
		public static object CreateNewMonsterMenuNav()
		{
			return new MonsterMenuNav();
		}
		
		public static XmlSerializer CreateMonsterMenuNavSerializer()
		{
			return new XmlSerializer(typeof(MonsterMenuNav));
		}

		public static IMonsterMenuRunner MenuRunnerInst = null;

		public bool bNavigatingMenu = false;

		public bool bShouldStartNewGame = false;
		public bool bShouldLoadGame = false;
		public bool bShouldQuit = false;

		public bool bStartedSuccessfully = false;
		public bool bFailedToStart = false;
		public bool bQuit = false;

		public float TimeToWait = 600.0f;
		public float TimeLeft = -100.0f;
		
	#if UNITY_EDITOR
		public override LinkedEntity<MonsterTestBase> EditorDuplicate(LinkedEntity<MonsterTestBase> DerivedDuplicateInto = null)
		{
			MonsterMenuNav DuplicateInto = (MonsterMenuNav)DerivedDuplicateInto;
			
			if(DuplicateInto == null)
			{
				DuplicateInto = new MonsterMenuNav();
			}
			
			return base.EditorDuplicate(DuplicateInto);
		}
	#endif // UNITY_EDITOR
		
		public override void CreateStaticNodesIfNotPresent()
		{
			base.CreateStaticNodesIfNotPresent();
			
			if(InputEvents.Count != 3)
			{
				InputEvents.Clear();

				EntityLink<MonsterTestBase> InputLink = new EntityLink<MonsterTestBase>();
				InputLink.SetOwner(this);
				InputLink.Name = "Start New Game";
				InputEvents.Add(InputLink);

				InputLink = new EntityLink<MonsterTestBase>();
				InputLink.SetOwner(this);
				InputLink.Name = "Load Saved Game";
				InputEvents.Add(InputLink);

				InputLink = new EntityLink<MonsterTestBase>();
				InputLink.SetOwner(this);
				InputLink.Name = "Quit";
				InputEvents.Add(InputLink);
			}
			if(OutputEvents.Count != 3)
			{
				EntityLink<MonsterTestBase> OutputLink = new EntityLink<MonsterTestBase>();
				OutputLink.SetOwner(this);
				OutputLink.Name = "Game Started";
				OutputEvents.Add(OutputLink);

				OutputLink = new EntityLink<MonsterTestBase>();
				OutputLink.SetOwner(this);
				OutputLink.Name = "Game Failed To Start";
				OutputEvents.Add(OutputLink);

				OutputLink = new EntityLink<MonsterTestBase>();
				OutputLink.SetOwner(this);
				OutputLink.Name = "Quit";
				OutputEvents.Add(OutputLink);
			}
		}
		
		public override void TriggerLink(EntityLink<MonsterTestBase> InputLink)
		{
			base.TriggerLink(InputLink);
			
			if(InputLink.Name == "Start New Game")
			{
				bShouldStartNewGame = true;
			}
			else if(InputLink.Name == "Load Saved Game")
			{
				bShouldLoadGame = true;
			}
			else if(InputLink.Name == "Quit")
			{
				bShouldQuit = true;
			}
		}
		
		public override string GetEntityName()
		{
			return "MonsterMenuNav";
		}

		public override string GetLinkListName()
		{
			return "LinkedMonsterMenuNav";
		}
		
		public override bool ExecuteEvent()
		{
			base.ExecuteEvent();

			if(TimeLeft == -100.0f)
			{
				TimeLeft = TimeToWait;
			}

			if(MenuRunnerInst != null)
			{
				MenuRunnerInst.MonsterRunMenu(this);
			}
			else if(!bNavigatingMenu)
			{
				TimeLeft -= Time.deltaTime;

				if(TimeLeft < 0.0f)
				{
					bFailedToStart = true;
				}
			}
			
			return !bStartedSuccessfully && !bFailedToStart && !bQuit;
		}

		public virtual int GetOutputIndex()
		{
			if(bStartedSuccessfully)
			{
				return 0;
			}
			else if(bFailedToStart)
			{
				return 1;
			}
			else if(bQuit)
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

			SerializeFloat("TimeToWait", ref TimeToWait);
		}

		public override bool IsEventImmediate()
		{
			return false;
		}

		public override void ResetEvent()
		{
			base.ResetEvent();

			bShouldStartNewGame = false;
			bShouldLoadGame = false;
			bShouldQuit = false;
		}
	}
}

#endif // MONSTER_TEST_RUNTIME || UNITY_EDITOR
