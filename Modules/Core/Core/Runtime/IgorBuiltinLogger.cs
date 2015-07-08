using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif // UNITY_EDITOR

namespace Igor
{
	public class IgorBuiltinLogger : IIgorLogger
	{
		public virtual void Log(string Message)
		{
			Debug.Log("Igor Log: " + Message);
		}

		public virtual void Log(IIgorModule Module, string Message)
		{
			if(Module != null)
			{
				Debug.Log("Igor Log: " + Module.GetModuleName() + " : " + Message);
			}
			else
			{
				Log(Message);
			}
		}

		public virtual void LogWarning(string Message)
		{
			Debug.LogWarning("Igor Warning: " + Message);
		}

		public virtual void LogWarning(IIgorModule Module, string Message)
		{
			if(Module != null)
			{
				Debug.LogWarning("Igor Warning: " + Module.GetModuleName() + " : " + Message);
			}
			else
			{
				LogWarning(Message);
			}
		}

		public virtual void LogError(string Message)
		{
			Debug.LogError("Igor Error: " + Message);
		}

		public virtual void LogError(IIgorModule Module, string Message)
		{
			if(Module != null)
			{
				Debug.LogError("Igor Error: " + Module.GetModuleName() + " : " + Message);
			}
			else
			{
				LogError(Message);
			}
		}

		public virtual void CriticalError(string Message)
		{
			Debug.LogError("Igor Error: " + Message);

			throw new UnityException(Message);
		}

		public virtual void CriticalError(IIgorModule Module, string Message)
		{
			if(Module != null)
			{
				Debug.LogError("Igor Error: " + Module.GetModuleName() + " : " + Message);

				throw new UnityException(Message);
			}
			else
			{
				LogError(Message);
			}
		}

		public virtual int LoggerPriority()
		{
			return 0;
		}
	}
}