#if MONSTER_TEST_RUNTIME || UNITY_EDITOR
using UnityEngine;
using System;
using System.IO;

namespace Igor
{
	public class MonsterStandaloneRunner : IMonsterRunner
	{
		public virtual int GetRunnerPriority()
		{
			return 0;
		}

		public virtual string GetConfigRoot()
		{
			return Path.Combine(Application.streamingAssetsPath, Path.Combine("Igor", "Monster"));;
		}

		public virtual void ProcessArgs(IIgorStepHandler StepHandler)
		{
		}

#if UNITY_EDITOR
		public virtual string DrawJobInspectorAndGetEnabledParams(string CurrentParams)
		{
			return CurrentParams;
		}

		public virtual bool BuildTestable()
		{
			return true;
		}

		public virtual bool CleanupTestable()
		{
			return true;
		}
#endif // UNITY_EDITOR


		public virtual bool RunTest(string TestName)
		{
			MonsterDebug.Log("Attempting to run test " + TestName + " on a standalone copy of the game.");

			Environment.SetEnvironmentVariable(MonsterTestCore.MonsterStarterTestNameEnvVariable, TestName);

			string AppPath = "";

			if(IgorJobConfig.IsStringParamSet(MonsterTestCore.ExplicitAppPathFlag))
			{
				AppPath = IgorJobConfig.GetStringParam(MonsterTestCore.ExplicitAppPathFlag);
			}
			else
			{
				foreach(string CurrentProduct in IgorCore.GetModuleProducts())
				{
					if(CurrentProduct.Contains(".app"))
					{
						AppPath = CurrentProduct.Substring(0, CurrentProduct.IndexOf(".app") + 4);
					}
					else if(CurrentProduct.EndsWith(".exe"))
					{
						AppPath = CurrentProduct;
					}
				}
			}

			if(AppPath.EndsWith(".app"))
			{
				AppPath = Path.Combine(AppPath, Path.Combine("Contents", Path.Combine("MacOS", AppPath.Substring(AppPath.LastIndexOf('/') + 1, AppPath.Length - AppPath.LastIndexOf('/') - 5))));
			}

			string AppOutput = "";
			string AppError = "";

			int RunAppRC = IgorRuntimeUtils.RunProcessCrossPlatform(AppPath, AppPath, "", Path.GetFullPath("."), ref AppOutput, ref AppError);

			if(RunAppRC != 0)
			{
				MonsterDebug.LogError("Failed to run test.  App retruned RC " + RunAppRC + "!\n\nOutput:\n" + AppOutput + "\n\nError:\n" + AppError);

				return true;
			}

			MonsterDebug.Log("Test ran successfully!\n\nOutput:\n" + AppOutput + "\n\nError:\n" + AppError);

			return true;
		}
	}
}

#endif // MONSTER_TEST_RUNTIME || UNITY_EDITOR
