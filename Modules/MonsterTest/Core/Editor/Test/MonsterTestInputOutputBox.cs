using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;

namespace Igor
{
public class MonsterTestInputOutputBox : InputOutputBox<MonsterTestBase>
{
	public MonsterTestInputOutputBox(MonsterTestWindow Owner, MonsterTestBase EntityToWatch, string InBoxTitle, bool InbIsInputBox) : base(Owner, EntityToWatch, InBoxTitle, InbIsInputBox)
	{
	}
	
	public override string GetTypeName()
	{
		return bIsInputBox ? "StartingTestStates" : "EndingTestStates";
	}
	
	public override string GetBoxKey()
	{
		return bIsInputBox ? "StartingTestStates" : "EndingTestStates";
	}

	public override bool HandleContextMenu(GenericMenu GenericContextMenu)
	{
		return true;
	}
	
	public override LinkedEntityManager<MonsterTestBase> GetManager()
	{
		return MonsterTestManager.GetActiveInstance();
	}
	
	public override List<EntityLink<MonsterTestBase>> GetInputEvents()
	{
		if(!bIsInputBox)
		{
			return GetManager().EndingEntities;
		}
		
		return new List<EntityLink<MonsterTestBase>>();
	}
	
	public override List<EntityLink<MonsterTestBase>> GetOutputEvents()
	{
		if(bIsInputBox)
		{
			return GetManager().StartingEntities;
		}
		
		return new List<EntityLink<MonsterTestBase>>();
	}
	
	public override bool WrapsInstance(LinkedEntity<MonsterTestBase> InstToCheck)
	{
		return GetManager().GetFilename() == InstToCheck.GetFilename();
	}
	
	public override void OnInspectorGUIClickedSaveButton()
	{
		MonsterTestManager.SaveTest();
		
		Owner.SerializeBoxMetadata(true);
	}

	public override List<EntityLink<MonsterTestBase>> GetStartEntities()
	{
		return GetManager().StartingEntities;
	}

	public override List<EntityLink<MonsterTestBase>> GetEndEntities()
	{
		return GetManager().EndingEntities;
	}
	
	public override EntityLink<MonsterTestBase> GetLinkByName(string AnchorName)
	{
		return GetManager().GetLinkByName(AnchorName);
	}
	
	public override void InspectorDrawStartWidgets()
	{
		InspectorGUIList<EntityLink<MonsterTestBase>>("Outputs", "Name", ref GetManager().StartingEntities, bReadOnlyInputList);
	}

	public override void InspectorDrawEndWidgets()
	{
		InspectorGUIList<EntityLink<MonsterTestBase>>("Inputs", "Name", ref GetManager().EndingEntities, bReadOnlyOutputList);
	}

}
}
