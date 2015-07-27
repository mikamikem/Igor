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
	public class MonsterTest : LinkedEntity<MonsterTest> {

		public bool bAllowRunningInEditor = true;
		public bool bStartGameInEditor = true;
		public bool bForceLoadToFirstSceneInEditor = false;
		
		public static void RegisterType()
		{
			TypeUtils.RegisterType("MonsterTest", CreateNewMonsterTest, CreateMonsterTestSerializer);
		}
		
		public static object CreateNewMonsterTest()
		{
			return new MonsterTest();
		}
		
		public static XmlSerializer CreateMonsterTestSerializer()
		{
			return new XmlSerializer(typeof(MonsterTest));
		}

		public static MonsterTest LoadMonsterTest(string Filename)
		{
			MonsterTest LoadedInstance = new MonsterTest();

			XMLSerializable.SerializeFromXML<MonsterTest>(GetFullPathFromFilename(Filename), ref LoadedInstance, false);
			
			return LoadedInstance;
		}

		public static string GetFullPathFromFilename(string Filename)
		{
			return MonsterTestCore.MonsterLocalDirectoryRoot + "/Config/Tests/" + Filename + ".xml";
		}
		
	#if UNITY_EDITOR
		public virtual void EditorSaveMonsterTest()
		{
			MonsterTest ThisInst = this;

			XMLSerializable.SerializeFromXML<MonsterTest>(GetFullPathFromFilename(GetFilename()), ref ThisInst, true);
		}

		public override LinkedEntity<MonsterTest> EditorDuplicate(LinkedEntity<MonsterTest> DerivedDuplicateInto = null)
		{
			MonsterTest DuplicateInto = (MonsterTest)DerivedDuplicateInto;
			
			if(DuplicateInto == null)
			{
				DuplicateInto = new MonsterTest();
			}

			return base.EditorDuplicate(DuplicateInto);
		}
		
		public override EntityID GenerateEntityIDForEvent()
		{
			EntityID NewID = new EntityID();
			
			NewID.ChapterFilename = Filename;
			
			return NewID;
		}
	#endif // UNITY_EDITOR
		
		public MonsterTestManager TestStates = null;

		public class MonsterTestLink : EntityLink<MonsterTest>
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
		
		public override string GetEntityName()
		{
			return "MonsterTest";
		}

		public override string GetLinkTypeName()
		{
			return "MonsterTestLink";
		}
		
		public override string GetLinkListName()
		{
			return "LinkedMonsterTests";
		}
		
		public virtual string GetTestStateListFilename()
		{
			return MonsterTestCore.MonsterLocalDirectoryRoot + "/Config/Tests/TestStateList" + GetFilename() + ".xml";
		}
		
		public override MonsterTest GetLinkedEntityForFilename(string Filename)
		{
	//		return MonsterTestManager.GetInstance().GetEntityByFileName(Filename);
			return null;
		}

		public override LinkedEntityManager<MonsterTest> GetEntityManager()
		{
			return null;//ChapterManager.GetInstance();
		}
		
		public virtual void LoadTestStates()
		{
			TestStates = MonsterTestManager.SwapActiveMonsterTestManager(this);
		}

		public virtual IGraphEvent GetStartingEvent()
		{
			if(InputEvents.Count > 0)
			{
				return GetEventAndTriggerLink(InputEvents[0]);
			}

			return null;
		}

		public virtual void TestSucceeded()
		{
			MonsterDebug.Log("Test succeeded!");
		}

		public virtual void TestFailed()
		{
			MonsterDebug.Log("Test failed!");
		}

		public virtual IGraphEvent GetNextTest(EntityLink<MonsterTestBase> NextLink)
		{
			foreach(EntityLink<MonsterTest> CurrentLink in OutputEvents)
			{
				if(CurrentLink.Name == NextLink.Name)
				{
					if(CurrentLink.Name == "Test Succeeded")
					{
						TestSucceeded();
					}
					else if(CurrentLink.Name == "Test Failed")
					{
						TestFailed();
					}
					if(CurrentLink.LinkedEntities.Count > 0 && CurrentLink.LinkedEntities[0].GetOwner() != null)
					{
						return CurrentLink.LinkedEntities[0].GetOwner().GetStartingEvent();
					}
				}
			}

			return null;
		}

		public override void CreateStaticNodesIfNotPresent()
		{
			base.CreateStaticNodesIfNotPresent();
			
			if(InputEvents.Count == 0)
			{
				EntityLink<MonsterTest> InputLink = new EntityLink<MonsterTest>();
				InputLink.SetOwner(this);
				InputLink.Name = "Test Started";
				InputEvents.Add(InputLink);

				InputLink = new EntityLink<MonsterTest>();
				InputLink.SetOwner(this);
				InputLink.Name = "Test Failed To Start";
				InputEvents.Add(InputLink);
			}
		}

		public override IGraphEvent GetEventAndTriggerLink(EntityLink<MonsterTest> InputLink)
		{
	//		MonsterTestManager.GetActiveInstance().CurrentNode = this;
			
			base.GetEventAndTriggerLink(InputLink);
			
			if(TestStates == null)
			{
				LoadTestStates();
			}
			
	/*		if(GameManager.GetInstance() != null)
			{
				GameManager.GetInstance().ChapterChanged();
			}*/
			
			return TestStates.GetEventForStart(InputLink);
		}

		public override void SerializeXML()
		{
			base.SerializeXML();

			SerializeBool("bAllowRunningInEditor", ref bAllowRunningInEditor);
			SerializeBool("bStartGameInEditor", ref bStartGameInEditor);
			SerializeBool("bForceLoadToFirstSceneInEditor", ref bForceLoadToFirstSceneInEditor);
		}
	}
}

#endif // MONSTER_TEST_RUNTIME || UNITY_EDITOR
