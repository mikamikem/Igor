#if MONSTER_TEST_RUNTIME || UNITY_EDITOR
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace Igor
{
[System.Serializable]
public class MonsterTestList : ManagerList<MonsterTest> {
	
	private static MonsterTestList ManagerInst;
	
	public static MonsterTestList GetInstance()
	{
		if(!XMLSerializable.bSafeToLoad)
		{
			TypeUtils.RegisterAllTypes();
		}
		
		if(MonsterTestList.ManagerInst == null)
		{
			LoadMonsterTestList();
		}
		
		return MonsterTestList.ManagerInst;
	}
	
	public static string GetManagerFilename()
	{
		return MonsterTestCore.MonsterLocalDirectoryRoot + "/Config/MonsterTestList.xml";
	}
	
	public static void LoadMonsterTestList()
	{
		MonsterTestList.ManagerInst = new MonsterTestList();
		
		SerializeFromXML<MonsterTestList>(GetManagerFilename(), ref MonsterTestList.ManagerInst, false);
	}
	
	public static void SaveMonsterTestList()
	{
		if(MonsterTestList.ManagerInst != null)
		{
			SerializeFromXML<MonsterTestList>(GetManagerFilename(), ref MonsterTestList.ManagerInst, true);
		}
	}
	
	[System.NonSerialized]
	private Dictionary<string, MonsterTest> Tests = new Dictionary<string, MonsterTest>();
	
	public static MonsterTest GetMonsterTestForID(string TestName)
	{
		return MonsterTestList.GetInstance().InternalGetTestForID(TestName);
	}
	
#if UNITY_EDITOR
	private List<MonsterTest> PendingNewTests = new List<MonsterTest>();
	
	public static void EditorRemoveMonsterTests(string CompareName)
	{
		MonsterTestList.GetInstance().EditorInternalRemoveTest(CompareName);
	}
	
	private void EditorInternalRemoveTest(string CompareName)
	{
		if(Tests.ContainsKey(CompareName))
		{
			Tests.Remove(CompareName);
		}
	}
	
	public static void EditorAddMonsterTest(MonsterTest NewTest)
	{
		MonsterTestList.GetInstance().EditorInternalAddTest(NewTest);
	}
	
	private void EditorInternalAddTest(MonsterTest NewTest)
	{
		for(int CurrentTest = 0; CurrentTest < PendingNewTests.Count; ++CurrentTest)
		{
			if(PendingNewTests[CurrentTest].GetFilename() == NewTest.GetFilename())
			{
//				PendingNewTests[CurrentTest].EditorUpdateWith(NewTest);
				
				return;
			}
		}
		
		PendingNewTests.Add(NewTest);
	}
	
	public static void EditorMoveTestToMainList(MonsterTest NewTest)
	{
		MonsterTestList.GetInstance().EditorInternalMoveTestToMainList(NewTest);
	}
	
	private void EditorInternalMoveTestToMainList(MonsterTest NewTest)
	{
		if(NewTest.GetFilename() != "")
		{
			for(int CurrentTest = 0; CurrentTest < PendingNewTests.Count; ++CurrentTest)
			{
				if(PendingNewTests[CurrentTest].GetFilename() == NewTest.GetFilename())
				{
					PendingNewTests.RemoveAt(CurrentTest);
					
					break;
				}
			}
			
			EditorInternalAddTestWithFilename(NewTest);
		}
	}
	
	private void EditorInternalAddTestWithFilename(MonsterTest NewTest)
	{
		string NewTestFilename = NewTest.GetFilename();
		
		if(Tests.ContainsKey(NewTestFilename))
		{
			Tests[NewTestFilename] = NewTest;
		}
		else
		{
			Tests.Add(NewTestFilename, NewTest);
		}
	}
	
	public static string EditorGetUniqueID(string NewName)
	{
		return MonsterTestList.GetInstance().EditorInternalGetUniqueID(NewName);
	}
	
	public string EditorInternalGetUniqueID(string OriginalID)
	{
		bool bExists = true;
		string TestID = OriginalID;
		int CurrentIndex = 0;
		
		while(bExists)
		{
			bExists = false;
			
			foreach(KeyValuePair<string, MonsterTest> TestPair in Tests)
			{
				if(TestID == TestPair.Value.GetFilename())
				{
					bExists = true;
					break;
				}
			}
			
			if(bExists)
			{
				TestID = OriginalID + "-" + (++CurrentIndex);
			}
		}
		
		return TestID;
	}

	public override MonsterTest EditorGetValueAtIndex(int Index)
	{
		if(Tests.Values.Count > Index && Index > -1)
		{
			int CurrentIndex = 0;
			
			foreach(KeyValuePair<string, MonsterTest> CurrentTest in Tests)
			{
				if(CurrentIndex == Index)
				{
					return CurrentTest.Value;
				}
				
				++CurrentIndex;
			}
		}
		
		return null;
	}
	
	public override int EditorGetListCount()
	{
		return Tests.Values.Count;
	}
	
	public override int EditorHasCompareString(string CompareString)
	{
		if(Tests.ContainsKey(CompareString))
		{
			int CurrentIndex = 0;
			
			foreach(KeyValuePair<string, MonsterTest> CurrentTest in Tests)
			{
				if(CurrentTest.Key == CompareString)
				{
					return CurrentIndex;
				}
				
				++CurrentIndex;
			}
		}
		
		return -1;
	}
#endif // UNITY_EDITOR
	
	private MonsterTest InternalGetTestForID(string TestName)
	{
		if(Tests.ContainsKey(TestName))
		{
			return Tests[TestName];
		}
		
		return null;
	}
	
	public override void SerializeXML()
	{
		base.SerializeXML();
		
		SerializeBegin("MonsterTestList");
		
		if(bReading)
		{
			List<string> TestFilenames = new List<string>();

			SerializeStringList("MonsterTestList", "MonsterTestEntry", ref TestFilenames);
			
			for(int CurrentTest = 0; CurrentTest < TestFilenames.Count; ++CurrentTest)
			{
				MonsterTest CurrentTestInst = MonsterTest.LoadMonsterTest(TestFilenames[CurrentTest]);

				string CurrentTestFilename = CurrentTestInst.GetFilename();

				if(Tests.ContainsKey(CurrentTestFilename))
				{
					Tests[CurrentTestFilename] = CurrentTestInst;
				}
				else
				{
					Tests.Add(CurrentTestFilename, CurrentTestInst);
				}
			}
		}
		else
		{
			List<string> TestFilenames = new List<string>();

			foreach(KeyValuePair<string, MonsterTest> CurrentTest in Tests)
			{
				string NewFilename = CurrentTest.Value.GetFilename();

				if(NewFilename != "")
				{
					TestFilenames.Add(NewFilename);
				}
			}
			
			SerializeStringList("MonsterTestList", "MonsterTestEntry", ref TestFilenames);
		}
	}
}
}

#endif // MONSTER_TEST_RUNTIME || UNITY_EDITOR
