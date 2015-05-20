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

				FacebookID = GetParamOrConfigString(FacebookIDFlag, "Your Facebook ID hasn't been set!  Facebook functionality will probably not work correctly.");
				FacebookDisplayName = GetParamOrConfigString(FacebookDisplayNameFlag, "Your Facebook Display Name hasn't been set!  Facebook functionality will probably not work correctly.");
			}
		}

		public override string DrawJobInspectorAndGetEnabledParams(string CurrentParams)
		{
			string EnabledParams = CurrentParams;

			DrawBoolParam(ref EnabledParams, "Use Facebook iOS Hats", EnableFacebookiOSHatsFlag);
			DrawStringConfigParam(ref EnabledParams, "Facebook Dev ID", FacebookIDFlag);
			DrawStringConfigParam(ref EnabledParams, "Facebook Display Name", FacebookDisplayNameFlag);

			return EnabledParams;
		}

		public string FacebookID = "";
		public string FacebookDisplayName = "";

		public virtual bool UpdateXCodeProj()
		{
			List<string> BuildProducts = IgorBuildCommon.GetBuildProducts();

			if(IgorAssert.EnsureTrue(this, BuildProducts.Count > 0, "Attempting to update the XCode project, but one was not generated in the build phase!"))
			{
				string ProjectPath = Path.Combine(BuildProducts[0], "Unity-IPhone.xcodeproj");

				string FacebookIntegrationGUID = IgorXCodeProjUtils.AddNewFileReference(this, ProjectPath, "FacebookIntegration.h", TreeEnum.GROUP);

				IgorXCodeProjUtils.SortGUIDIntoGroup(this, ProjectPath, FacebookIntegrationGUID, "Libraries");

				IgorXCodeProjUtils.AddFramework(this, ProjectPath, "FacebookSDK.framework", TreeEnum.GROUP, "Libraries/FacebookSDK.framework", -1, "wrapper.framework", "FacebookSDK.framework");

				IgorXCodeProjUtils.AddFrameworkSearchPath(this, ProjectPath, "$(SRCROOT)/Libraries");

				string PlistPath = Path.Combine(BuildProducts[0], "Info.plist");

				IgorPlistUtils.SetStringValue(this, PlistPath, "FacebookAppID", FacebookID);

				IgorPlistUtils.SetStringValue(this, PlistPath, "FacebookDisplayName", FacebookDisplayName);

				IgorPlistUtils.AddBundleURLType(this, PlistPath, "fb" + FacebookID);

				IgorZip.UnzipArchiveCrossPlatform(this, Path.Combine(Path.GetFullPath("."), Path.Combine("Assets", Path.Combine("Plugins", Path.Combine("iOS", Path.Combine("FacebookSDK", "FacebookSDK.framework.zip"))))), Path.Combine(BuildProducts[0], "Libraries"));

				IgoriOSSourceUtils.AddHeaderToAppControllerSource(this, BuildProducts[0], "../Libraries/FacebookIntegration.h");

				IgoriOSSourceUtils.AddFunctionToAppControllerSource(this, BuildProducts[0], "/* Pre iOS 4.2 support */\n- (BOOL)application:(UIApplication *)application handleOpenURL:(NSURL *)url\n{\n\treturn [FBSession.activeSession handleOpenURL:url];\n}\n");

				IgorUtils.ReplaceStringsInFile(this, Path.Combine(BuildProducts[0], Path.Combine("Classes", "UnityAppController.mm")), "AppController_SendNotificationWithArg(kUnityOnOpenURL, notifData);\n\treturn YES;", "AppController_SendNotificationWithArg(kUnityOnOpenURL, notifData); return [FBSession.activeSession handleOpenURL:url];");

				IgoriOSSourceUtils.AddSourceToApplicationDidBecomeActive(this, BuildProducts[0], "[FBSession.activeSession handleDidBecomeActive];");
			}

			return true;
		}
	}
}
