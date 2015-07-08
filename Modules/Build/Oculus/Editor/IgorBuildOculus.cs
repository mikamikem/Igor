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
	public class IgorBuildOculus : IgorModuleBase
	{
		public static string BuildOculusFlag = "buildoculus";

		public static StepID BuildOculusStep = new StepID("Build Oculus Files", 501);

		public override string GetModuleName()
		{
			return "Build.Oculus";
		}

		public override void RegisterModule()
		{
			IgorCore.RegisterNewModule(this);
		}

		public override void ProcessArgs(IIgorStepHandler StepHandler)
		{
			if(IgorJobConfig.IsBoolParamSet(IgorBuildCommon.BuildFlag) && IgorJobConfig.IsBoolParamSet(BuildOculusFlag))
			{
				bool bWindows = false;
				bool bOSX = false;
				bool bLinux = false;

				IgorBuildDesktop.GetBuildTargetForCurrentJob(out bWindows, out bOSX, out bLinux);

				if(bWindows)
				{
					StepHandler.RegisterJobStep(BuildOculusStep, this, BuildOculus);
				}
			}
		}

		public override bool ShouldDrawInspectorForParams(string CurrentParams)
		{
			bool bBuilding = IgorRuntimeUtils.IsBoolParamSet(CurrentParams, IgorBuildCommon.BuildFlag);
			bool bRecognizedPlatform = false;

			if(bBuilding)
			{
				bool bWindows = false;
				bool bOSX = false;
				bool bLinux = false;

				IgorBuildDesktop.GetBuildTargetForCurrentJob(out bWindows, out bOSX, out bLinux, CurrentParams);

				bRecognizedPlatform = bWindows;
			}

			return bBuilding && bRecognizedPlatform;
		}

        public override string DrawJobInspectorAndGetEnabledParams(string CurrentParams)
		{
			string EnabledParams = CurrentParams;

			DrawBoolParam(ref EnabledParams, "Build for Oculus", BuildOculusFlag);

			return EnabledParams;
		}

		public virtual bool BuildOculus()
		{
			List<string> BuiltDesktopFiles = new List<string>();

			BuiltDesktopFiles.AddRange(IgorCore.GetModuleProducts());

			string OculusDirectToRiftFilename = "";

			foreach(string CurrentFile in BuiltDesktopFiles)
			{
				if(CurrentFile.EndsWith(".exe"))
				{
					OculusDirectToRiftFilename = CurrentFile.Replace(".exe", "_DirectToRift.exe");
				}
			}

			if(File.Exists(OculusDirectToRiftFilename))
			{
				BuiltDesktopFiles.Add(OculusDirectToRiftFilename);
			}

			IgorCore.SetNewModuleProducts(BuiltDesktopFiles);

			return true;
		}
	}
}