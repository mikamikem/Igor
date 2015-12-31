using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;

namespace Igor
{
	public class MonsterEditorRunner : IgorModuleBase, IMonsterRunner
	{
		public static string LastDisplayResolutionDialogFlag = "lastdisplayresolutiondialogvalue";

		public virtual int GetRunnerPriority()
		{
			return 1;
		}

		public virtual string GetConfigRoot()
		{
			return Path.Combine(IgorUpdater.BaseIgorDirectory, "Monster");
		}

		public virtual void ProcessArgs(IIgorStepHandler StepHandler)
		{
			if(IgorJobConfig.IsBoolParamSet(MonsterTestCore.RebuildLaunchersFlag))
			{
				IgorCore.SetModuleActiveForJob(this);

				StepHandler.RegisterJobStep(MonsterTestCore.RebuildLaunchersStep, this, RebuildLaunchers);
			}
		}

		public virtual string DrawJobInspectorAndGetEnabledParams(string CurrentParams)
		{
			// TODO: We also need to add in platform stuff to the monstertestcore module and to this inspector call
			// Also probably add in a checkbox to say if we should run in the editor for platforms that allow it...or would it ever make sense to trigger from the editor?
			string EnabledParams = CurrentParams;

			bool bIsBuilding = IgorRuntimeUtils.IsBoolParamSet(CurrentParams, IgorBuildCommon.BuildFlag);
			bool bBuildTestableApp = false;

			if(bIsBuilding)
			{
				bBuildTestableApp = DrawBoolParam(ref EnabledParams, "Built executable should be testable", MonsterTestCore.BuildTestableAppFlag);

				if(bBuildTestableApp)
				{
					DrawBoolParam(ref EnabledParams, "Rebuild Launchers On Build", MonsterTestCore.RebuildLaunchersFlag);

					if(GUILayout.Button("Rebuild Launcher Executables Now"))
					{
						RebuildLaunchers();
					}
				}
			}

			if(!bIsBuilding || bBuildTestableApp)
			{
				bool bRunTests = DrawBoolParam(ref EnabledParams, "Run tests", MonsterTestCore.RunTestFlag);

				if(bRunTests)
				{
					string CurrentTestName = IgorRuntimeUtils.GetStringParam(EnabledParams, MonsterTestCore.TestNameFlag);

					CurrentTestName = DrawTestDropdown(CurrentTestName);

					EnabledParams = IgorRuntimeUtils.SetStringParam(EnabledParams, MonsterTestCore.TestNameFlag, CurrentTestName);

					DrawStringParam(ref EnabledParams, "Explicit executable path", MonsterTestCore.ExplicitAppPathFlag);
				}
			}

			return EnabledParams;
		}

		public virtual bool BuildTestable()
		{
			return InternalBuildTestable();
		}

