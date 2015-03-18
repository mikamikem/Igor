
namespace Igor
{
	public interface IIgorModule
	{
		string GetModuleName();

		void RegisterModule();
		void ProcessArgs();

		string DrawJobInspectorAndGetEnabledParams(string CurrentParams);
	}
}