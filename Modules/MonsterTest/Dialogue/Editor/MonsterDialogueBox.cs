using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;

namespace Igor
{
	[InitializeOnLoad]
	public class MonsterDialogueBox : MonsterTestBaseBox {
		
		public static void RegisterEditorType()
		{
			TypeUtils.RegisterEditorType("MonsterDialogue", CreateMonsterDialogueBoxForType);
		}
		
		public static object CreateMonsterDialogueBoxForType(object Owner, object EntityToWatch)
		{
			return new MonsterDialogueBox((GraphWindow<MonsterTestBase>)Owner, (MonsterDialogue)EntityToWatch);
		}
		
		protected MonsterDialogue MonsterDialogueInst
		{
			get
			{
				return (MonsterDialogue)WrappedInstance;
			}
		}
		
		public MonsterDialogueBox(GraphWindow<MonsterTestBase> Owner, MonsterTestBase TestBaseToWatch) : base(Owner, TestBaseToWatch)
		{
		}
		
		public override string GetTypeName()
		{
			return "MonsterDialogue";
		}
		
		public override string GetClassTypeSaveString()
		{
			return "Save dialogue traversal";
		}
		
		public override string GetBoxTitle()
		{
			return MonsterDialogueInst.Title;
		}

		public override void EntityDrawInspectorWidgets(object Instance)
		{
			base.EntityDrawInspectorWidgets(Instance);

			InspectorGUIBool("Prefer Exit Options", ref MonsterDialogueInst.bPreferExitOptions);
			InspectorGUIBool("Avoid Exit Options", ref MonsterDialogueInst.bAvoidExitOptions);
		}
		
		public override string GetUniqueFilename(string OriginalFilename)
		{
			return MonsterTestManager.EditorGetUniqueTestStateFilename(OriginalFilename);
		}
	}
}
