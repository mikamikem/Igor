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
using UnityEngine.VR;

namespace Igor
{
	public class IgorBuildVR : IgorModuleBase
	{
		public static string EnableVRFlag = "enablevr";

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

			DrawBoolParam(ref EnabledParams, "Enable VR", EnableVRFlag);

			return EnabledParams;
		}

		public virtual bool SetVRSettings()
		{
			VRSettings.enabled = IgorJobConfig.IsBoolParamSet(EnableVRFlag);
			
			return true;
		}
	}
}