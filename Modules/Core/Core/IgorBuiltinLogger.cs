using UnityEngine;
using UnityEditor;

namespace Igor
{
	public class IgorBuiltinLogger : IIgorLogger
	{
		public virtual void Log(string Message)
		{
			Debug.Log(Message);
		}

		public virtual void Log(IIgorModule Module, string Message)
		{
			if(Module != null)
			{
				Debug.Log(Module.GetModuleName() + " : " + Message);
			}
			else
			{
				Log(Message);
			}
		}

		public virtual void LogWarning(string Message)
		{
			Debug.LogWarning(Message);
		}

		public virtual void LogWarning(IIgorModule Module, string Message)
		{
			if(Module != null)
			{
				Debug.LogWarning(Module.GetModuleName() + " : " + Message);
			}
			else
			{
				LogWarning(Message);
			}
		}

		public virtual void LogError(string Message)
		{
			Debug.LogError(Message);
		}

		public virtual void LogError(IIgorModule Module, string Message)
		{
			if(Module != null)
			{
				Debug.LogError(Module.GetModuleName() + " : " + Message);
			}
			else
			{
				LogError(Message);
			}
		}

		public virtual void CriticalError(string Message)
		{
			Debug.LogError(Message);

			throw new UnityException(Message);
		}

		public virtual void CriticalError(IIgorModule Module, string Message)
		{
			if(Module != null)
			{
				Debug.LogError(Module.GetModuleName() + " : " + Message);

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