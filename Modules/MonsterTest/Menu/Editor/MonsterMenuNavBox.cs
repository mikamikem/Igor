using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;

namespace Igor
{
	[InitializeOnLoad]
	public class MonsterMenuNavBox : MonsterTestBaseBox {
		
		public static void RegisterEditorType()
		{
			TypeUtils.RegisterEditorType("MonsterMenuNav", CreateMonsterMenuNavBoxForType);
		}
		
		public static object CreateMonsterMenuNavBoxForType(object Owner, object EntityToWatch)
		{
			return new MonsterMenuNavBox((GraphWindow<MonsterTestBase>)Owner, (MonsterMenuNav)EntityToWatch);
		}
		
		protected MonsterMenuNav MonsterMenuNavInst
		{
			get
			{
				return (MonsterMenuNav)WrappedInstance;
			}
		}
		
		public MonsterMenuNavBox(GraphWindow<MonsterTestBase> Owner, MonsterTestBase TestBaseToWatch) : base(Owner, TestBaseToWatch)
		{
		}
		
		public override string GetTypeName()
		{
			return "MonsterMenuNav";
		}
		
		public override string GetClassTypeSaveString()
		{
			return "Save menu navigation";
		}
		
		public override string GetBoxTitle()
		{
			return MonsterMenuNavInst.Title;
		}

		public override void EntityDrawInspectorWidgets(object Instance)
		{
			base.EntityDrawInspectorWidgets(Instance);

			InspectorGUIFloat("Time To Wait For Menu Initialization", ref MonsterMenuNavInst.TimeToWait);
		}
		
		public override string GetUniqueFilename(string OriginalFilename)
		{
			return MonsterTestManager.EditorGetUniqueTestStateFilename(OriginalFilename);
		}
	}
}
