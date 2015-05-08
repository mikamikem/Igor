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
	public class IgorFacebookiOSHats : IgorModuleBase
	{
		public static string EnableFacebookiOSHatsFlag = "FacebookiOSHats";
		public static string FacebookIDFlag = "FacebookID";
		public static string FacebookDisplayNameFlag = "FacebookDisplayName";

		public override string GetModuleName()
		{
			return "3rdParty.FacebookiOSHats";
		}

		public override void RegisterModule()
		{
			IgorCore.RegisterNewModule(this);
		}

		public override void ProcessArgs(IIgorStepHandler StepHandler)
		{
			if(IgorJobConfig.IsBoolParamSet(EnableFacebookiOSHatsFlag))
			{
				IgorCore.SetModuleActiveForJob(this);

				StepHandler.RegisterJobStep(IgorBuildiOS.CustomFixupXCodeProjStep, this, UpdateXCodeProj);

				if(IgorJobConfig.IsStringParamSet(FacebookIDFlag))
				{
					FacebookID = IgorJobConfig.GetStringParam(FacebookIDFlag);
				}
				else
				{
					FacebookID = IgorConfig.GetModuleString(this, FacebookIDFlag);
				}

				if(IgorJobConfig.IsStringParamSet(FacebookDisplayNameFlag))
				{
					FacebookDisplayName = IgorJobConfig.GetStringParam(FacebookDisplayNameFlag);
				}
				else
				{
					FacebookDisplayName = IgorConfig.GetModuleString(this, FacebookDisplayNameFlag);
				}
			}
		}

		public override string DrawJobInspectorAndGetEnabledParams(string CurrentParams)
		{
			string EnabledParams = CurrentParams;

			DrawBoolParam(ref EnabledParams, "Use Facebook iOS Hats", EnableFacebookiOSHatsFlag);
			DrawStringConfigParam(ref EnabledParams, "Facebook Dev ID", FacebookIDFlag, FacebookIDFlag);
			DrawStringConfigParam(ref EnabledParams, "Facebook Display Name", FacebookDisplayNameFlag, FacebookDisplayNameFlag);

			return EnabledParams;
		}

		public string FacebookID = "";
		public string FacebookDisplayName = "";

		public virtual bool UpdateXCodeProj()
		{
			List<string> BuildProducts = IgorBuildCommon.GetBuildProducts();

			if(BuildProducts.Count > 0)
			{
				string ProjectPath = Path.Combine(BuildProducts[0], "Unity-IPhone.xcodeproj");

				IgorXCodeProjUtils.AddNewFileReference(this, ProjectPath, "FacebookIntegration.h", TreeEnum.GROUP);

				IgorXCodeProjUtils.AddFramework(this, ProjectPath, "FacebookSDK.framework", TreeEnum.GROUP, "Libraries/FacebookSDK.framework", -1, "wrapper.framework", "FacebookSDK.framework");

				IgorXCodeProjUtils.AddFrameworkSearchPath(this, ProjectPath, "$(SRCROOT)/Libraries");

				string PlistPath = Path.Combine(BuildProducts[0], "Info.plist");

				IgorPlistUtils.SetStringValue(this, PlistPath, "FacebookAppID", FacebookID);

				IgorPlistUtils.SetStringValue(this, PlistPath, "FacebookDisplayName", FacebookDisplayName);

				IgorPlistUtils.AddBundleURLType(this, PlistPath, "fb" + FacebookID);

				IgorZip.UnzipArchiveCrossPlatform(this, Path.Combine(Path.GetFullPath("."), Path.Combine("Assets", Path.Combine("Plugins", Path.Combine("iOS", Path.Combine("FacebookSDK", "FacebookSDK.framework.zip"))))), Path.Combine(BuildProducts[0], "Libraries"));
			}

			return true;
		}
	}
}
