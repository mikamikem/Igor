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
#if UNITY_5
					JobBuildTarget = BuildTarget.iOS;
#else
					JobBuildTarget = BuildTarget.iPhone;
#endif
					bIOS = true;

					StepHandler.RegisterJobStep(IgorBuildCommon.SwitchPlatformStep, this, SwitchPlatforms);
					StepHandler.RegisterJobStep(IgorBuildCommon.BuildStep, this, BuildiOS);
					StepHandler.RegisterJobStep(FixupXCodeProjStep, this, FixupXCodeProj);
					StepHandler.RegisterJobStep(BuildXCodeProjStep, this, BuildXCodeProj);
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
			bool bBuilding = IgorRuntimeUtils.IsBoolParamSet(CurrentParams, IgorBuildCommon.BuildFlag);
			bool bRecognizedPlatform = false;

			if(bBuilding)
			{
				string Platform = IgorRuntimeUtils.GetStringParam(CurrentParams, IgorBuildCommon.PlatformFlag);

				if(Platform == "iOS")
				{
					bRecognizedPlatform = true;
				}
			}

			return bBuilding && bRecognizedPlatform;
		}

		public BuildTarget JobBuildTarget =
#if UNITY_5
											BuildTarget.iOS;
#else
											BuildTarget.iPhone;
#endif

		public List<IgorBuildCommon.GetExtraBuildOptions> BuildOptionsDelegates = new List<IgorBuildCommon.GetExtraBuildOptions>();

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

#if UNITY_5
			if(NewTarget == BuildTarget.iOS)
#else
			if(NewTarget == BuildTarget.iPhone)
#endif
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

			if(Directory.Exists(XCodeProjDirectory))
			{
				IgorRuntimeUtils.DeleteDirectory(XCodeProjDirectory);
			}

			Log("XCode project destination directory is: " + XCodeProjDirectory);

			BuildOptions AllOptions = PlatformSpecificOptions;

			AllOptions |= GetExternalBuildOptions(JobBuildTarget);

#if UNITY_4_3
			BuildPipeline.BuildPlayer(IgorUtils.GetLevels(), XCodeProjDirectory, JobBuildTarget, AllOptions);
#else
			BuildPipeline.BuildPlayer(IgorUtils.GetLevels(), System.IO.Path.Combine(System.IO.Path.GetFullPath("."), XCodeProjDirectory), JobBuildTarget, AllOptions);
