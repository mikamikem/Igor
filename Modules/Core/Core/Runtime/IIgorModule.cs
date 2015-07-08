
namespace Igor
{
	public interface IIgorModule
	{
		string GetModuleName();

		void RegisterModule();
		void ProcessArgs(IIgorStepHandler StepHandler);

#if UNITY_EDITOR
		string DrawJobInspectorAndGetEnabledParams(string CurrentParams);
		bool ShouldDrawInspectorForParams(string CurrentParams);
#endif // UNITY_EDITOR

        void PostJobCleanup();
	}
}