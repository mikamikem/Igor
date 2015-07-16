using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;

namespace Igor
{
[InitializeOnLoad]
public class MonsterTestBaseBox : EntityBox<MonsterTestBase> {
	
	public static void RegisterEditorType()
	{
		TypeUtils.RegisterEditorType("MonsterTestBase", CreateMonsterTestBaseBoxForType);
	}
	
	public static object CreateMonsterTestBaseBoxForType(object Owner, object EntityToWatch)
	{
		return new MonsterTestBaseBox((GraphWindow<MonsterTestBase>)Owner, (MonsterTestBase)EntityToWatch);
	}
	
	protected MonsterTestBase MonsterTestBaseInst
	{
		get
		{
			return (MonsterTestBase)WrappedInstance;
		}
	}
	
	public MonsterTestBaseBox(GraphWindow<MonsterTestBase> Owner, MonsterTestBase TestBaseToWatch) : base(Owner, TestBaseToWatch)
	{
	}
	
	public override string GetTypeName()
	{
		return "MonsterTestBase";
	}
	
	public override string GetClassTypeSaveString()
	{
		return "Save test base";
	}
	
	public override string GetBoxTitle()
	{
		return MonsterTestBaseInst.Title;
	}

	public override void EntityDrawInspectorWidgets(object Instance)
	{
		base.EntityDrawInspectorWidgets(Instance);
	}
	
	public override void OnInspectorGUIClickedSaveButton()
	{
		base.OnInspectorGUIClickedSaveButton();
	}

	public override string GetUniqueFilename(string OriginalFilename)
	{
		return MonsterTestManager.EditorGetUniqueTestStateFilename(OriginalFilename);
	}
	
	public override void SaveEntities()
	{
		MonsterTestManager.SaveTest();
	}
	
	public override void RemoveEntity()
	{
		MonsterTestManager.RemoveTestState(MonsterTestBaseInst);
	}

	public static MonsterTestBase.MonsterTestLink MonsterTestBaseLinkToEntityLink(MonsterTestBase.MonsterTestLink MonsterTestLinkInst)
	{
		return (MonsterTestBase.MonsterTestLink)MonsterTestLinkInst;
	}

	public override bool HandleContextMenu(GenericMenu GenericContextMenu)
	{
		base.HandleContextMenu(GenericContextMenu);
		
//		GenericContextMenu.AddItem(new GUIContent("Generate RTF From Node/For Only This Dialogue"), false, GenerateRTFForDialogue);

		GenericContextMenu.AddSeparator("");
		
		return true;
	}

/*	public virtual void GenerateRTFForDialogue()
	{
		ChapterWindow.ExportSpecificToRTFWithMask(ScriptExporter.SearchMask.CurrentDialogue);
	}*/

	public override void DrawAnchorContent(Anchor<MonsterTestBase> CurrentAnchor, int CurrentAnchorIndex, ref Vector2 Origin, ref float WidestLabel, ref Rect WholeAnchorRect, bool bIsHovered, bool bJustCalculateSize, TypeOfAnchor AnchorType, bool bLeftAlign)
	{
		base.DrawAnchorContent(CurrentAnchor, CurrentAnchorIndex, ref Origin, ref WidestLabel, ref WholeAnchorRect, bIsHovered, bJustCalculateSize, AnchorType, bLeftAlign);
		
		Vector2 PlusTextSize = GetLabelSize("+");
		Rect PlusButtonPosition = new Rect(Origin.x + AnchorBoxSize, Origin.y, PlusTextSize.x + AnchorLabelGap, PlusTextSize.y + AnchorSubtextGap);
		
		if(!bLeftAlign)
		{
			PlusButtonPosition = new Rect(Origin.x + WidestLabel - AnchorBoxSize - PlusButtonPosition.width, Origin.y, PlusButtonPosition.width, PlusButtonPosition.height);
		}
		
		if(!bJustCalculateSize && !bLeftAlign)
		{
			if(GUI.Button(PlusButtonPosition, new GUIContent("+")))
			{
				Owner.HandleChainBoxContextMenu(CurrentAnchor);
			}
		
			Origin.y += PlusTextSize.y + AnchorSubtextGap;
		}
	}

/*	public virtual void UpdateEvent()
	{
	}

	public override void RunChecks(EditorTypeUtils.EntityTestType TestType)
	{
		base.RunChecks(TestType);
		
		switch(TestType)
		{
		case EditorTypeUtils.EntityTestType.UpdateEvents:
			UpdateEvent();
			break;
		}
	}*/
}
}
