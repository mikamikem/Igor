using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.IO;
using System;
using System.Reflection;
using System.Xml.Serialization;

namespace Igor
{
	public class IgorPackageCommon : IgorModuleBase
	{
		public static string RunPackageFromMenuFlag = "runpackagefrommenu";

		public static StepID PackageStep = new StepID("Package", 750);

		public override string GetModuleName()
		{
			return "Package.Common";
		}

		public override void RegisterModule()
		{
			IgorCore.RegisterNewModule(this);
		}

#if UNITY_EDITOR
		public override string DrawJobInspectorAndGetEnabledParams(string CurrentParams)
		{
			string EnabledParams = CurrentParams;

			DrawBoolParam(ref EnabledParams, "Run Package Steps For Manual Builds", RunPackageFromMenuFlag);

			return EnabledParams;
		}
#endif // UNITY_EDITOR

		public override void ProcessArgs(IIgorStepHandler StepHandler)
		{
		}

		public static bool RunPackageStepsThisJob()
		{
			if(IgorJobConfig.IsBoolParamSet(IgorPackageCommon.RunPackageFromMenuFlag))
			{
				return true;
			}
			else
			{
				if(IgorJobConfig.GetWasMenuTriggered())
				{
					return false;
				}

				return true;
			}
		}
	}
}