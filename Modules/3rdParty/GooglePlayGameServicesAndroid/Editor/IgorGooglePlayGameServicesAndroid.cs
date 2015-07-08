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
	public class IgorGooglePlayGameServicesAndroid : IgorModuleBase
	{
		public static string EnableGooglePlayGameServicesAndroidFlag = "GooglePlayGameServicesAndroid";

		public override string GetModuleName()
		{
			return "3rdParty.GooglePlayGameServicesAndroid";
		}

		public override void RegisterModule()
		{
			IgorCore.RegisterNewModule(this);
		}

		public override void ProcessArgs(IIgorStepHandler StepHandler)
		{
			if(IgorJobConfig.IsBoolParamSet(EnableGooglePlayGameServicesAndroidFlag))
			{
				IgorCore.SetModuleActiveForJob(this);

				StepHandler.RegisterJobStep(IgorBuildAndroid.FixupAndroidProjStep, this, UpdateAndroidProj);
			}
		}

		public override string DrawJobInspectorAndGetEnabledParams(string CurrentParams)
		{
			string EnabledParams = CurrentParams;

			DrawBoolParam(ref EnabledParams, "Use Google Play Game Services for Android", EnableGooglePlayGameServicesAndroidFlag);

			return EnabledParams;
		}

		public virtual bool UpdateAndroidProj()
		{
			List<string> BuildProducts = IgorCore.GetModuleProducts();

			if(IgorAssert.EnsureTrue(this, BuildProducts.Count > 0, "Attempting to update the Android project, but one was not generated in the build phase!"))
			{
				if(IgorBuildAndroid.RunAndroidCommandLineUtility(this, Path.Combine(BuildProducts[0], "google_play_services_lib"), "update project --path ."))
				{
					IgorBuildAndroid.AddNewLibrary("google_play_services_lib");
				}
			}

			return true;
		}
	}
}
