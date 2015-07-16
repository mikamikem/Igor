#if MONSTER_TEST_RUNTIME || UNITY_EDITOR
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.IO;
using System;
using System.Reflection;
using System.Xml.Serialization;

namespace Igor
{
	public class MonsterTestCore : IgorModuleBase
	{
		private static string _MonsterLocalDirectoryRoot = null;
		public static string MonsterLocalDirectoryRoot
		{
			get
			{
				if(_MonsterLocalDirectoryRoot == null)
				{
					if(ActiveMonsterRunner == null)
					{
						_MonsterLocalDirectoryRoot = "";
					}
					else
					{
						_MonsterLocalDirectoryRoot = ActiveMonsterRunner.GetConfigRoot();
					}
				}

				return _MonsterLocalDirectoryRoot;
			}
		}

		private static IMonsterRunner _ActiveMonsterRunner = null;
		public static IMonsterRunner ActiveMonsterRunner
		{
			get
			{
				if(_ActiveMonsterRunner == null)
				{
					List<Type> RunnerTypes = IgorRuntimeUtils.GetTypesInheritFrom<IMonsterRunner>();
					IMonsterRunner HighestPriorityInst = null;

					foreach(Type CurrentRunner in RunnerTypes)
					{
						IMonsterRunner CurrentRunnerInst = (IMonsterRunner)Activator.CreateInstance(CurrentRunner);

						if(HighestPriorityInst == null || CurrentRunnerInst.GetRunnerPriority() > HighestPriorityInst.GetRunnerPriority())
						{
							HighestPriorityInst = CurrentRunnerInst;
						}
					}

					_ActiveMonsterRunner = HighestPriorityInst;
				}

				return _ActiveMonsterRunner;
			}
		}

		public static StepID BuildTestableStep = new StepID("Make Testable", 0);
		public static StepID RunTestStep = new StepID("Run Test", 600);
		public static StepID CleanupTestableStep = new StepID("Cleanup Testable", 2000);
		public static StepID RebuildLaunchersStep = new StepID("Rebuild Monster Launchers", 2001);

		public static string RunTestFlag = "runtests";
		public static string TestNameFlag = "testname";
		public static string BuildTestableAppFlag = "maketestable";
		public static string RebuildLaunchersFlag = "rebuildmonsterlaunchers";
		public static string ExplicitAppPathFlag = "testexecutablepath";

		public static string TestToRun = "";
		public static bool bRunTests = false;
		public static bool bBuildTestableApp = false;

		public static string MonsterLauncherJobNameEnvVariable = "MonsterJobName";
		public static string MonsterStarterTestNameEnvVariable = "MonsterTestName";

		public static MonsterTestCore MonsterTestCoreInst = null;

		public override string GetModuleName()
		{
			return "MonsterTest.Core";
		}

		public override void RegisterModule()
		{
			IgorCore.RegisterNewModule(this);
		}

#if UNITY_EDITOR
		public override string DrawJobInspectorAndGetEnabledParams(string CurrentParams)
		{
			string EnabledParams = CurrentParams;

			EnabledParams = ActiveMonsterRunner.DrawJobInspectorAndGetEnabledParams(EnabledParams);

			return EnabledParams;
		}
#endif // UNITY_EDITOR

		public override void ProcessArgs(IIgorStepHandler StepHandler)
		{
			if(IgorJobConfig.IsBoolParamSet(RunTestFlag))
			{
				IgorCore.SetModuleActiveForJob(this);

				StepHandler.RegisterJobStep(RunTestStep, this, RunTest);
			}

			if(IgorJobConfig.IsBoolParamSet(BuildTestableAppFlag))
			{
				IgorCore.SetModuleActiveForJob(this);

				StepHandler.RegisterJobStep(BuildTestableStep, this, BuildTestable);
				StepHandler.RegisterJobStep(CleanupTestableStep, this, CleanupTestable);
			}

			ActiveMonsterRunner.ProcessArgs(StepHandler);
		}

		public override bool IsDependentOnModule(IIgorModule ModuleInst)
		{
			if(ModuleInst.GetModuleName() == "Configure.SetScriptingDefines")
			{
				return true;
			}

			return false;
		}

		public virtual bool BuildTestable()
		{
			MonsterTestCoreInst = this;
			
			bool bReturn = true;
#if UNITY_EDITOR
			bReturn = ActiveMonsterRunner.BuildTestable();
#endif // UNITY_EDITOR

			MonsterTestCoreInst = null;

			return bReturn;
		}

		public virtual bool RunTest()
		{
			MonsterTestCoreInst = this;

			bool bReturn = ActiveMonsterRunner.RunTest(IgorJobConfig.GetStringParam(TestNameFlag));

			MonsterTestCoreInst = null;

			return bReturn;
		}

		public virtual bool CleanupTestable()
		{
			bool bReturn = true;

			MonsterTestCoreInst = this;

#if UNITY_EDITOR
			bReturn = ActiveMonsterRunner.CleanupTestable();
#endif // UNITY_EDITOR

			MonsterTestCoreInst = null;

			return bReturn;
		}
	}
}

#endif // MONSTER_TEST_RUNTIME || UNITY_EDITOR
