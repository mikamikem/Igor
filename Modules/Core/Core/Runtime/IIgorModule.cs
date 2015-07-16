#if IGOR_RUNTIME || UNITY_EDITOR

namespace Igor
{
	public interface IIgorModule
	{
		string GetModuleName();

		void RegisterModule();
		void ProcessArgs(IIgorStepHandler StepHandler);
		bool IsDependentOnModule(IIgorModule ModuleInst);
        
#if UNITY_EDITOR
		string DrawJobInspectorAndGetEnabledParams(string CurrentParams);
		bool ShouldDrawInspectorForParams(string CurrentParams);
#endif // UNITY_EDITOR

		void PostJobCleanup();
	}
}

#endif // IGOR_RUNTIME || UNITY_EDITOR
