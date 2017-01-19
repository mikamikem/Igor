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
	public class IgorBuildVR : IgorModuleBase
	{
		public static string VRSupportedFlag = "vrsupported";

		public static StepID SetVRStep = new StepID("Set VR Settings", 270);

		public override string GetModuleName()
		{
			return "Build.VR";
		}

		public override void RegisterModule()
		{
			IgorCore.RegisterNewModule(this);
		}

		public override void ProcessArgs(IIgorStepHandler StepHandler)
		{
			StepHandler.RegisterJobStep(SetVRStep, this, SetVRSettings);
		}

		public override bool ShouldDrawInspectorForParams(string CurrentParams)
		{
			return true;
		}

        public override string DrawJobInspectorAndGetEnabledParams(string CurrentParams)
		{
			string EnabledParams = CurrentParams;

			DrawBoolParam(ref EnabledParams, "VR Supported", VRSupportedFlag);

			return EnabledParams;
		}

		public virtual bool SetVRSettings()
		{
			PlayerSettings.virtualRealitySupported = IgorJobConfig.IsBoolParamSet(VRSupportedFlag);

			return true;
		}
	}
}