		public virtual bool InternalBuildTestable(bool bRunningTestInEditor = false)
		{
			if(!bRunningTestInEditor)
			{
				if(!IgorSetScriptingDefines.ExtraModuleParams.Contains("MONSTER_TEST_RUNTIME"))
				{
					IgorSetScriptingDefines.ExtraModuleParams += ";MONSTER_TEST_RUNTIME";
				}

				if(!IgorSetScriptingDefines.ExtraModuleParams.Contains("IGOR_RUNTIME"))
				{
					IgorSetScriptingDefines.ExtraModuleParams += ";IGOR_RUNTIME";
				}

				IgorJobConfig.SetStringParam(LastDisplayResolutionDialogFlag, PlayerSettings.displayResolutionDialog.ToString());

				PlayerSettings.displayResolutionDialog = ResolutionDialogSetting.Disabled;
			}

			if(!bRunningTestInEditor || (TestRunnerInst.CurrentTest != null && TestRunnerInst.CurrentTest.bForceLoadToFirstSceneInEditor))
			{
				string FirstLevelName = IgorUtils.GetFirstLevelName();

				if(FirstLevelName != "")
				{
					if(EditorApplication.currentScene != FirstLevelName)
					{
						EditorApplication.OpenScene(FirstLevelName);

						return false;
					}
				}
			}

			if(MonsterStarter.GetInstance() == null)
			{
				GameObject MonsterStarterInst = new GameObject("MonsterTestStarter");

				MonsterStarterInst.AddComponent<MonsterStarter>();

				EditorApplication.SaveScene();
			}

			if(!bRunningTestInEditor)
			{
				string StreamingAssetsFolder = Path.Combine("Assets", Path.Combine("StreamingAssets", Path.Combine("Igor", Path.Combine("Monster", "Config"))));

				if(Directory.Exists(StreamingAssetsFolder))
				{
					MonsterDebug.LogError("Attempting to overwrite the " + StreamingAssetsFolder + ", but it already exists!");

					IgorRuntimeUtils.DeleteDirectory(StreamingAssetsFolder);

					Directory.CreateDirectory(StreamingAssetsFolder);
				}
				else
				{
					Directory.CreateDirectory(StreamingAssetsFolder);
				}

				string ConfigRoot = Path.Combine(MonsterTestCore.MonsterLocalDirectoryRoot, "Config");

				if(Directory.Exists(ConfigRoot))
				{
					IgorRuntimeUtils.DirectoryCopy(ConfigRoot, StreamingAssetsFolder, true);
				}
			}

			return true;
		}

		public static MonsterTestRunner TestRunnerInst = null;

		public static bool bWasTestRunningInEditor = false;

		public virtual bool RunTest(string TestName)
		{
			if(bWasTestRunningInEditor && !EditorApplication.isPlaying && TestRunnerInst != null && TestRunnerInst.CurrentTest != null && TestRunnerInst.CurrentTest.bStartGameInEditor)
			{
				if(InternalCleanupTestable(true))
				{
					bWasTestRunningInEditor = false;

					return true;
				}

				return false;
			}

			if(MonsterStarter.GetInstance(false) == null || TestRunnerInst == null)
			{
				if(TestRunnerInst == null)
				{
					TestRunnerInst = new MonsterTestRunner();

					TestRunnerInst.PreloadTest(TestName);
				}

				if(TestRunnerInst.CurrentTest != null && TestRunnerInst.CurrentTest.bAllowRunningInEditor &&
					!InternalBuildTestable(true))
				{
					return false;
				}

				if(TestRunnerInst.CurrentTest != null && TestRunnerInst.CurrentTest.bStartGameInEditor)
				{
					EditorApplication.isPlaying = true;

					bWasTestRunningInEditor = true;

					if(MonsterStarter.GetInstance(false) == null)
					{
						return false;
					}
				}

				TestRunnerInst.StartTest(TestName);
			}

			if(TestRunnerInst.RunTests())
			{
				return TestRunnerInst.CurrentTest == null || !TestRunnerInst.CurrentTest.bAllowRunningInEditor || InternalCleanupTestable(true);
			}

			return false;
		}

		public virtual bool CleanupTestable()
		{
			return InternalCleanupTestable();
		}

