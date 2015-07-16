using UnityEngine;
using System.Collections;

namespace Igor
{
public class MonsterTestItem : ListItem<MonsterTest> {

	public MonsterTestItem(MonsterTestListWindow NewOwner, int NewIndexToWatch = -1) : base(NewOwner, NewIndexToWatch)
	{
	}
	
	public override object GetInspectorInstance()
	{
		if(IndexToWatch != -1)
		{
			return Owner.GetManager().EditorGetValueAtIndex(IndexToWatch);
		}
		
		return null;
	}

	public static MonsterTest CreateNewMonsterTest()
	{
		MonsterTest NewTest = new MonsterTest();

		NewTest.CreateStaticNodesIfNotPresent();

		NewTest.LoadTestStates();
		NewTest.EditorSetFilename(MonsterTestListWindow.NewTestName);
		NewTest.EditorSaveMonsterTest();

		MonsterTestWindow.CurrentlyOpenTest = NewTest;
		MonsterTestWindow.Init();

		MonsterTestList.EditorMoveTestToMainList(NewTest);

		return NewTest;
	}
	
	public override void CreateNewSourceAsset()
	{
		MonsterTest NewTest = CreateNewMonsterTest();
		
		CompareString = NewTest.GetFilename();
		
		IndexToWatch = Owner.GetManager().EditorHasCompareString(CompareString);
		
		Owner.RebuildList();
	}

	public override void HandleDoubleClick()
	{
		if(IndexToWatch != -1)
		{
			MonsterTest CurrentValue = Owner.GetManager().EditorGetValueAtIndex(IndexToWatch);

			MonsterTestWindow.CurrentlyOpenTest = CurrentValue;
			MonsterTestWindow.Init();
		}
	}

	public override void EntityDrawInspectorWidgets(object Instance)
	{
		base.EntityDrawInspectorWidgets(Instance);
		
		MonsterTest TestInst = (MonsterTest)Instance;

		string Filename = TestInst.GetFilename();
		InspectorGUIString("Test Name", ref Filename, true);

		InspectorGUIBool("Allow Running In Editor", ref TestInst.bAllowRunningInEditor);

		if(TestInst.bAllowRunningInEditor)
		{
			InspectorGUIBool("Start Game In Editor", ref TestInst.bStartGameInEditor);
			InspectorGUIBool("Force Load To First Scene In Editor", ref TestInst.bForceLoadToFirstSceneInEditor);
		}
		
		OnInspectorGUIDrawSaveButton("Save test");
		
/*		string CurrentDisplayName = TestInst.GetName();
		
		InspectorGUIString("Name", ref CurrentDisplayName);
		
		if(CurrentDisplayName != AttributeInst.GetName())
		{
			AttributeInst.EditorSetDisplayName(CurrentDisplayName);
		}*/
	}
	
	public override void EntityPostDrawInspectorWidgets(object Instance)
	{
		base.EntityPostDrawInspectorWidgets(Instance);
	}

	public override void OnInspectorGUIClickedSaveButton()
	{
		base.OnInspectorGUIClickedSaveButton();
		
		MonsterTestList.SaveMonsterTestList();
		
		Owner.RebuildList();
	}
}
}
