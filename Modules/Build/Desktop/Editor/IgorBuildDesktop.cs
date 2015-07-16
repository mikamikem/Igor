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

		    IgorBuildCommon.RegisterBuildPlatforms(new string[] {"OSX32", "OSX64", "OSXUniversal", "Windows32", "Windows64", "Linux32", "Linux64", "LinuxUniversal"});
		}

		public static BuildTarget GetBuildTargetForCurrentJob(out bool bWindows, out bool bOSX, out bool bLinux, string AllParams = "")
		{
			string PlatformString = IgorJobConfig.GetStringParam(IgorBuildCommon.PlatformFlag);

			if(PlatformString == "")
			{
				PlatformString = IgorRuntimeUtils.GetStringParam(AllParams, IgorBuildCommon.PlatformFlag);
			}

			bWindows = false;
			bOSX = false;
			bLinux = false;

			BuildTarget CurrentJobBuildTarget = BuildTarget.StandaloneOSXIntel;

			if(PlatformString.Contains("OSX32"))
			{
				CurrentJobBuildTarget = BuildTarget.StandaloneOSXIntel;
				bOSX = true;
			}
			else if(PlatformString.Contains("OSX64"))
			{
				CurrentJobBuildTarget = BuildTarget.StandaloneOSXIntel64;
				bOSX = true;
			}
			else if(PlatformString.Contains("OSXUniversal"))
			{
				CurrentJobBuildTarget = BuildTarget.StandaloneOSXUniversal;
				bOSX = true;
			}
			else if(PlatformString.Contains("Windows32"))
			{
				CurrentJobBuildTarget = BuildTarget.StandaloneWindows;
				bWindows = true;
			}
			else if(PlatformString.Contains("Windows64"))
			{
				CurrentJobBuildTarget = BuildTarget.StandaloneWindows64;
				bWindows = true;
			}
			else if(PlatformString.Contains("Linux32"))
			{
				CurrentJobBuildTarget = BuildTarget.StandaloneLinux;
				bLinux = true;
			}
			else if(PlatformString.Contains("Linux64"))
			{
				CurrentJobBuildTarget = BuildTarget.StandaloneLinux64;
				bLinux = true;
			}
			else if(PlatformString.Contains("LinuxUniversal"))
			{
				CurrentJobBuildTarget = BuildTarget.StandaloneLinuxUniversal;
				bLinux = true;
			}

			return CurrentJobBuildTarget;
		}

		public override void ProcessArgs(IIgorStepHandler StepHandler)
		{
			if(IgorJobConfig.IsBoolParamSet(IgorBuildCommon.BuildFlag))
			{
				IgorCore.SetModuleActiveForJob(this);

				bool bWindows = false;
				bool bOSX = false;
				bool bLinux = false;

				JobBuildTarget = GetBuildTargetForCurrentJob(out bWindows, out bOSX, out bLinux);

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
				else if(bLinux)
				{
					StepHandler.RegisterJobStep(IgorBuildCommon.SwitchPlatformStep, this, SwitchPlatforms);
					StepHandler.RegisterJobStep(IgorBuildCommon.BuildStep, this, BuildLinux);
				}
			}
		}

		public virtual string GetBuiltNameConfigKeyForPlatform(string PlatformName)
		{
			return "Built" + PlatformName + "Name";
		}

		public override bool ShouldDrawInspectorForParams(string CurrentParams)
		{
			bool bBuilding = IgorRuntimeUtils.IsBoolParamSet(CurrentParams, IgorBuildCommon.BuildFlag);
			bool bRecognizedPlatform = false;

			if(bBuilding)
			{
				string Platform = IgorRuntimeUtils.GetStringParam(CurrentParams, IgorBuildCommon.PlatformFlag);

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
				else if(Platform == "Linux32")
				{
					bRecognizedPlatform = true;
				}
				else if(Platform == "Linux64")
				{
					bRecognizedPlatform = true;
				}
				else if(Platform == "LinuxUniversal")
				{
					bRecognizedPlatform = true;
				}
			}

			return bBuilding && bRecognizedPlatform;
		}

        public override string DrawJobInspectorAndGetEnabledParams(string CurrentParams)
		{
			string EnabledParams = CurrentParams;

			string Platform = IgorRuntimeUtils.GetStringParam(CurrentParams, IgorBuildCommon.PlatformFlag);

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
			bool bLinux = false;

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

			if(NewTarget == BuildTarget.StandaloneLinux)
			{
				BuiltName = GetConfigString("BuiltLinux32Name");
				bLinux = true;
			}
			else if(NewTarget == BuildTarget.StandaloneLinux64)
			{
				BuiltName = GetConfigString("BuiltLinux64Name");
				bLinux = true;
			}
			else if(NewTarget == BuildTarget.StandaloneLinuxUniversal)
			{
				BuiltName = GetConfigString("BuiltLinuxUniversalName");
				bLinux = true;
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
				else if(bLinux)
				{
					BuiltName = GetConfigString("BuiltLinuxName");
				}
			}

            if(BuiltName == "")
            {
                BuiltName = IgorJobConfig.GetStringParam(IgorBuildCommon.BuiltNameFlag);
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
				else if(bLinux)
				{
					BuiltName = "Unity";
				}
			}

            if(!bLinux && !BuiltName.Contains(".exe") && !BuiltName.Contains(".app"))
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

            if(!string.IsNullOrEmpty(BuiltName) && IgorJobConfig.IsBoolParamSet(IgorBuildCommon.AppendCommitInfoFlag))
            {
                string CommitInfo = IgorBuildCommon.GetCommitInfo();
                if(!string.IsNullOrEmpty(CommitInfo))
                {
                    BuiltName = BuiltName.Insert(BuiltName.IndexOf("."), "_" + CommitInfo);
                }
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

		public virtual bool BuildLinux()
		{
			Log("Building Linux build (Target:" + JobBuildTarget + ")");

			return Build();
		}

		public virtual bool Build()
		{
			string BuiltName = GetBuiltNameForTarget(JobBuildTarget);
			string BuiltBaseName = BuiltName;

			if(BuiltBaseName.Contains("."))
			{
				BuiltBaseName = BuiltName.Substring(0, BuiltBaseName.LastIndexOf('.'));
			}

			string DataFolderName = BuiltBaseName + "_Data";

			if(File.Exists(BuiltName))
			{
				IgorRuntimeUtils.DeleteFile(BuiltName);
			}

			if(IsPlatformWindows(JobBuildTarget))
			{
				if(Directory.Exists(DataFolderName))
				{
					IgorRuntimeUtils.DeleteDirectory(DataFolderName);
				}
			}

#if !UNITY_4_3
            BuiltName = System.IO.Path.Combine(System.IO.Path.GetFullPath("."), BuiltName);	
#endif
            BuildPipeline.BuildPlayer(IgorUtils.GetLevels(), BuiltName, JobBuildTarget, IgorBuildCommon.GetBuildOptions());

            Log("Destination file is: " + BuiltName);

			List<string> BuiltFiles = new List<string>();

			if(IsPlatformWindows(JobBuildTarget))
			{
				if(IgorAssert.EnsureTrue(this, File.Exists(BuiltName), "The built file " + BuiltName + " doesn't exist.  Something went wrong during the build step.  Please check the logs!"))
				{
					BuiltFiles.Add(BuiltName);
				}

				if(IgorAssert.EnsureTrue(this, Directory.Exists(DataFolderName), "The built data directory for the Windows build " + DataFolderName + " doesn't exist.  Something went wrong during the build step.  Please check the logs!"))
				{
					BuiltFiles.Add(DataFolderName);
				}
			}
			else
			{
				if(IgorAssert.EnsureTrue(this, Directory.Exists(BuiltName), "The built app directory for the Mac build " + BuiltName + " doesn't exist.  Something went wrong during the build step.  Please check the logs!"))
				{
					BuiltFiles.Add(BuiltName);
				}
			}

			IgorCore.SetNewModuleProducts(BuiltFiles);

			return true;
		}
	}
}