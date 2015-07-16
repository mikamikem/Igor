#if MONSTER_TEST_RUNTIME || UNITY_EDITOR
using UnityEngine;
using System;
using System.Collections.Generic;

namespace Igor
{
	public class MonsterDebug
	{
		public static void Log(string Message)
		{
			if(MonsterTestCore.MonsterTestCoreInst != null && MonsterTestManager.GetActiveInstance() != null)
			{
				if(IgorDebug.Logger != null)
				{
					IgorDebug.Logger.Log(MonsterTestCore.MonsterTestCoreInst, " - " + MonsterTestManager.GetActiveInstance().GetOwnerFilename() + Message);
				}
				else
				{
					Debug.Log("Igor Log: " + MonsterTestCore.MonsterTestCoreInst + " - " + MonsterTestManager.GetActiveInstance().GetOwnerFilename() + " : " + Message);
				}
			}
			else
			{
				CoreLog(Message);
			}
		}

		public static void LogWarning(string Message)
		{
			if(MonsterTestCore.MonsterTestCoreInst != null && MonsterTestManager.GetActiveInstance() != null)
			{
				if(IgorDebug.Logger != null)
				{
					IgorDebug.Logger.LogWarning(MonsterTestCore.MonsterTestCoreInst, " - " + MonsterTestManager.GetActiveInstance().GetOwnerFilename() + Message);
				}
				else
				{
					Debug.LogWarning("Igor Warning: " + MonsterTestCore.MonsterTestCoreInst + " - " + MonsterTestManager.GetActiveInstance().GetOwnerFilename() + " : " + Message);
				}
			}
			else
			{
				CoreLog(Message);
			}
		}

		public static void LogError(string Message)
		{
			if(MonsterTestCore.MonsterTestCoreInst != null && MonsterTestManager.GetActiveInstance() != null)
			{
				if(IgorDebug.Logger != null)
				{
					IgorDebug.Logger.LogError(MonsterTestCore.MonsterTestCoreInst, " - " + MonsterTestManager.GetActiveInstance().GetOwnerFilename() + Message);
				}
				else
				{
					Debug.LogError("Igor Error: " + MonsterTestCore.MonsterTestCoreInst + " - " + MonsterTestManager.GetActiveInstance().GetOwnerFilename() + " : " + Message);
				}
			}
			else
			{
				CoreLog(Message);
			}
		}

		public static void CriticalError(string Message)
		{
			if(MonsterTestCore.MonsterTestCoreInst != null && MonsterTestManager.GetActiveInstance() != null)
			{
				if(IgorDebug.Logger != null)
				{
					IgorDebug.Logger.CriticalError(MonsterTestCore.MonsterTestCoreInst, " - " + MonsterTestManager.GetActiveInstance().GetOwnerFilename() + Message);
				}
				else
				{
					Debug.LogError("Igor Error: " + MonsterTestCore.MonsterTestCoreInst + " - " + MonsterTestManager.GetActiveInstance().GetOwnerFilename() + " : " + Message);

					throw new UnityException(MonsterTestCore.MonsterTestCoreInst + " - " + MonsterTestManager.GetActiveInstance().GetOwnerFilename() + " : " + Message);
				}
			}
			else
			{
				CoreLog(Message);
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

#endif // MONSTER_TEST_RUNTIME || UNITY_EDITOR
