using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.IO;
using System;
using System.Reflection;
using System.Xml.Serialization;
using System.Linq;

namespace Igor
{
	public class IgorBuildDesktop : IgorModuleBase
	{
		public override string GetModuleName()
		{
			return "Build.Desktop";
		}

		public override void RegisterModule()
		{
			bool DidRegister = IgorCore.RegisterNewModule(this);

		    IgorBuildCommon.RegisterBuildPlatforms(new string[] {"OSX32", "OSX64", "OSXUniversal", "Windows32", "Windows64"});
		}

		public override void ProcessArgs(IIgorStepHandler StepHandler)
		{
			if(IgorJobConfig.IsBoolParamSet(IgorBuildCommon.BuildFlag))
			{
				IgorCore.SetModuleActiveForJob(this);

				string Platform = IgorJobConfig.GetStringParam(IgorBuildCommon.PlatformFlag);

				bool bWindows = false;
				bool bOSX = false;

				if(Platform.Contains("OSX32"))
				{
					JobBuildTarget = BuildTarget.StandaloneOSXIntel;
					bOSX = true;
				}
				else if(Platform.Contains("OSX64"))
				{
					JobBuildTarget = BuildTarget.StandaloneOSXIntel64;
					bOSX = true;
				}
				else if(Platform.Contains("OSXUniversal"))
				{
					JobBuildTarget = BuildTarget.StandaloneOSXUniversal;
					bOSX = true;
				}
				else if(Platform.Contains("Windows32"))
				{
					JobBuildTarget = BuildTarget.StandaloneWindows;
					bWindows = true;
				}
				else if(Platform.Contains("Windows64"))
				{
					JobBuildTarget = BuildTarget.StandaloneWindows64;
					bWindows = true;
				}

				if(bOSX)
				{
					StepHandler.RegisterJobStep(IgorBuildCommon.SwitchPlatformStep, this, SwitchPlatforms);
					StepHandler.RegisterJobStep(IgorBuildCommon.BuildStep, this, BuildOSX);
				}
				else if(bWindows)
				{
					StepHandler.RegisterJobStep(IgorBuildCommon.SwitchPlatformStep, this, SwitchPlatforms);
					StepHandler.RegisterJobStep(IgorBuildCommon.BuildStep, this, BuildWindows);
				}
			}
		}

		public virtual string GetBuiltNameConfigKeyForPlatform(string PlatformName)
		{
			return "Built" + PlatformName + "Name";
		}

		public override bool ShouldDrawInspectorForParams(string CurrentParams)
		{
			bool bBuilding = IgorUtils.IsBoolParamSet(CurrentParams, IgorBuildCommon.BuildFlag);
			bool bRecognizedPlatform = false;

			if(bBuilding)
			{
				string Platform = IgorUtils.GetStringParam(CurrentParams, IgorBuildCommon.PlatformFlag);

				if(Platform == "OSX32")
				{
					bRecognizedPlatform = true;
				}
				else if(Platform == "OSX64")
				{
					bRecognizedPlatform = true;
				}
				else if(Platform == "OSXUniversal")
				{
					bRecognizedPlatform = true;
				}
				else if(Platform == "Windows32")
				{
					bRecognizedPlatform = true;
				}
				else if(Platform == "Windows64")
				{
					bRecognizedPlatform = true;
				}
			}

			return bBuilding && bRecognizedPlatform;
		}

        public override string DrawJobInspectorAndGetEnabledParams(string CurrentParams)
		{
			string EnabledParams = CurrentParams;

			string Platform = IgorUtils.GetStringParam(CurrentParams, IgorBuildCommon.PlatformFlag);

			DrawStringConfigParamDifferentOverride(ref EnabledParams, "Built name", IgorBuildCommon.BuiltNameFlag, GetBuiltNameConfigKeyForPlatform(Platform));

			return EnabledParams;
		}

		public BuildTarget JobBuildTarget = BuildTarget.StandaloneOSXIntel;
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

			bool bOSX = false;
			bool bWindows = false;

