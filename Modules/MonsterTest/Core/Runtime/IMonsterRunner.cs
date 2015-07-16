#if MONSTER_TEST_RUNTIME || UNITY_EDITOR

namespace Igor
{
	public interface IMonsterRunner
	{
		int GetRunnerPriority();// This allows us to find and prioritize the Editor implementation if it's avaialable.

		string GetConfigRoot();

		void ProcessArgs(IIgorStepHandler StepHandler);

#if UNITY_EDITOR
		string DrawJobInspectorAndGetEnabledParams(string CurrentParams);

		bool BuildTestable();

		bool CleanupTestable();
#endif // UNITY_EDITOR

		bool RunTest(string TestName);
	}
}

#endif // MONSTER_TEST_RUNTIME || UNITY_EDITOR
