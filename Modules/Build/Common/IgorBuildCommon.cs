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
	public class IgorBuildCommon : IgorModuleBase
	{
		public static string BuildFlag = "build";
		public static string PlatformFlag = "platform";

		public static StepID BuildStep = new StepID("Build", 500);
		public static StepID SwitchPlatformStep = new StepID("SwitchPlatform", 0);

		public delegate BuildOptions GetExtraBuildOptions(BuildTarget CurrentTarget);

		public override string GetModuleName()
		{
			return "Build.Common";
		}

		public override void RegisterModule()
		{
			IgorCore.RegisterNewModule(this);
		}

		public override void ProcessArgs()
		{
		}

		public static string[] GetLevels()
		{
			List<string> LevelNames = new List<string>();
			
			foreach(EditorBuildSettingsScene CurrentScene in EditorBuildSettings.scenes)
			{
				if(CurrentScene.enabled)
				{
					LevelNames.Add(CurrentScene.path);
				}
			}
			
			return LevelNames.ToArray();
		}
	}
}