using System;
using UnityEngine;
using UnityEditor;
using System.Collections;

namespace Igor
{
public class MonsterTestListWindow : ListWindow<ManagerList<MonsterTest>, MonsterTest> {

	private static MonsterTestListWindow WindowInstance;
	
	public static MonsterTestListWindow GetInstance()
	{
		return WindowInstance;
	}
	
	[MenuItem ("Window/Monster/Test List")]
	static public void Init () {
		System.Reflection.Assembly EditorAssembly = typeof(UnityEditor.EditorWindow).Assembly;
		
		Type ProjectViewType = EditorAssembly.GetType("UnityEditor.ProjectBrowser");
		
		WindowInstance = (MonsterTestListWindow)EditorWindow.GetWindow<MonsterTestListWindow>("Monster Tests", ProjectViewType);
		
		WindowInstance.Initialize();
	}
	
	public override ManagerList<MonsterTest> GetManager()
	{
		return MonsterTestList.GetInstance();
	}

	public static string NewTestName = "";

	protected override void DrawHeader()
	{
		NewTestName = EditorGUILayout.TextField("New Test Name", NewTestName);

		EditorGUILayout.BeginHorizontal();
		
		GUI.enabled = NewTestName != "";

		if(GUILayout.Button(GetAddButtonText()))
		{
			AddNewItem();

			NewTestName = "";

			GUIUtility.keyboardControl = 0;
		}

		GUI.enabled = GetSelectionCount() > 0;
		
		if(GUILayout.Button(GetRemoveButtonText()))
		{
			RemoveSelectedItems();

			GUIUtility.keyboardControl = 0;
		}

		GUI.enabled = true;
		
		EditorGUILayout.EndHorizontal();
	}
	
	protected override string GetAddButtonText()
	{
		return "Add test";
	}
	
	protected override string GetRemoveButtonText()
	{
		if(GetSelectionCount() > 1)
		{
			return "Remove tests";
		}
		else
		{
			return "Remove test";
		}
	}
	
	protected override ListItem<MonsterTest> CreateNewItem(int IndexToWatch)
	{
		if(IndexToWatch == -1)
		{
			MonsterTest NewTest = MonsterTestItem.CreateNewMonsterTest();

			string CompareString = NewTest.GetFilename();
			
			IndexToWatch = GetManager().EditorHasCompareString(CompareString);

			MonsterTestList.SaveMonsterTestList();

			RebuildList();
		}

		return new MonsterTestItem(this, IndexToWatch);
	}
	
	protected override void RemoveItemWithCompareString(string CompareString)
	{
		MonsterTestList.EditorRemoveMonsterTests(CompareString);

		MonsterTestList.SaveMonsterTestList();
	}
}
}
