using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace Igor
{
public class MonsterTestWindow : GraphWindow<MonsterTestBase> {
	
	private static MonsterTestWindow WindowInstance;
	
	public static MonsterTestWindow GetInstance()
	{
		return WindowInstance;
	}
	
	public static MonsterTest CurrentlyOpenTest = null;
	
	public override void CreateStaticNodesIfNotPresent()
	{
		MonsterTestManager.GetActiveInstance().CreateStaticNodesIfNotPresent();
	}
	
	[MenuItem ("Window/Monster/Test/Open Test Editor", false, 0)]
	static public void Init () {
/*		if(MonsterTestManager.GetActiveInstance() == null)
		{
			ChapterManager.GetInstance().GetGameStart();
		}*/

		WindowInstance = (MonsterTestWindow)EditorWindow.GetWindow<MonsterTestWindow>("Monster Test", typeof (SceneView));
		
		WindowInstance.Initialize();
		
		ReloadTestFromFile();
	}
	
	[MenuItem ("Window/Monster/Test/Reload Test From File", false, 1)]
	static public void ReloadTestFromFile()
	{
		if(WindowInstance != null)
		{
			WindowInstance.ClearAllBoxesAndLinks();
			
			if(CurrentlyOpenTest != null)
			{
				MonsterTestManager.SwapActiveMonsterTestManager(CurrentlyOpenTest);
			}
			
			MonsterTestManager.LoadTest();
			
			WindowInstance.PreInit();
			
			List<MonsterTestBase> AllTestStates = MonsterTestManager.EditorGetTestStateList();
		
			foreach(MonsterTestBase CurrentTestState in AllTestStates)
			{
				MonsterTestBaseBox NewBox = (MonsterTestBaseBox)TypeUtils.GetEditorBoxForTypeString(CurrentTestState.GetEntityName(), WindowInstance, CurrentTestState);
				WindowInstance.AddBox(NewBox);
			}
			
			WindowInstance.PostInit();
		}
	}

	static public void FullGraphRefreshFromCurrentData()
	{
		if(WindowInstance != null)
		{
			WindowInstance.ClearAllBoxesAndLinks();
			
			WindowInstance.PreInit();
			
			List<MonsterTestBase> AllTestStates = MonsterTestManager.EditorGetTestStateList();
		
			foreach(MonsterTestBase CurrentTestState in AllTestStates)
			{
				MonsterTestBaseBox NewBox = (MonsterTestBaseBox)TypeUtils.GetEditorBoxForTypeString(CurrentTestState.GetEntityName(), WindowInstance, CurrentTestState);
				WindowInstance.AddBox(NewBox);
			}
			
			WindowInstance.PostInit();
		}
	}
	
	[MenuItem ("Window/Monster/Test/Save Test", false, 2)]
	static public void SaveTestStateList()
	{
		MonsterTestManager.GetActiveInstance().SaveEntities();

		if(CurrentlyOpenTest != null)
		{
			CurrentlyOpenTest.EditorSaveMonsterTest();
		}
		
		MonsterTestWindow.GetInstance().SerializeBoxMetadata(true);
	}

/*	[MenuItem ("Dialogue/Tests/Conversation/Check For Missing VO", false, 15)]
	static public void RunMissingAudioCheck()
	{
		RunTest(EditorTypeUtils.EntityTestType.TestForMissingAudio);
	}
	
	[MenuItem ("Dialogue/Tests/Conversation/Check For Missing Connections", false, 22)]
	static public void RunMissingConnectionCheck()
	{
		RunTest(EditorTypeUtils.EntityTestType.TestForMissingConnections);
	}*/
	
	static public void RunTest(EditorTypeUtils.EntityTestType TestType)
	{
		if(WindowInstance != null)
		{
			foreach(EntityBox<MonsterTestBase> CurrentBox in WindowInstance.Boxes)
			{
				CurrentBox.RunChecks(TestType);
			}
			
			WindowInstance.Repaint();
		}
	}

/*	[MenuItem ("Dialogue/Tests/Conversation/Fix One Way Links", false, 23)]
	static public void FixOneWayLinks()
	{
		RunTest(EditorTypeUtils.EntityTestType.FixOneWayLinks);
	}*/

	public virtual MonsterTestBase GetCurrentlySelected()
	{
		object SelectedInst = SelectedBox.GetInspectorInstance();

		if(SelectedInst != null)
		{
			if(typeof(MonsterTestBase).IsAssignableFrom(SelectedInst.GetType()))
			{
				return (MonsterTestBase)SelectedInst;
			}
		}

		return null;
	}
	
	public override LinkedEntityManager<MonsterTestBase> GetManager()
	{
		return MonsterTestManager.GetActiveInstance();
	}
	
	public override void CreateInputOutputBoxes()
	{
		StartBox = new MonsterTestInputOutputBox(this, null, "Test Begins", true);
		EndBox = new MonsterTestInputOutputBox(this, null, "Test Ends", false);
		
		AddBox(StartBox);
		AddBox(EndBox);
	}
	
	public override void AddNoBoxContextMenuEntries(GenericMenu MenuToAddTo)
	{
//		MenuToAddTo.AddItem(new GUIContent("Add Line"), false, AddLine);

		MenuToAddTo.AddSeparator("");

		MenuToAddTo.AddItem(new GUIContent("Reload Test From File"), false, ReloadTestFromFile);
		MenuToAddTo.AddItem(new GUIContent("Organize Boxes"), false, OrganizeBoxes);
		MenuToAddTo.AddItem(new GUIContent("Save Test"), false, SaveTestStateList);
	}
	
	public override void AddNewBoxContextMenuEntries(GenericMenu MenuToAddTo)
	{
		base.AddNewBoxContextMenuEntries(MenuToAddTo);
		
//		MenuToAddTo.AddItem(new GUIContent("Connect new Line"), false, ConnectLine);
	}

/*	public virtual void AddLine()
	{
		Line NewLine = new Line();
		
		SetupNewLineBox(NewLine);
	}
	
	public virtual void ConnectLine()
	{
		Line NewLine = new Line();
		
		NewLine.CreateStaticNodesIfNotPresent();
		
		LineBox NewBox = SetupNewLineBox(NewLine);
		
		if(NewBox.GetAllAnchors().Count > 0)
		{
			Anchor<ConversationBase> NewBoxAnchor = NewBox.GetAllAnchors()[0];
			
			ConnectInputToOutput(NewBoxAnchor, StartingAnchorForNewBox);
		}
	}
	
	public virtual LineBox SetupNewLineBox(Line NewLine)
	{		
		ConversationManager.AddConversation(NewLine);
		
		LineBox NewBox = new LineBox(ConversationWindow.WindowInstance, NewLine);
		
		NewBox.InitializeNewBox();
		
		NewBox.MoveBoxTo(InputState.GetLocalMousePosition(this, -GetWindowOffset()));
		
		ConversationWindow.WindowInstance.AddBox(NewBox);
		
		return NewBox;
	}*/
	
	public override string GetBoxMetadataFile()
	{
		return MonsterTestCore.MonsterLocalDirectoryRoot + "/Config/Editor/MonsterTests/MonsterTestBoxes" + MonsterTestManager.GetActiveInstance().GetOwnerFilename() + ".xml";
	}
	
	public override void SaveRequested()
	{
		MonsterTestManager.SaveTest();
	}
	
	public override string[] GetSaveDialogText()
	{
		string[] DialogText = { "Save all test states for this test?", "Would you like to save all the test states for this test?", "Yes", "No" };
		
		return DialogText;
	}
	
}
}
