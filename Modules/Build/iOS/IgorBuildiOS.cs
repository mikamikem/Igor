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
		public static StepID CustomFixupXCodeProjStep = new StepID("3rdPartyFixupXCodeProj", 650);
		public static StepID BuildXCodeProjStep = new StepID("BuildXCodeProj", 700);

		public static string iOSDevTeamIDFlag = "iOSDevTeamID";
		public static string iOSBuiltNameFlag = "BuiltiOSName";
		public static string iOSProvisionProfileFlag = "iOSProvisionProfile";
		public static string iOSMobileProvisionFlag = "iOSMobileProvisionFile";

		public static string EnableGameKitFlag = "EnableGamekit";

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

					DevTeamID = GetParamOrConfigString(iOSDevTeamIDFlag, "Your Dev Team ID hasn't been set!  Your build may not sign correctly.");
					SigningIdentity = GetParamOrConfigString(iOSProvisionProfileFlag, "Your Signing Identity hasn't been set!  Your build may not sign correctly.");
					ProvisionPath = GetParamOrConfigString(iOSMobileProvisionFlag, "Your Mobile Provision path hasn't been set!  Your build may not sign correctly.");

					bEnableGamekit = IgorJobConfig.IsBoolParamSet(EnableGameKitFlag);
				}
			}
		}

		public override string DrawJobInspectorAndGetEnabledParams(string CurrentParams)
		{
			string EnabledParams = CurrentParams;

			DrawStringConfigParamDifferentOverride(ref EnabledParams, "Built name", IgorBuildCommon.BuiltNameFlag, iOSBuiltNameFlag);

			DrawStringConfigParam(ref EnabledParams, "iOS Dev Team ID", iOSDevTeamIDFlag);

			DrawStringConfigParam(ref EnabledParams, "iOS Signing Provision Profile Name", iOSProvisionProfileFlag);

			DrawStringConfigParam(ref EnabledParams, "iOS Mobile Provision Path", iOSMobileProvisionFlag);

			DrawBoolParam(ref EnabledParams, "Enable Gamekit", EnableGameKitFlag);

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
		public bool bEnableGamekit = false;
		public string SigningIdentity = "";
		public string ProvisionPath = "";

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

#if DUMMY
			string XCodeProjDirectory = "iOS";

			List<string> BuiltFiles = new List<string>();

			BuiltFiles.Add(XCodeProjDirectory);

			IgorBuildCommon.SetNewBuildProducts(BuiltFiles);
#endif

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

				IgorXCodeProjUtils.AddOrUpdateForAllBuildProducts(this, ProjectPath, "CODE_SIGN_IDENTITY", "iPhone Developer");

				IgorXCodeProjUtils.AddOrUpdateForAllBuildProducts(this, ProjectPath, "CODE_SIGN_IDENTITY[sdk=iphoneos*]", "iPhone Developer");

				IgorXCodeProjUtils.AddOrUpdateForAllBuildProducts(this, ProjectPath, "CODE_SIGN_RESOURCE_RULES_PATH", "$(SDKROOT)/ResourceRules.plist");

				string PlistPath = Path.Combine(BuildProducts[0], "Info.plist");

				IgorPlistUtils.SetBoolValue(this, PlistPath, "UIViewControllerBasedStatusBarAppearance", false);

				if(bEnableGamekit)
				{
					IgorPlistUtils.AddRequiredDeviceCapability(this, PlistPath, "gamekit");
				}
			}

			return true;
		}

		public virtual bool BuildXCodeProj()
		{
			List<string> BuildProducts = IgorBuildCommon.GetBuildProducts();

			if(BuildProducts.Count > 0)
			{
				string BuiltName = GetBuiltNameForTarget(JobBuildTarget);
				
				string BuildOutput = "";
				string BuildError = "";

				string FullBuildProductPath = Path.Combine(Path.GetFullPath("."), BuildProducts[0]);

				int BuildExitCode = IgorUtils.RunProcessCrossPlatform("/Applications/Xcode.app/Contents/Developer/usr/bin/xcodebuild", "",
					"-project Unity-iPhone.xcodeproj clean build", FullBuildProductPath, ref BuildOutput, ref BuildError);

				if(BuildExitCode != 0)
				{
					LogError("XCode build failed.\nOutput:\n" + BuildOutput + "\n\n\nError:\n" + BuildError);

					return true;
				}

				Log("XCode build succeeded!\nOutput:\n" + BuildOutput + "\n\n\nError:\n" + BuildError);

				BuildOutput = "";
				BuildError = "";

				string LastBundleIdentifierPart = PlayerSettings.bundleIdentifier.Substring(PlayerSettings.bundleIdentifier.LastIndexOf('.') + 1);

				BuildExitCode = IgorUtils.RunProcessCrossPlatform("/usr/bin/xcrun", "",
					"-sdk iphoneos PackageApplication -v \"build/" + LastBundleIdentifierPart + ".app\" -o \"" + Path.Combine(FullBuildProductPath, BuiltName + ".ipa") +
					"\" --sign \"" + SigningIdentity + "\" --embed \"../" + ProvisionPath + "\"",
					FullBuildProductPath, ref BuildOutput, ref BuildError);

				if(BuildExitCode != 0)
				{
					LogError("Packaging the application failed.\nOutput:\n" + BuildOutput + "\n\n\nError:\n" + BuildError);

					return true;
				}

				Log("Packaging the application succeeded!\nOutput:\n" + BuildOutput + "\n\n\nError:\n" + BuildError);

			}

			return true;
		}
	}
}