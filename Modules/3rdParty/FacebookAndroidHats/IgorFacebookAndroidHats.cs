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
	public class IgorFacebookAndroidHats : IgorModuleBase
	{
		public static string EnableFacebookAndroidHatsFlag = "FacebookAndroidHats";

		public override string GetModuleName()
		{
			return "3rdParty.FacebookAndroidHats";
		}

		public override void RegisterModule()
		{
			IgorCore.RegisterNewModule(this);
		}

		public override void ProcessArgs(IIgorStepHandler StepHandler)
		{
			if(IgorJobConfig.IsBoolParamSet(EnableFacebookAndroidHatsFlag))
			{
				IgorCore.SetModuleActiveForJob(this);

				StepHandler.RegisterJobStep(IgorBuildAndroid.FixupAndroidProjStep, this, UpdateAndroidProj);
			}
		}

		public override string DrawJobInspectorAndGetEnabledParams(string CurrentParams)
		{
			string EnabledParams = CurrentParams;

			DrawBoolParam(ref EnabledParams, "Use Facebook for Android Hats", EnableFacebookAndroidHatsFlag);

			return EnabledParams;
		}

		public virtual bool UpdateAndroidProj()
		{
			List<string> BuildProducts = IgorBuildCommon.GetBuildProducts();

			if(IgorAssert.EnsureTrue(this, BuildProducts.Count > 0, "Attempting to update the Android project, but one was not generated in the build phase!"))
			{
				if(IgorBuildAndroid.RunAndroidCommandLineUtility(this, Path.Combine(BuildProducts[0], "facebook"), "update project --path ."))
				{
					IgorBuildAndroid.AddNewLibrary("facebook");
				}
			}

			return true;
		}
	}
}
