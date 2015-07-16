using UnityEngine;
using System.Collections;

namespace Igor
{
	public class Anchor<EntityType> where EntityType : LinkedEntity<EntityType>, new() {
		
		public class AnchorType
		{
			public bool bIsInput;
			
			public AnchorType(bool bInIsInput)
			{
				bIsInput = bInIsInput;
			}
		}
		
		protected string LabelText;
		protected string LabelDescription;
		protected AnchorType Type;
		public EntityBox<EntityType> Owner;
		public Rect LastRect = new Rect();
		
		public Anchor(string NewLabelText, string NewLabelDescription, AnchorType InType, EntityBox<EntityType> InOwner)
		{
			LabelText = NewLabelText;
			LabelDescription = NewLabelDescription;
			Type = InType;
			Owner = InOwner;
		}

		public virtual string GetLabelDescription()
		{
			return LabelDescription;
		}
		
		public virtual string GetLabelText()
		{
			return LabelText;
		}
		
		public virtual bool CanConnectTo(Anchor<EntityType> OtherAnchor)	
		{
			return Type.bIsInput != OtherAnchor.Type.bIsInput;
		}
		
		public virtual bool IsInput()
		{
			return Type.bIsInput;
		}
		
		public virtual void CleanupBeforeRemoval()
		{
		}

	}
}