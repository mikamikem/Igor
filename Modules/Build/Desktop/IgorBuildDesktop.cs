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
	public class IgorBuildDesktop : IgorModuleBase
	{
		public override string GetModuleName()
		{
			return "Build.Desktop";
		}

		public override void RegisterModule()
		{
			IgorCore.RegisterNewModule(this);

			BuildOptionsDelegates.Clear();
		}

		public override void ProcessArgs()
		{
			if(IgorJobConfig.IsBoolParamSet(IgorBuildCommon.BuildFlag))
			{
				IgorCore.SetModuleActiveForJob(this);

				string Platform = IgorJobConfig.GetStringParam(IgorBuildCommon.PlatformFlag);

				bool bWindows = false;
				bool bOSX = false;

				if(Platform == "OSX32")
				{
					JobBuildTarget = BuildTarget.StandaloneOSXIntel;
					bOSX = true;
				}
				else if(Platform == "OSX64")
				{
					JobBuildTarget = BuildTarget.StandaloneOSXIntel64;
					bOSX = true;
				}
				else if(Platform == "OSXUniversal")
				{
					JobBuildTarget = BuildTarget.StandaloneOSXUniversal;
					bOSX = true;
				}
				else if(Platform == "Windows32")
				{
					JobBuildTarget = BuildTarget.StandaloneWindows;
					bWindows = true;
				}
				else if(Platform == "Windows64")
				{
					JobBuildTarget = BuildTarget.StandaloneWindows64;
					bWindows = true;
				}

				if(bOSX)
				{
					IgorCore.RegisterJobStep(IgorBuildCommon.SwitchPlatformStep, this, SwitchPlatforms);
					IgorCore.RegisterJobStep(IgorBuildCommon.BuildStep, this, BuildOSX);
				}
				else if(bWindows)
				{
					IgorCore.RegisterJobStep(IgorBuildCommon.SwitchPlatformStep, this, SwitchPlatforms);
					IgorCore.RegisterJobStep(IgorBuildCommon.BuildStep, this, BuildWindows);
				}
			}
		}

		public override string DrawJobInspectorAndGetEnabledParams(string CurrentParams)
		{
			string EnabledParams = CurrentParams;

			DrawBoolParam(ref EnabledParams, "Build the game", IgorBuildCommon.BuildFlag);
			DrawStringParam(ref EnabledParams, "Platform to build", IgorBuildCommon.PlatformFlag, new string[] {"OSX32", "OSX64", "OSXUniversal", "Windows32", "Windows64"});

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
				BuiltName = GetConfigString("BuiltWindows4Name");
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

		public virtual bool BuildOSX()
		{
			Log("Building OSX build (Target:" + JobBuildTarget + ")");

			return Build(BuildOptions.None);
		}

		public virtual bool BuildWindows()
		{
			Log("Building Windows build (Target:" + JobBuildTarget + ")");

			return Build(BuildOptions.None);
		}

		public virtual bool Build(BuildOptions PlatformSpecificOptions)
		{
			string BuiltName = GetBuiltNameForTarget(JobBuildTarget);

			Log("Destination file is: " + BuiltName);

			BuildOptions AllOptions = PlatformSpecificOptions;

			AllOptions |= GetExternalBuildOptions(JobBuildTarget);

#if UNITY_4_3
			BuildPipeline.BuildPlayer(IgorBuildCommon.GetLevels(), BuiltName, JobBuildTarget, AllOptions);
#else
			BuildPipeline.BuildPlayer(IgorBuildCommon.GetLevels(), System.IO.Path.Combine(System.IO.Path.GetFullPath("."), BuiltName), JobBuildTarget, AllOptions);
#endif

			return true;
		}
	}
}