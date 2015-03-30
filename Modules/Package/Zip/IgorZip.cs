using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.IO;
using System;
using System.Reflection;
using System.Xml.Serialization;

namespace Igor
{
	public class IgorZip : IgorModuleBase
	{
		public static string ZipFlag = "zip";
		public static string ZipFilenameFlag = "zipname";

		public override string GetModuleName()
		{
			return "Package.Zip";
		}

		public override void RegisterModule()
		{
			IgorCore.RegisterNewModule(this);
		}

		public override void ProcessArgs(IIgorStepHandler StepHandler)
		{
			if(IgorJobConfig.IsBoolParamSet(ZipFlag) && IgorJobConfig.GetStringParam(ZipFilenameFlag) != "")
			{
				IgorCore.SetModuleActiveForJob(this);
				StepHandler.RegisterJobStep(IgorPackageCommon.PackageStep, this, CreateZip);
			}
		}

		public override string DrawJobInspectorAndGetEnabledParams(string CurrentParams)
		{
			string EnabledParams = CurrentParams;

			DrawBoolParam(ref EnabledParams, "Zip the built files", ZipFlag);
			DrawStringParam(ref EnabledParams, "Zip filename", ZipFilenameFlag);

			return EnabledParams;
		}

		public virtual bool CreateZip()
		{
			string LogDetails = "Creating zip file with name " + IgorJobConfig.GetStringParam(ZipFilenameFlag) + " from files:";

			List<string> BuiltProducts = IgorBuildCommon.GetBuildProducts();

			foreach(string Product in BuiltProducts)
			{
				LogDetails += "\n" + Product;
			}

			Log(LogDetails);

#if UNITY_EDITOR_OSX
			RunMacZip(BuiltProducts, IgorJobConfig.GetStringParam(ZipFilenameFlag));
#else
			RunWindowsZip(BuiltProducts, IgorJobConfig.GetStringParam(ZipFilenameFlag));
#endif // UNITY_EDITOR_OSX

			return true;
		}

		public virtual void RunMacZip(List<string> FilesToZip, string ZipFilename)
		{
			string ZipParams = "-r " + ZipFilename + " ";

			foreach(string CurrentFile in FilesToZip)
			{
				ZipParams += "\"" + CurrentFile + "\" ";
			}

			string ZipOutput = "";
			string ZipError = "";

			if(IgorUtils.RunProcessCrossPlatform("zip", "", ZipParams, Path.GetFullPath("."), ref ZipOutput, ref ZipError, true) != 0)
			{
				LogError("There was a problem zipping the built files.\nOutput:\n" + ZipOutput + "\nError:\n" + ZipError);
			}
			else
			{
				Log("Zip file " + ZipFilename + " created successfully!\nOutput:\n" + ZipOutput + "\nError\n" + ZipError);

				List<string> NewProducts = new List<string>();

				NewProducts.Add(ZipFilename);

				IgorBuildCommon.SetNewBuildProducts(NewProducts);
			}
		}

		public virtual void RunWindowsZip(List<string> FilesToZip, string ZipFilename)
		{
			string ZipCommand = "";
			string ZipParams = "";

			if(File.Exists("C:\\Program Files\\7-Zip\\7z.exe"))
			{
				ZipCommand = "C:\\Program Files\\7-Zip\\7z.exe";
				ZipParams += "a -tzip " + ZipFilename + " ";
			}
			else
			{
				LogError("7Zip is not installed.  Currently 7Zip is the only zip tool supported on Windows.\nPlease download it from here: http://www.7-zip.org/download.html");
				LogError("Skipping zip step.");

				return;
			}

			foreach(string CurrentFile in FilesToZip)
			{
				ZipParams += "\"" + CurrentFile + "\" ";
			}

			string ZipOutput = "";
			string ZipError = "";

			if(IgorUtils.RunProcessCrossPlatform("", ZipCommand, ZipParams, Path.GetFullPath("."), ref ZipOutput, ref ZipError) != 0)
			{
				LogError("There was a problem zipping the built files.\nOutput:\n" + ZipOutput + "\nError:\n" + ZipError);
			}
			else
			{
				Log("Zip file " + ZipFilename + " created successfully!\nOutput:\n" + ZipOutput + "\nError\n" + ZipError);

				List<string> NewProducts = new List<string>();

				NewProducts.Add(ZipFilename);

				IgorBuildCommon.SetNewBuildProducts(NewProducts);
			}
		}
	}
}