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
	public class IgorDistributionCommon : IgorModuleBase
	{
		public static string RunDistributeFromMenuFlag = "rundistributefrommenu";

		public override string GetModuleName()
		{
			return "Distribution.Common";
		}

		public override void RegisterModule()
		{
			IgorCore.RegisterNewModule(this);
		}

#if UNITY_EDITOR
		public override string DrawJobInspectorAndGetEnabledParams(string CurrentParams)
		{
			string EnabledParams = CurrentParams;

			DrawBoolParam(ref EnabledParams, "Run Distribution Steps For Manual Builds", RunDistributeFromMenuFlag);

			return EnabledParams;
		}
#endif // UNITY_EDITOR

		public override void ProcessArgs(IIgorStepHandler StepHandler)
		{
		}

		public static bool RunDistributionStepsThisJob()
		{
			if(IgorJobConfig.IsBoolParamSet(IgorDistributionCommon.RunDistributeFromMenuFlag))
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