using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;

namespace Igor
{
	public class InputState {
		
		public enum MouseButton
		{
			Mouse_Left,
			Mouse_Right,
			Mouse_Middle
		};
		
		public enum ModifierKeys
		{
			Key_Alt,
			Key_Control,
			Key_Shift,
			Key_Command
		}
		
		private static bool[] MouseButtonDown = new bool[3];
		private static Dictionary<object, Vector2> MousePosition = new Dictionary<object, Vector2>();
		private static Dictionary<object, Vector2> LastUpPosition = new Dictionary<object, Vector2>();
		private static Dictionary<object, Vector2> Origins = new Dictionary<object, Vector2>();
		private static bool[] ModifiersDown = new bool[4];
		private static bool[] KeysDown = new bool[(int)KeyCode.Joystick8Button19];
		private static double[] LastClickTime = new double[3];
		private static double DoubleClickMaxDelay = 0.3;
		private static bool[] LastClickWasDoubleClick = new bool[3];
		private static bool[] MouseButtonDownHandled = new bool[3];
		private static bool[] MouseButtonUpHandled = new bool[3];
		
		public static void Update(object CallingObject, Vector2 Origin)
		{
			if(Event.current.type == EventType.MouseDown)
			{
				if(!MouseButtonDown[Event.current.button])
				{
					MouseButtonDownHandled[Event.current.button] = false;
					
					if(LastClickTime[Event.current.button] + DoubleClickMaxDelay > EditorApplication.timeSinceStartup)
					{
						LastClickWasDoubleClick[Event.current.button] = true;
					}
					else
					{
						LastClickWasDoubleClick[Event.current.button] = false;
					}
				}
				
				MouseButtonDown[Event.current.button] = true;
				
				LastClickTime[Event.current.button] = EditorApplication.timeSinceStartup;
			}
			else if(Event.current.type == EventType.MouseUp)
			{
				if(MouseButtonDown[Event.current.button])
				{
					MouseButtonUpHandled[Event.current.button] = false;

					if(!LastUpPosition.ContainsKey(CallingObject))
					{
						LastUpPosition.Add(CallingObject, Vector2.zero);
					}
					
					LastUpPosition[CallingObject] = Event.current.mousePosition;
				}
					
				MouseButtonDown[Event.current.button] = false;
			}
			
			ModifiersDown[(int)ModifierKeys.Key_Alt] = Event.current.alt;
			ModifiersDown[(int)ModifierKeys.Key_Control] = Event.current.control;
			ModifiersDown[(int)ModifierKeys.Key_Shift] = Event.current.shift;
			ModifiersDown[(int)ModifierKeys.Key_Command] = Event.current.command;

			if(Event.current.type == EventType.KeyDown)
			{
				SetKeyDown(Event.current.keyCode, true);
			}
			else if(Event.current.type == EventType.KeyUp)
			{
				SetKeyDown(Event.current.keyCode, false);
			}
			
			if(!MousePosition.ContainsKey(CallingObject))
			{
				MousePosition.Add(CallingObject, Vector2.zero);
			}
			
			MousePosition[CallingObject] = Event.current.mousePosition;

			if(!Origins.ContainsKey(CallingObject))
			{
				Origins.Add(CallingObject, Vector2.zero);
			}
			
			Origins[CallingObject] = Origin;
		}
		
		public static bool WasLastMouseDownHandled(MouseButton Button)
		{
			return MouseButtonDownHandled[(int)Button] && MouseButtonDown[(int)Button];
		}
		
		public static void HandledMouseDown(MouseButton Button)
		{
			MouseButtonDownHandled[(int)Button] = true;
		}
		
		public static bool WasLastMouseUpHandled(object CallingObject, MouseButton Button)
		{
			return (MouseButtonUpHandled[(int)Button] && !MouseButtonDown[(int)Button]) ||
				   (GetLocalMousePosition(CallingObject, Vector2.zero) - GetLastUpPosition(CallingObject)).sqrMagnitude > 10.0f;
		}
		
		public static void HandledMouseUp(MouseButton Button)
		{
			MouseButtonUpHandled[(int)Button] = true;
		}
		
		public static bool WasLastClickDoubleClick(MouseButton Button)
		{
			return LastClickWasDoubleClick[(int)Button];
		}
		
		public static void HandledDoubleClick(MouseButton Button)
		{
			LastClickWasDoubleClick[(int)Button] = false;
		}
		
		public static bool IsMouseButtonDown(MouseButton Button)
		{
			return MouseButtonDown[(int)Button];
		}
		
		public static Vector2 GetLocalMousePosition(object CallingObject, Vector2 Offset)
		{
			if(MousePosition.ContainsKey(CallingObject) && Origins.ContainsKey(CallingObject))
			{
				return MousePosition[CallingObject] - Offset + Origins[CallingObject];
			}
			
			return Offset;
		}
		
		public static Vector2 GetLastUpPosition(object CallingObject)
		{
			if(LastUpPosition.ContainsKey(CallingObject) && Origins.ContainsKey(CallingObject))
			{
				return LastUpPosition[CallingObject] + Origins[CallingObject];
			}
			
			return Vector2.zero;
		}
		
		public static bool IsModifierDown(ModifierKeys Key)
		{
			return ModifiersDown[(int)Key];
		}

		public static void SetKeyDown(KeyCode Key, bool bIsDown)
		{
			if((int)Key < KeysDown.Length)
			{
				KeysDown[(int)Key] = bIsDown;
			}
		}

		public static void HandleKeyDown(KeyCode Key)
		{
			if((int)Key < KeysDown.Length)
			{
				KeysDown[(int)Key] = false;
			}
		}
		
		public static bool IsKeyDown(KeyCode Key)
		{
			if((int)Key < KeysDown.Length)
			{
				return KeysDown[(int)Key];
			}

			return false;
		}

	}
}