		public virtual bool InternalCleanupTestable(bool bRunningTestInEditor = false)
		{
			if(!bRunningTestInEditor || (TestRunnerInst.CurrentTest != null && TestRunnerInst.CurrentTest.bForceLoadToFirstSceneInEditor))
			{
				string FirstLevelName = IgorUtils.GetFirstLevelName();

				if(FirstLevelName != "")
				{
					if(EditorApplication.currentScene != FirstLevelName)
					{
						EditorApplication.OpenScene(FirstLevelName);

						return false;
					}
				}
			}

			MonsterStarter[] Starters = GameObject.FindObjectsOfType<MonsterStarter>();

			foreach(MonsterStarter CurrentStarter in Starters)
			{
				GameObject.DestroyImmediate(CurrentStarter.gameObject);
			}

			if(!bRunningTestInEditor)
			{
				string StreamingAssetsFolder = Path.Combine("Assets", Path.Combine("StreamingAssets", Path.Combine("Igor", Path.Combine("Monster", "Config"))));

				if(Directory.Exists(StreamingAssetsFolder))
				{
					IgorRuntimeUtils.DeleteDirectory(StreamingAssetsFolder);
				}

				string LastValue = IgorJobConfig.GetStringParam(LastDisplayResolutionDialogFlag);

				switch(LastValue)
				{
				case "Disabled":
					PlayerSettings.displayResolutionDialog = ResolutionDialogSetting.Disabled;
					break;
				case "Enabled":
					PlayerSettings.displayResolutionDialog = ResolutionDialogSetting.Enabled;
					break;
				case "HiddenByDefault":
					PlayerSettings.displayResolutionDialog = ResolutionDialogSetting.HiddenByDefault;
					break;
				}
			}

			return true;
		}

		public virtual string DrawTestDropdown(string CurrentTestName)
		{
			List<string> TestNames = new List<string>();

			if(MonsterTestList.GetInstance() != null)
			{
				for(int CurrentIndex = 0; CurrentIndex < MonsterTestList.GetInstance().EditorGetListCount(); ++CurrentIndex)
				{
					MonsterTest CurrentValue = MonsterTestList.GetInstance().EditorGetValueAtIndex(CurrentIndex);

					if(CurrentValue != null)
					{
						TestNames.Add(CurrentValue.GetFilename());
					}
				}
			}

			TestNames.Add("Create new test");

			int CurrentTestIndex = TestNames.IndexOf(CurrentTestName);

			if(CurrentTestIndex == -1)
			{
				CurrentTestIndex = 0;
			}

			int NewTestIndex = EditorGUILayout.Popup("Test to run", CurrentTestIndex, TestNames.ToArray());

			bool bCreatedNew = false;

            if(NewTestIndex != CurrentTestIndex || TestNames.Count == 1)
            {
            	if(NewTestIndex == (TestNames.Count - 1))
            	{
            		if(TestNames.Count > 1 || MonsterTestListWindow.GetInstance() == null)
            		{
						MonsterTestListWindow.Init();
					}

					bCreatedNew = true;
				}

                CurrentTestIndex = NewTestIndex;
            }

            if(!bCreatedNew)
            {
            	CurrentTestName = TestNames[CurrentTestIndex];
            }

            return CurrentTestName;
		}

		public virtual bool RebuildLaunchers()
		{
			BuildLauncher(BuildTarget.StandaloneOSXIntel64);

			BuildLauncher(BuildTarget.StandaloneWindows64);

			return true;
		}

		public virtual string GetBuildMethodName(BuildTarget Target)
		{
			switch(Target)
			{
			case BuildTarget.StandaloneOSXIntel64:
				return "Igor.MonsterEditorRunner.LauncherProjectBuildOSXLauncher";
			case BuildTarget.StandaloneWindows64:
				return "Igor.MonsterEditorRunner.LauncherProjectBuildWindowsLauncher";
			}

			return "";
		}

