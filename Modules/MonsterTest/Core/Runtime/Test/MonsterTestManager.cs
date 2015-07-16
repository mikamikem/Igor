#if MONSTER_TEST_RUNTIME || UNITY_EDITOR
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;

namespace Igor
{
[System.Serializable]
public class MonsterTestManager : LinkedEntityManager<MonsterTestBase> {
	
	private static MonsterTest Owner;
	private static string EntityListFilename;
	
	private static MonsterTestManager Instance;
	public static MonsterTestManager GetActiveInstance()
	{
		return Instance;
	}
	
	public static MonsterTestManager SwapActiveMonsterTestManager(MonsterTest NewTest)
	{
		if(Instance != null)
		{
			Instance.UnloadTests();
		}
		
		Instance = new MonsterTestManager();

		EntityListFilename = NewTest.GetTestStateListFilename();
		Owner = NewTest;

		XMLSerializable.SerializeFromXML<MonsterTestManager>(NewTest.GetTestStateListFilename(), ref Instance, false);
		
		return Instance;
	}
	
	public virtual void UnloadTests()
	{
		Instance = null;
	}
	
	public override void CreateStaticNodesIfNotPresent()
	{
		base.CreateStaticNodesIfNotPresent();
		
		if(StartingEntities.Count == 0)
		{
			EntityLink<MonsterTestBase> StartLink = new EntityLink<MonsterTestBase>();
			StartLink.CurrentLinkType = XMLSerializable.LinkType.LINK_StartLink;
			StartLink.Name = "Test Started";
			StartingEntities.Add(StartLink);

			StartLink = new EntityLink<MonsterTestBase>();
			StartLink.CurrentLinkType = XMLSerializable.LinkType.LINK_StartLink;
			StartLink.Name = "Test Failed To Start";
			StartingEntities.Add(StartLink);
		}
		if(EndingEntities.Count == 0)
		{
			EntityLink<MonsterTestBase> EndLink = new EntityLink<MonsterTestBase>();
			EndLink.CurrentLinkType = XMLSerializable.LinkType.LINK_EndLink;
			EndLink.Name = "Test Succeeded";
			EndingEntities.Add(EndLink);

			EndLink = new EntityLink<MonsterTestBase>();
			EndLink.CurrentLinkType = XMLSerializable.LinkType.LINK_EndLink;
			EndLink.Name = "Test Failed";
			EndingEntities.Add(EndLink);
		}
	}
	
	public override void PostSerialize()
	{
		base.PostSerialize();
		
		for(int CurrentNode = 0; CurrentNode < StartingEntities.Count; ++CurrentNode)
		{
			StartingEntities[CurrentNode].FixupLinks(null, GetActiveInstance);
		}

		for(int CurrentNode = 0; CurrentNode < EndingEntities.Count; ++CurrentNode)
		{
			EndingEntities[CurrentNode].FixupLinks(null, GetActiveInstance);
		}
	}

	public virtual IGraphEvent GetNextEventAfterCurrentTestState(EntityLink<MonsterTestBase> NextLink)
	{
		if(Owner != null)
		{
			return Owner.GetNextTest(NextLink);
		}

		return null;
	}
	
#if UNITY_EDITOR
	public static void LoadTest()
	{
		MonsterTestManager.GetActiveInstance().LoadEntities();
	}
	
	public static void SaveTest()
	{
		MonsterTestManager.GetActiveInstance().SaveEntities();
	}
	
	public static void AddTestState(MonsterTestBase NewTestState)
	{
		MonsterTestManager.GetActiveInstance().AddEntity(NewTestState);
	}
	
	public static void RemoveTestState(MonsterTestBase TestStateToRemove)
	{
		MonsterTestManager.GetActiveInstance().RemoveEntity(TestStateToRemove);
	}
	
	public static List<MonsterTestBase> EditorGetTestStateList()
	{
		return MonsterTestManager.GetActiveInstance().Entities;
	}
	
	public static string EditorGetUniqueTestStateFilename(string OriginalFilename)
	{
		return MonsterTestManager.GetActiveInstance().EditorGetUniqueEntityFilename(OriginalFilename);
	}
	
	public static string EditorGetLocationName()
	{
//		return DialogueManager.EditorGetLocationName() + " - " + Owner.EditorGetDisplayName();
		return "";
	}
#endif // UNITY_EDITOR
	
	public override string GetEntityListFilename()
	{
		return EntityListFilename;
	}
	
	public override string GetDefaultEntityPath()
	{
		return MonsterTestCore.MonsterLocalDirectoryRoot + "/Config/TestStates/" + GetOwnerFilename() + "/";
	}
	
	public virtual string GetOwnerFilename()
	{
		return Owner.GetFilename();
	}
	
	protected override string GetManagerName()
	{
		return "MonsterTestManager";
	}

	protected override string GetEntityListName()
	{
		return "MonsterTestStateList";
	}
	
	protected override string GetEntityElementName()
	{
		return "MonsterTestStateFilename";
	}
	
	public override void LoadEntities()
	{
		SerializeEntityManager(false);
	}
	
	protected override void SerializeEntityManager(bool bSaving)
	{
		if(bSaving)
		{
//			StringLibrary.SaveLibrary();
			
			SerializeListOwnerPrefixFixup();
		}
		
		XMLSerializable.SerializeFromXML<MonsterTestManager>(GetEntityListFilename(), ref Instance, bSaving);

		if(bSaving)
		{
#if UNITY_EDITOR
			if(Owner != null)
			{
				Owner.EditorSaveMonsterTest();
			}
#endif // UNITY_EDITOR
		}
	}
	
	public virtual IGraphEvent GetEventForStart(EntityLink<MonsterTest> StartLink)
	{
		for(int CurrentLink = 0; CurrentLink < StartingEntities.Count; ++CurrentLink)
		{
			if(StartingEntities[CurrentLink].Name == StartLink.Name)
			{
				if(StartingEntities[CurrentLink].LinkedEntities.Count > 0)
				{
					EntityLink<MonsterTestBase> ValidLink = StartingEntities[CurrentLink].GetValidLink();
					
					if(ValidLink != null)
					{
						return ValidLink.GetEventAndTriggerLink();
					}
				}
			}
		}
		
		return null;
	}
	
	public virtual IGraphEvent GetEntityForID(EntityID CurrentID)
	{
/*		foreach(ConversationBase CurrentConversationBase in Entities)
		{
			if(CurrentConversationBase.GetFilename() == CurrentID.ConversationFilename)
			{
				return CurrentConversationBase;
			}
		}*/
		
		return null;
	}
}
}

#endif // MONSTER_TEST_RUNTIME || UNITY_EDITOR
