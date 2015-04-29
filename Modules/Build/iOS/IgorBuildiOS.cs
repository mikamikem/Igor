using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.IO;
using System;
using System.Reflection;
using System.Xml.Serialization;

namespace Igor
{
	public class IgorBuildiOS : IgorModuleBase
	{
		public static StepID FixupXCodeProjStep = new StepID("FixupXCodeProj", 600);
		public static StepID BuildXCodeProjStep = new StepID("BuildXCodeProj", 700);

		public static string iOSDevTeamIDFlag = "iOSDevTeamID";

		public override string GetModuleName()
		{
			return "Build.iOS";
		}

		public override void RegisterModule()
		{
			IgorCore.RegisterNewModule(this);

			BuildOptionsDelegates.Clear();

			IgorBuildCommon.RegisterBuildPlatforms(new string[] {"iOS"});
		}

		public override void ProcessArgs(IIgorStepHandler StepHandler)
		{
			if(IgorJobConfig.IsBoolParamSet(IgorBuildCommon.BuildFlag))
			{
				IgorCore.SetModuleActiveForJob(this);

				string Platform = IgorJobConfig.GetStringParam(IgorBuildCommon.PlatformFlag);

				bool bIOS = false;

				if(Platform == "iOS")
				{
					JobBuildTarget = BuildTarget.iPhone;
					bIOS = true;

					StepHandler.RegisterJobStep(IgorBuildCommon.SwitchPlatformStep, this, SwitchPlatforms);
					StepHandler.RegisterJobStep(IgorBuildCommon.BuildStep, this, BuildiOS);
					StepHandler.RegisterJobStep(FixupXCodeProjStep, this, FixupXCodeProj);
					StepHandler.RegisterJobStep(BuildXCodeProjStep, this, BuildXCodeProj);

					if(IgorJobConfig.IsStringParamSet(iOSDevTeamIDFlag))
					{
						DevTeamID = IgorJobConfig.GetStringParam(iOSDevTeamIDFlag);
					}
					else
					{
						DevTeamID = IgorConfig.GetModuleString(this, iOSDevTeamIDFlag);
					}

					if(DevTeamID == "")
					{
						LogWarning("Your Dev Team ID hasn't been set!  Your build may not sign correctly.");
					}
				}
			}
		}

		public override string DrawJobInspectorAndGetEnabledParams(string CurrentParams)
		{
			string EnabledParams = CurrentParams;

			DrawStringConfigParam(ref EnabledParams, "Built name", IgorBuildCommon.BuiltNameFlag, "BuiltiOSName");

			DrawStringConfigParam(ref EnabledParams, "iOS Dev Team ID", iOSDevTeamIDFlag, iOSDevTeamIDFlag);

			return EnabledParams;
		}

		public override bool ShouldDrawInspectorForParams(string CurrentParams)
		{
			bool bBuilding = IgorUtils.IsBoolParamSet(CurrentParams, IgorBuildCommon.BuildFlag);
			bool bRecognizedPlatform = false;

			if(bBuilding)
			{
				string Platform = IgorUtils.GetStringParam(CurrentParams, IgorBuildCommon.PlatformFlag);

				if(Platform == "iOS")
				{
					bRecognizedPlatform = true;
				}
			}

			return bBuilding && bRecognizedPlatform;
		}

		public BuildTarget JobBuildTarget = BuildTarget.iPhone;
		public List<IgorBuildCommon.GetExtraBuildOptions> BuildOptionsDelegates = new List<IgorBuildCommon.GetExtraBuildOptions>();
		public string DevTeamID = "";

		public virtual void AddDelegateCallback(IgorBuildCommon.GetExtraBuildOptions NewDelegate)
		{
			if(!BuildOptionsDelegates.Contains(NewDelegate))
			{
				BuildOptionsDelegates.Add(NewDelegate);
			}
		}

		public virtual string GetBuiltNameForTarget(BuildTarget NewTarget)
		{
			string BuiltName = "";

			bool biOS = false;

			if(NewTarget == BuildTarget.iPhone)
			{
				BuiltName = GetConfigString("BuiltiOSName");
				biOS = true;
			}

			if(BuiltName == "")
			{
				BuiltName = Path.GetFileName(EditorUserBuildSettings.GetBuildLocation(NewTarget));
			}

			if(BuiltName == "")
			{
				if(biOS)
				{
					BuiltName = "iOS";
				}
			}

			return BuiltName;
		}

		public virtual BuildOptions GetExternalBuildOptions(BuildTarget CurrentTarget)
		{
			BuildOptions ExtraOptions = BuildOptions.None;

			foreach(IgorBuildCommon.GetExtraBuildOptions CurrentDelegate in BuildOptionsDelegates)
			{
				ExtraOptions |= CurrentDelegate(CurrentTarget);
			}

			return ExtraOptions;
		}

		public virtual bool SwitchPlatforms()
		{
			Log("Switching platforms to " + JobBuildTarget);

			EditorUserBuildSettings.SwitchActiveBuildTarget(JobBuildTarget);

			return true;
		}

		public virtual bool BuildiOS()
		{
			Log("Building iOS build (Target:" + JobBuildTarget + ")");

			return Build(BuildOptions.SymlinkLibraries);
		}

		public virtual bool Build(BuildOptions PlatformSpecificOptions)
		{
			string XCodeProjDirectory = "iOS";

			Log("XCode project destination directory is: " + XCodeProjDirectory);

			BuildOptions AllOptions = PlatformSpecificOptions;

			AllOptions |= GetExternalBuildOptions(JobBuildTarget);

#if UNITY_4_3
			BuildPipeline.BuildPlayer(IgorBuildCommon.GetLevels(), XCodeProjDirectory, JobBuildTarget, AllOptions);
#else
			BuildPipeline.BuildPlayer(IgorBuildCommon.GetLevels(), System.IO.Path.Combine(System.IO.Path.GetFullPath("."), XCodeProjDirectory), JobBuildTarget, AllOptions);
#endif

			List<string> BuiltFiles = new List<string>();

			BuiltFiles.Add(XCodeProjDirectory);

			IgorBuildCommon.SetNewBuildProducts(BuiltFiles);

			return true;
		}

		public virtual bool FixupXCodeProj()
		{
			List<string> BuildProducts = IgorBuildCommon.GetBuildProducts();

			if(BuildProducts.Count > 0)
			{
				string ProjectPath = Path.Combine(BuildProducts[0], "Unity-IPhone.xcodeproj");

				IgorXCodeProjUtils.SetDevTeamID(this, ProjectPath, DevTeamID);
			}

			return true;
		}

		public virtual bool BuildXCodeProj()
		{
			string BuiltName = GetBuiltNameForTarget(JobBuildTarget);

			return true;
		}
	}
}