		public virtual void BuildLauncher(BuildTarget TargetTestPlatform)
		{
			MonsterDebug.Log("Building launcher for platform " + TargetTestPlatform);

			string MethodName = GetBuildMethodName(TargetTestPlatform);

			if(MethodName == "")
			{
				MonsterDebug.LogError("Test platform " + TargetTestPlatform + " is not supported yet.  Please add support for it!");
			}

			string MonsterLauncherProjectPath = Path.Combine(Application.temporaryCachePath, "MonsterLauncher");

			MonsterDebug.Log("Creating new project at " + MonsterLauncherProjectPath);

			if(Directory.Exists(MonsterLauncherProjectPath))
			{
				MonsterDebug.Log("Cleaning up old project first!");

				IgorRuntimeUtils.DeleteDirectory(MonsterLauncherProjectPath);
			}

			Directory.CreateDirectory(MonsterLauncherProjectPath);

			string LauncherProjectAssetsIgorFolder = Path.Combine(MonsterLauncherProjectPath, Path.Combine("Assets", "Igor"));

			Directory.CreateDirectory(LauncherProjectAssetsIgorFolder);

			MonsterDebug.Log("Copying project files.");

			IgorRuntimeUtils.DirectoryCopy(Path.Combine(Path.GetFullPath("."), Path.Combine("Assets", "Igor")), LauncherProjectAssetsIgorFolder, true);

			string OldLaunchersFolder = Path.Combine(LauncherProjectAssetsIgorFolder, Path.Combine("Monster", "Launchers"));

			IgorRuntimeUtils.DeleteDirectory(OldLaunchersFolder);

			MonsterDebug.Log("Copying Igor config.");

			string StreamingAssetsFolder = Path.Combine(MonsterLauncherProjectPath, Path.Combine("Assets", Path.Combine("StreamingAssets", "Igor")));

			Directory.CreateDirectory(StreamingAssetsFolder);

			if(File.Exists(IgorConfig.DefaultConfigPath))
			{
				IgorRuntimeUtils.CopyFile(IgorConfig.DefaultConfigPath, Path.Combine(StreamingAssetsFolder, IgorConfig.IgorConfigFilename));
			}

			string BuildLauncherOutput = "";
			string BuildLauncherError = "";

			MonsterDebug.Log("Attempting to build launcher.");

			int ReturnCode = IgorRuntimeUtils.RunProcessCrossPlatform(EditorApplication.applicationPath + "/Contents/MacOS/Unity", EditorApplication.applicationPath,
				"-projectPath \"" + MonsterLauncherProjectPath + "\" -buildmachine -executeMethod " + MethodName + " -logfile Monster.log",
				MonsterLauncherProjectPath, ref BuildLauncherOutput, ref BuildLauncherError);

			if(ReturnCode != 0)
			{
				MonsterDebug.LogError("Something went wrong with the build!  Returned error code " + ReturnCode + "\n\nOutput:\n" + BuildLauncherOutput + "\n\nError:\n" + BuildLauncherError);

				return;
			}

			MonsterDebug.Log("Launcher successfully built!");

			string MonsterLauncherPath = Path.Combine(MonsterTestCore.MonsterLocalDirectoryRoot, "Launchers");

			if(!Directory.Exists(MonsterLauncherPath))
			{
				Directory.CreateDirectory(MonsterLauncherPath);
			}

			MonsterDebug.Log("Copying launcher back to project.");

			CopyLauncherToProjectPath(TargetTestPlatform, MonsterLauncherProjectPath, MonsterLauncherPath);

			IgorRuntimeUtils.DeleteDirectory(MonsterLauncherProjectPath);

			string MonsterRunPyFile = Path.Combine(MonsterLauncherPath, "MonsterRun.py");

			if(File.Exists(MonsterRunPyFile))
			{
				IgorRuntimeUtils.DeleteFile(MonsterRunPyFile);
			}

			string MonsterRunPyFileLatest = Path.Combine(IgorUpdater.LocalModuleRoot, Path.Combine("MonsterTest", Path.Combine("Core", Path.Combine("Runtime", "MonsterRun.py"))));

			if(File.Exists(MonsterRunPyFileLatest))
			{
				MonsterDebug.Log("Copying latest MonsterRun.py to the Launchers folder.");

				IgorRuntimeUtils.CopyFile(MonsterRunPyFileLatest, MonsterRunPyFile);
			}

			MonsterDebug.Log("Done building launcher for platform " + TargetTestPlatform);
		}

