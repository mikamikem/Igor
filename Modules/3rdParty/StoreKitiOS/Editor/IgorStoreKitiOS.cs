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
using UnityEditor.XCodeEditor;

namespace Igor
{
	public class IgorStoreKitiOS : IgorModuleBase
	{
		public static string EnableStoreKitiOSFlag = "StoreKitiOS";

		public override string GetModuleName()
		{
			return "3rdParty.StoreKitiOS";
		}

		public override void RegisterModule()
		{
			IgorCore.RegisterNewModule(this);
		}

		public override void ProcessArgs(IIgorStepHandler StepHandler)
		{
			if(IgorJobConfig.IsBoolParamSet(EnableStoreKitiOSFlag))
			{
				IgorCore.SetModuleActiveForJob(this);

				StepHandler.RegisterJobStep(IgorBuildiOS.CustomFixupXCodeProjStep, this, UpdateXCodeProj);
			}
		}

		public override string DrawJobInspectorAndGetEnabledParams(string CurrentParams)
		{
			string EnabledParams = CurrentParams;

			DrawBoolParam(ref EnabledParams, "Use StoreKit iOS", EnableStoreKitiOSFlag);

			return EnabledParams;
		}

		public virtual bool UpdateXCodeProj()
		{
			List<string> BuildProducts = IgorCore.GetModuleProducts();

			if(IgorAssert.EnsureTrue(this, BuildProducts.Count > 0, "Attempting to update the XCode project, but one was not generated in the build phase!"))
			{
				string ProjectPath = Path.Combine(BuildProducts[0], "Unity-IPhone.xcodeproj");

				IgorXCodeProjUtils.AddFramework(this, ProjectPath, "StoreKit.framework", TreeEnum.SDKROOT, "System/Library/Frameworks/StoreKit.framework", -1, "wrapper.framework", "StoreKit.framework");
			}

			return true;
		}
	}
}