#endif

			List<string> BuiltFiles = new List<string>();

			if(IgorAssert.EnsureTrue(this, Directory.Exists(XCodeProjDirectory), "The XCode project directory " + XCodeProjDirectory + " doesn't exist.  Something went wrong during the build step.  Please check the logs!"))
			{
				BuiltFiles.Add(XCodeProjDirectory);
			}

			IgorCore.SetNewModuleProducts(BuiltFiles);

			return true;
		}

		public virtual bool FixupXCodeProj()
		{
			List<string> BuildProducts = IgorCore.GetModuleProducts();

			string DevTeamID = GetParamOrConfigString(iOSDevTeamIDFlag, "Your Dev Team ID hasn't been set!  Your build may not sign correctly.");
			string SigningIdentity = GetParamOrConfigString(iOSProvisionProfileFlag, "Your Signing Identity hasn't been set!  Your build may not sign correctly.");
			bool bEnableGamekit = IgorJobConfig.IsBoolParamSet(EnableGameKitFlag);

			if(IgorAssert.EnsureTrue(this, BuildProducts.Count > 0, "Trying to fix up the XCode project, but one was not generated in the build phase!"))
			{
				string ProjectPath = Path.Combine(BuildProducts[0], "Unity-IPhone.xcodeproj");

				IgorXCodeProjUtils.SetDevTeamID(this, ProjectPath, DevTeamID);

				IgorXCodeProjUtils.AddOrUpdateForAllBuildProducts(this, ProjectPath, "CODE_SIGN_IDENTITY", SigningIdentity);

				IgorXCodeProjUtils.AddOrUpdateForAllBuildProducts(this, ProjectPath, "CODE_SIGN_IDENTITY[sdk=iphoneos*]", SigningIdentity);

				// This is now required to not be in your xcodeproj as of iOS 9.0
//				IgorXCodeProjUtils.AddOrUpdateForAllBuildProducts(this, ProjectPath, "CODE_SIGN_RESOURCE_RULES_PATH", "$(SDKROOT)/ResourceRules.plist");

				string PlistPath = Path.Combine(BuildProducts[0], "Info.plist");

				IgorPlistUtils.SetBoolValue(this, PlistPath, "UIViewControllerBasedStatusBarAppearance", false);
				IgorPlistUtils.SetBoolValue(this, PlistPath, "UIRequiresFullScreen", true);

				if(bEnableGamekit)
				{
					IgorPlistUtils.AddRequiredDeviceCapability(this, PlistPath, "gamekit");
				}
			}

			return true;
		}

		public string GenerateExportPlist(string ProjectPath)
		{
			string DevTeamID = GetParamOrConfigString(iOSDevTeamIDFlag, "Your Dev Team ID hasn't been set!  Your build may not sign correctly.");
			string Plist = "<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n<!DOCTYPE plist PUBLIC \"-//Apple//DTD PLIST 1.0//EN\" \"http://www.apple.com/DTDs/PropertyList-1.0.dtd\">\n<plist version=\"1.0\">\n<dict>\n    <key>iCloudContainerEnvironment</key>\n    <string>Production</string>\n    <key>teamID</key>\n    <string>" +
							 DevTeamID + "</string>\n    <key>method</key>\n    <string>ad-hoc</string>\n</dict>\n</plist>";

			string ProjectRelativePath = "Options.plist";
			string FullFilePath = Path.Combine(ProjectPath, ProjectRelativePath);

			if(File.Exists(FullFilePath))
			{
				IgorRuntimeUtils.DeleteFile(FullFilePath);
			}

			File.WriteAllText(FullFilePath, Plist);

			return ProjectRelativePath;
		}

		public virtual bool BuildXCodeProj()
		{
			List<string> BuildProducts = IgorCore.GetModuleProducts();

			string SigningIdentity = GetParamOrConfigString(iOSProvisionProfileFlag, "Your Signing Identity hasn't been set!  Your build may not sign correctly.");
			string ProvisionPath = GetParamOrConfigString(iOSMobileProvisionFlag, "Your Mobile Provision path hasn't been set!  Your build may not sign correctly.");

			if(IgorAssert.EnsureTrue(this, BuildProducts.Count > 0, "Trying to build the XCode project, but one was not generated in the build phase!"))
			{
				string BuiltName = GetBuiltNameForTarget(JobBuildTarget);
				
				string BuildOutput = "";
				string BuildError = "";

				string FullBuildProductPath = Path.Combine(Path.GetFullPath("."), BuildProducts[0]);

				string LastBundleIdentifierPart = PlayerSettings.bundleIdentifier.Substring(PlayerSettings.bundleIdentifier.LastIndexOf('.') + 1);

				int BuildExitCode = IgorRuntimeUtils.RunProcessCrossPlatform(this, "/Applications/Xcode.app/Contents/Developer/usr/bin/xcodebuild", "",
					"-project Unity-iPhone.xcodeproj clean archive -archivePath " + LastBundleIdentifierPart + ".xcarchive -scheme Unity-iPhone", FullBuildProductPath, "XCode build");

				if(BuildExitCode != 0)
				{
					return true;
				}

				BuildOutput = "";
				BuildError = "";

				string OptionsPlistFile = GenerateExportPlist(FullBuildProductPath);

				BuildExitCode = IgorRuntimeUtils.RunProcessCrossPlatform(this, "/Applications/Xcode.app/Contents/Developer/usr/bin/xcodebuild", "",
					"xcodebuild -exportArchive -archivePath " + LastBundleIdentifierPart + ".xcarchive -exportPath . -exportOptionsPlist " + OptionsPlistFile,
					FullBuildProductPath, "Packaging the application");

				if(BuildExitCode != 0)
				{
					return true;
				}

				List<string> NewBuildProducts = new List<string>();

				string XCodeOutput = Path.Combine(BuildProducts[0], "Unity-iPhone.ipa");
				string BuiltIPAName = Path.Combine(BuildProducts[0], BuiltName + ".ipa");

				if(IgorAssert.EnsureTrue(this, File.Exists(XCodeOutput), "The built IPA " + XCodeOutput + " doesn't exist.  Something went wrong during the build step.  Please check the logs!"))
				{
					if(File.Exists(BuiltIPAName))
					{
						IgorRuntimeUtils.DeleteFile(BuiltIPAName);
					}

					File.Copy(XCodeOutput, BuiltIPAName);
				}

				if(IgorAssert.EnsureTrue(this, File.Exists(BuiltIPAName), "The built IPA " + BuiltIPAName + " doesn't exist.  Something went wrong during the build step.  Please check the logs!"))
				{
					NewBuildProducts.Add(BuiltIPAName);
				}

				if(IgorAssert.EnsureTrue(this, Directory.Exists(BuildProducts[0]), "The XCode project directory " + BuildProducts[0] + " doesn't exist.  Something went wrong during the build step.  Please check the logs!"))
				{
					NewBuildProducts.Add(BuildProducts[0]);
				}

				IgorCore.SetNewModuleProducts(NewBuildProducts);

				Log("Packaging the application succeeded!\nOutput:\n" + BuildOutput + "\n\n\nError:\n" + BuildError);
			}

			return true;
		}
	}
}