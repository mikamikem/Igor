#if IGOR_RUNTIME || UNITY_EDITOR

namespace Igor
{
	public interface IIgorLogger
	{
		void Log(string Message);
		void Log(IIgorModule Module, string Message);
		void LogWarning(string Message);
		void LogWarning(IIgorModule Module, string Message);
		void LogError(string Message);
		void LogError(IIgorModule Module, string Message);
		void CriticalError(string Message);
		void CriticalError(IIgorModule Module, string Message);

		int LoggerPriority(); // Higher has priority
	}
}

#endif // IGOR_RUNTIME || UNITY_EDITOR
