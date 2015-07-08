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
		public static string FacebookAndroidAppIDFlag = "FacebookAndroidAppID";

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

			DrawStringConfigParam(ref EnabledParams, "Facebook App ID for Android", FacebookAndroidAppIDFlag);

			return EnabledParams;
		}

		public virtual bool UpdateAndroidProj()
		{
			List<string> BuildProducts = IgorCore.GetModuleProducts();

			if(IgorAssert.EnsureTrue(this, BuildProducts.Count > 0, "Attempting to update the Android project, but one was not generated in the build phase!"))
			{
				string RootJavaPluginSource = Path.Combine("Assets", Path.Combine("Plugins", Path.Combine("Android", Path.Combine("src", "com"))));
				string RootJavaDest = Path.Combine(BuildProducts[0], Path.Combine(PlayerSettings.productName, Path.Combine("src", "com")));

				CopyJavaFilesAndReplacePackageName(RootJavaPluginSource, RootJavaDest);

				string ResourcePath = Path.Combine(BuildProducts[0], Path.Combine(PlayerSettings.productName, "res"));
				string AppID = GetParamOrConfigString(FacebookAndroidAppIDFlag, "Android Facebook ID isn't set, but we've enabled Facebook!  This will probably cause the app to crash on startup!");
				
				IgorBuildAndroid.SwapStringValueInStringsXML(Path.Combine(Path.Combine(ResourcePath, "values"), "strings.xml"), "fbapp_id", AppID, "FACEBOOKAPPIDTOREPLACE");
				IgorBuildAndroid.SwapStringValueInStringsXML(Path.Combine(Path.Combine(ResourcePath, "values-es"), "strings.xml"), "fbapp_id", AppID, "FACEBOOKAPPIDTOREPLACE");
				IgorBuildAndroid.SwapStringValueInStringsXML(Path.Combine(Path.Combine(ResourcePath, "values-he"), "strings.xml"), "fbapp_id", AppID, "FACEBOOKAPPIDTOREPLACE");
				IgorBuildAndroid.SwapStringValueInStringsXML(Path.Combine(Path.Combine(ResourcePath, "values-iw"), "strings.xml"), "fbapp_id", AppID, "FACEBOOKAPPIDTOREPLACE");

				if(IgorBuildAndroid.RunAndroidCommandLineUtility(this, Path.Combine(BuildProducts[0], "facebook"), "update project --path ."))
				{
					IgorBuildAndroid.AddNewLibrary("facebook");
				}
			}

			return true;
		}

		public virtual void CopyJavaFilesAndReplacePackageName(string RootSourceDir, string RootDestDir)
		{
			List<string> JavaFilesToCopy = IgorRuntimeUtils.GetListOfFilesAndDirectoriesInDirectory(RootSourceDir, true, false, true, true, true);

			foreach(string CurrentFile in JavaFilesToCopy)
			{
				if(CurrentFile.EndsWith(".java") || CurrentFile.EndsWith(".aidl"))
				{
					string RelativeFilePath = CurrentFile.Substring(RootSourceDir.Length + 1);
					string NewDestinationPath = Path.Combine(RootDestDir, RelativeFilePath);
					
					if(!Directory.Exists(Path.GetDirectoryName(NewDestinationPath)))
					{
						Directory.CreateDirectory(Path.GetDirectoryName(NewDestinationPath));
					}
					
					if(!File.Exists(NewDestinationPath))
					{
						File.Copy(CurrentFile, NewDestinationPath);

						IgorUtils.ReplaceStringsInFile(this, NewDestinationPath, "com.facebook.android.R", PlayerSettings.bundleIdentifier + ".R");
						IgorUtils.ReplaceStringsInFile(this, NewDestinationPath, "import com.facebook.android.*;", "import com.facebook.android.*;\nimport " + PlayerSettings.bundleIdentifier + ".R;");
						IgorUtils.ReplaceStringsInFile(this, NewDestinationPath, "com.facebook.android.BuildConfig", PlayerSettings.bundleIdentifier + ".BuildConfig");
						IgorUtils.ReplaceStringsInFile(this, NewDestinationPath, "import com.mikamikem.AndroidUnity.R;", "import " + PlayerSettings.bundleIdentifier + ".R;");
					}
				}
			}
		}
	}
}
