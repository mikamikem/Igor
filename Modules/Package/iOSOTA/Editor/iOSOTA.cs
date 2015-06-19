using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.IO;
using System;
using System.Reflection;
using System.Xml.Serialization;
using Claunia.PropertyList;

namespace Igor
{
	public class iOSOTA : IgorModuleBase
	{
		public static string OTAEnabledFlag = "iosotaenabled";
		public static string OTAPlistNameFlag = "iosotaplistname";
		public static string OTAHTTPRootFlag = "iosotahttproot";

		public override string GetModuleName()
		{
			return "Package.iOSOTA";
		}

		public override void RegisterModule()
		{
			IgorCore.RegisterNewModule(this);
		}

		public override void ProcessArgs(IIgorStepHandler StepHandler)
		{
			if(IgorJobConfig.IsBoolParamSet(OTAEnabledFlag) && GetParamOrConfigString(OTAPlistNameFlag) != "" &&
				GetParamOrConfigString(OTAHTTPRootFlag) != "")
			{
				IgorCore.SetModuleActiveForJob(this);
				StepHandler.RegisterJobStep(IgorPackageCommon.PackageStep, this, CreateWebDeployFiles);
			}
		}

		public override string DrawJobInspectorAndGetEnabledParams(string CurrentParams)
		{
			string EnabledParams = CurrentParams;

			if(DrawBoolParam(ref EnabledParams, "iOS OTA", OTAEnabledFlag))
			{
				DrawStringConfigParam(ref EnabledParams, "iOS OTA Plist Name", OTAPlistNameFlag);
				DrawStringConfigParam(ref EnabledParams, "iOS OTA HTTP Root", OTAHTTPRootFlag);
			}

			return EnabledParams;
		}

		public virtual bool CreateWebDeployFiles()
		{
			List<string> BuiltProducts = IgorBuildCommon.GetBuildProducts();

			string FileToCopy = "";
			string RootProjectDirectory = "";
			string WebDeployTempDir = Path.Combine(Path.GetFullPath("."), "iOSOTATemp");

			if(IgorAssert.EnsureTrue(this, BuiltProducts.Count > 1, "iOS OTA expected at least two built products, the IPA and the iOS XCode project directory."))
			{
				FileToCopy = BuiltProducts[0];
				RootProjectDirectory = BuiltProducts[1];
			}

			if(IgorAssert.EnsureTrue(this, File.Exists(FileToCopy), "iOS OTA expected the IPA to be at " + FileToCopy + ", but it was not there.") &&
			   IgorAssert.EnsureTrue(this, Directory.Exists(RootProjectDirectory), "iOS OTA expected the XCode Project folder to be at " + RootProjectDirectory + ", but it was not there."))
			{
				if(Directory.Exists(WebDeployTempDir))
				{
					IgorUtils.DeleteDirectory(WebDeployTempDir);
				}

				Directory.CreateDirectory(WebDeployTempDir);

				string PlistPath = Path.Combine(RootProjectDirectory, "Info.plist");

				string BundleIdentifier = PlayerSettings.bundleIdentifier;
				string BundleVersion = IgorPlistUtils.GetStringValue(this, PlistPath, "CFBundleVersion");
				string DisplayName = IgorPlistUtils.GetStringValue(this, PlistPath, "CFBundleDisplayName");

				string OTAManifestPath = GetParamOrConfigString(OTAPlistNameFlag, "", "Application", false);

				if(!OTAManifestPath.EndsWith(".plist"))
				{
					OTAManifestPath += ".plist";
				}

				OTAManifestPath = Path.Combine(WebDeployTempDir, OTAManifestPath);

				string FullIconName = Path.Combine(RootProjectDirectory, Path.Combine("Unity-iPhone", Path.Combine("Images.xcassets", Path.Combine("AppIcon.appiconset", "Icon.png"))));

				string IPAName = Path.GetFileName(FileToCopy);
				string IconName = Path.GetFileName(FullIconName);

				GenerateAndSavePlist(OTAManifestPath, IPAName, IconName, BundleIdentifier, BundleVersion, DisplayName);

				string IPADeployName = Path.Combine(WebDeployTempDir, IPAName);
				string IconDeployName = Path.Combine(WebDeployTempDir, IconName);

				File.Copy(FileToCopy, IPADeployName);
				File.Copy(FullIconName, IconDeployName);

				List<string> NewBuiltProducts = new List<string>();

				NewBuiltProducts.Add(OTAManifestPath);
				NewBuiltProducts.Add(IPADeployName);
				NewBuiltProducts.Add(IconDeployName);

				IgorBuildCommon.SetNewBuildProducts(NewBuiltProducts);

				Log("iOS OTA files successfully generated.");
			}

			return true;
		}

		public virtual void GenerateAndSavePlist(string PlistFileName, string IPAName, string IconName, string BundleIdentifier, string BundleVersion, string BundleDisplayName)
		{
			string HTTPRoot = GetParamOrConfigString(OTAHTTPRootFlag, "iOS OTA HTTP root path is not set.  Can't generate the OTA manifest.");

			if(!HTTPRoot.EndsWith("/"))
			{
				HTTPRoot += "/";
			}

			if(HTTPRoot.StartsWith("http:"))
			{
				HTTPRoot = HTTPRoot.Replace("http:", "https:");
			}

			if(!HTTPRoot.StartsWith("https://"))
			{
				HTTPRoot = "https://" + HTTPRoot;
			}

			NSDictionary SoftwarePackageDict = new NSDictionary();

			SoftwarePackageDict.Add("kind", new NSString("software-package"));
			SoftwarePackageDict.Add("url", new NSString(HTTPRoot + IPAName));

			NSDictionary DisplayImageDict = new NSDictionary();

			DisplayImageDict.Add("kind", new NSString("display-image"));
			DisplayImageDict.Add("needs-shine", new NSNumber(false));
			DisplayImageDict.Add("url", new NSString(HTTPRoot + IconName));

			NSArray AssetsArray = new NSArray(new NSObject[] { SoftwarePackageDict, DisplayImageDict });

			NSDictionary MetadataDict = new NSDictionary();

			MetadataDict.Add("bundle-identifier", new NSString(BundleIdentifier));
			MetadataDict.Add("bundle-version", new NSString(BundleVersion));
			MetadataDict.Add("kind", new NSString("software"));
			MetadataDict.Add("title", new NSString(BundleDisplayName));

			NSDictionary Item0Dict = new NSDictionary();

			Item0Dict.Add("assets", AssetsArray);
			Item0Dict.Add("metadata", MetadataDict);

			NSArray ItemsArray = new NSArray(new NSObject[] { Item0Dict });

			NSDictionary RootDictionary = new NSDictionary();

			RootDictionary.Add("items", ItemsArray);

			FileInfo NewPlist = new FileInfo(PlistFileName);

			PropertyListParser.SaveAsXml(RootDictionary, NewPlist);
		}
	}
}