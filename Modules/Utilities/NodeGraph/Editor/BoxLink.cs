using UnityEngine;
using System.Collections;

namespace Igor
{
	public class BoxLink<EntityType> where EntityType : LinkedEntity<EntityType>, new() {
		
		protected bool bHighlighted = false;
		
		protected Anchor<EntityType> StartAnchor;
		protected Anchor<EntityType> EndAnchor;
		
		public BoxLink(Anchor<EntityType> FromAnchor, Anchor<EntityType> ToAnchor)
		{
			StartAnchor = FromAnchor.IsInput() ? FromAnchor : ToAnchor;
			EndAnchor = FromAnchor.IsInput() ? ToAnchor : FromAnchor;
			
			OnConnectAnchors();
		}
		
		public BoxLink(Anchor<EntityType> FromAnchor)
		{
			StartAnchor = FromAnchor;
			EndAnchor = null;
		}
		
		public virtual void OnConnectAnchors()
		{
			StartAnchor.Owner.OnConnectedAnchors(StartAnchor, EndAnchor);
			EndAnchor.Owner.OnConnectedAnchors(EndAnchor, StartAnchor);
		}
		
		public virtual void DrawLink(Vector2 Offset)
		{
			Color LineColor = Color.blue;
			
			if(bHighlighted)
			{
				LineColor = Color.red;
			}
			
			if(EndAnchor != null)
			{
				VisualScriptingDrawing.curveFromTo(EndAnchor.LastRect, StartAnchor.LastRect, LineColor, Color.green, Offset);
			}
			else
			{
				Vector2 MousePosition = InputState.GetLocalMousePosition(StartAnchor.Owner.Owner, Vector2.zero);
				Rect MouseRect = new Rect(MousePosition.x, MousePosition.y, 0.0f, 0.0f);
				
				if(StartAnchor.IsInput())
				{
					VisualScriptingDrawing.curveFromTo(MouseRect, StartAnchor.LastRect, LineColor, Color.green, Offset);
				}
				else
				{
					VisualScriptingDrawing.curveFromTo(StartAnchor.LastRect, MouseRect, LineColor, Color.green, Offset);
				}
			}
		}
		
		public virtual bool IsConnecting(Anchor<EntityType> TestAnchor)
		{
			return TestAnchor == StartAnchor || TestAnchor == EndAnchor;
		}
		
		public virtual void SetHighlighted(bool bShouldBeHighlighted)
		{
			bHighlighted = bShouldBeHighlighted;
		}
		
		public virtual void CleanupBeforeRemoval()
		{
			StartAnchor.CleanupBeforeRemoval();
			EndAnchor.CleanupBeforeRemoval();

			StartAnchor.Owner.OnDisconnectedAnchors(StartAnchor, EndAnchor);
			EndAnchor.Owner.OnDisconnectedAnchors(EndAnchor, StartAnchor);
		}
		
	}
}