		public virtual void CopyLauncherToProjectPath(BuildTarget TargetTestPlatform, string TempPath, string LocalPath)
		{
			if(TargetTestPlatform == BuildTarget.StandaloneOSXIntel64)
			{
				string ZipPath = Path.Combine(TempPath, "MonsterLauncherOSX.zip");
				List<string> ZipFileList = new List<string>();

				foreach(string FilePath in IgorRuntimeUtils.GetListOfFilesAndDirectoriesInDirectory(Path.Combine(TempPath, "MonsterLauncher.app"), true, false, true))
				{
					ZipFileList.Add("MonsterLauncher.app/" + FilePath);
				}

				IgorZip.ZipFilesCrossPlatform(this, ZipFileList, ZipPath, false, TempPath);

				string ZipLocalPath = Path.Combine(LocalPath, "MonsterLauncherOSX.zip");

				if(File.Exists(ZipLocalPath))
				{
					IgorRuntimeUtils.DeleteFile(ZipLocalPath);
				}

				IgorRuntimeUtils.CopyFile(ZipPath, ZipLocalPath);
			}
			else if(TargetTestPlatform == BuildTarget.StandaloneWindows64)
			{
				string ZipPath = Path.Combine(TempPath, "MonsterLauncherWindows.zip");
				List<string> ZipFileList = new List<string>();

				foreach(string FilePath in IgorRuntimeUtils.GetListOfFilesAndDirectoriesInDirectory(Path.Combine(TempPath, "MonsterLauncher_Data"), true, false, true))
				{
					ZipFileList.Add("MonsterLauncher_Data/" + FilePath);
				}

				ZipFileList.Add("MonsterLauncher.exe");

				IgorZip.ZipFilesCrossPlatform(this, ZipFileList, ZipPath, false, TempPath);

				string ZipLocalPath = Path.Combine(LocalPath, "MonsterLauncherWindows.zip");

				if(File.Exists(ZipLocalPath))
				{
					IgorRuntimeUtils.DeleteFile(ZipLocalPath);
				}

				IgorRuntimeUtils.CopyFile(ZipPath, ZipLocalPath);
			}
		}

		public static void LauncherProjectSharedProjectPrep()
		{
			GameObject MonsterLauncherInst = new GameObject("MonsterTestLauncher");

			MonsterLauncherInst.AddComponent<MonsterLauncher>();

			string LauncherScenePath = Path.Combine("Assets", "Launcher.unity");

			EditorApplication.SaveScene(LauncherScenePath);

			EditorBuildSettingsScene[] NewList = new EditorBuildSettingsScene[1];

			NewList[0] = new EditorBuildSettingsScene(LauncherScenePath, true);

			EditorBuildSettings.scenes = NewList;

            foreach(BuildTargetGroup group in IgorSetScriptingDefines.BuildTargets)
            {
                if(group != BuildTargetGroup.Unknown)
                {
                    PlayerSettings.SetScriptingDefineSymbolsForGroup(group, "IGOR_RUNTIME;MONSTER_TEST_RUNTIME");
                }
            }
            
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
		}

		public static void LauncherProjectBuildOSXLauncher()
		{
			LauncherProjectSharedProjectPrep();

			LauncherProjectBuildLauncher(BuildTarget.StandaloneOSXIntel64, "MonsterLauncher.app");
		}

		public static void LauncherProjectBuildWindowsLauncher()
		{
			LauncherProjectSharedProjectPrep();

			LauncherProjectBuildLauncher(BuildTarget.StandaloneWindows64, "MonsterLauncher.exe");
		}

		public static void LauncherProjectBuildLauncher(BuildTarget Target, string BuiltName)
		{
#if !UNITY_4_3
            BuiltName = System.IO.Path.Combine(System.IO.Path.GetFullPath("."), BuiltName);	
#endif
            BuildPipeline.BuildPlayer(IgorUtils.GetLevels(), BuiltName, Target, BuildOptions.Development | BuildOptions.AllowDebugging);

            EditorApplication.Exit(0);
		}
	}
}
