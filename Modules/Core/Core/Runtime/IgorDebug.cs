#if IGOR_RUNTIME || UNITY_EDITOR
using UnityEngine;
using System;
using System.Collections.Generic;

namespace Igor
{
	public class IgorDebug
	{
		static IgorDebug()
		{
			foreach(Type CurrentType in LoggerTypes)
			{
				IIgorLogger CurrentLogger = (IIgorLogger)Activator.CreateInstance(CurrentType);

				if(Logger == null || Logger.LoggerPriority() < CurrentLogger.LoggerPriority())
				{
					Logger = CurrentLogger;
				}
			}
		}

		public static IIgorLogger Logger = null;

        static List<Type> _LoggerTypes;
        static List<Type> LoggerTypes
        {
            get
            {
                if(_LoggerTypes == null)
                    _LoggerTypes = IgorRuntimeUtils.GetTypesInheritFrom<IIgorLogger>();
                return _LoggerTypes;
            }
        }

		public static void Log(IIgorModule Module, string Message)
		{
			if(Logger != null)
			{
				Logger.Log(Module, Message);
			}
			else
			{
				Debug.Log("Igor Log: " + Module + " : " + Message);
			}
		}

		public static void LogWarning(IIgorModule Module, string Message)
		{
			if(Logger != null)
			{
				Logger.LogWarning(Module, Message);
			}
			else
			{
				Debug.LogWarning("Igor Warning: " + Module + " : " + Message);
			}
		}

		public static void LogError(IIgorModule Module, string Message)
		{
			if(Logger != null)
			{
				Logger.LogError(Module, Message);
			}
			else
			{
				Debug.LogError("Igor Error: " + Module + " : " + Message);
			}
		}

		public static void CriticalError(IIgorModule Module, string Message)
		{
			if(Logger != null)
			{
				Logger.CriticalError(Module, Message);
			}
			else
			{
				Debug.LogError("Igor Error: " + Module + " : " + Message);

				throw new UnityException(Module + " : " + Message);
			}
		}

		public static void CoreLog(string Message)
		{
			if(IgorDebug.Logger != null)
			{
				IgorDebug.Logger.Log(Message);
			}
			else
			{
				Debug.Log("Igor Log: " + Message);
			}
		}

		public static void CoreLogWarning(string Message)
		{
			if(IgorDebug.Logger != null)
			{
				IgorDebug.Logger.LogWarning(Message);
			}
			else
			{
				Debug.LogWarning("Igor Warning: " + Message);
			}
		}

		public static void CoreLogError(string Message)
		{
			if(IgorDebug.Logger != null)
			{
				IgorDebug.Logger.LogError(Message);
			}
			else
			{
				Debug.LogError("Igor Error: " + Message);
			}
		}

		public static void CoreCriticalError(string Message)
		{
			if(IgorDebug.Logger != null)
			{
				IgorDebug.Logger.CriticalError(Message);
			}
			else
			{
				Debug.LogError("Igor Error: " + Message);

				throw new UnityException(Message);
			}
		}
	}
}

#endif // IGOR_RUNTIME || UNITY_EDITOR