			if(NewTarget == BuildTarget.StandaloneOSXIntel)
			{
				BuiltName = GetConfigString("BuiltOSX32Name");
				bOSX = true;
			}
			else if(NewTarget == BuildTarget.StandaloneOSXIntel64)
			{
				BuiltName = GetConfigString("BuiltOSX64Name");
				bOSX = true;
			}
			else if(NewTarget == BuildTarget.StandaloneOSXUniversal)
			{
				BuiltName = GetConfigString("BuiltOSXUniversalName");
				bOSX = true;
			}

			if(NewTarget == BuildTarget.StandaloneWindows)
			{
				BuiltName = GetConfigString("BuiltWindows32Name");
				bWindows = true;
			}
			else if(NewTarget == BuildTarget.StandaloneWindows64)
			{
				BuiltName = GetConfigString("BuiltWindows64Name");
				bWindows = true;
			}

			if(BuiltName == "")
			{
				if(bOSX)
				{
					BuiltName = GetConfigString("BuiltOSXName");
				}
				else if(bWindows)
				{
					BuiltName = GetConfigString("BuiltWindowsName");
				}
			}

			if(BuiltName == "")
			{
				BuiltName = Path.GetFileName(EditorUserBuildSettings.GetBuildLocation(NewTarget));
			}

			if(BuiltName == "")
			{
				if(bOSX)
				{
					BuiltName = "Unity.app";
				}
				else if(bWindows)
				{
					BuiltName = "Unity.exe";
				}
			}

            if(!BuiltName.Contains(".exe") && !BuiltName.Contains(".app"))
            {
                if(bOSX)
                {
                    BuiltName += ".app";
                }
                if(bWindows)
                {
                    BuiltName += ".exe";
                }
            }

            if(!string.IsNullOrEmpty(BuiltName) && !string.IsNullOrEmpty(IgorBuildCommon.CommitInfo))
            {
                BuiltName = BuiltName.Insert(BuiltName.IndexOf("."), "_" + IgorBuildCommon.CommitInfo.Trim(new char[] {'"'}));
            }

			return BuiltName;
		}

		public virtual bool IsPlatformWindows(BuildTarget CurrentTarget)
		{
			if(CurrentTarget == BuildTarget.StandaloneWindows || CurrentTarget == BuildTarget.StandaloneWindows64)
			{
				return true;
			}

			return false;
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

		public virtual bool BuildOSX()
		{
			Log("Building OSX build (Target:" + JobBuildTarget + ")");

			return Build();
		}

		public virtual bool BuildWindows()
		{
			Log("Building Windows build (Target:" + JobBuildTarget + ")");

			return Build();
		}

		public virtual bool Build()
		{
			string BuiltName = GetBuiltNameForTarget(JobBuildTarget);
			string DataFolderName = BuiltName.Substring(0, BuiltName.LastIndexOf('.')) + "_Data";

			if(File.Exists(BuiltName))
			{
				IgorUtils.DeleteFile(BuiltName);
			}

			if(IsPlatformWindows(JobBuildTarget))
			{
				if(Directory.Exists(DataFolderName))
				{
					IgorUtils.DeleteDirectory(DataFolderName);
				}
			}

#if !UNITY_4_3
            BuiltName = System.IO.Path.Combine(System.IO.Path.GetFullPath("."), BuiltName);	
#endif
            BuildPipeline.BuildPlayer(IgorBuildCommon.GetLevels(), BuiltName, JobBuildTarget, IgorBuildCommon.GetBuildOptions());

            Log("Destination file is: " + BuiltName);

			List<string> BuiltFiles = new List<string>();

			if(IgorAssert.EnsureTrue(this, File.Exists(BuiltName), "The built file " + BuiltName + " doesn't exist.  Something went wrong during the build step.  Please check the logs!"))
			{
				BuiltFiles.Add(BuiltName);
			}

			if(IsPlatformWindows(JobBuildTarget))
			{
				if(IgorAssert.EnsureTrue(this, Directory.Exists(DataFolderName), "The built data directory for the Windows build " + DataFolderName + " doesn't exist.  Something went wrong during the build step.  Please check the logs!"))
				{
					BuiltFiles.Add(DataFolderName);
				}
			}

			IgorBuildCommon.SetNewBuildProducts(BuiltFiles);

			return true;
		}
	}
}