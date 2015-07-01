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
		public static string ZipFilenameFlag = "package_name";

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

			var bZip = DrawBoolParam(ref EnabledParams, "Zip the built files", ZipFlag);

            if(bZip)
            {
			    DrawStringConfigParam(ref EnabledParams, "Zip filename", ZipFilenameFlag);
            }
            else
            {
                EnabledParams = IgorUtils.ClearParam(EnabledParams, ZipFilenameFlag);
            }

			return EnabledParams;
		}

		public virtual bool CreateZip()
		{
			string ZipFilename = GetParamOrConfigString(ZipFilenameFlag, "Zip destination filename is not set.");

			string LogDetails = "Creating zip file with name " + ZipFilename + " from files:";

			List<string> BuiltProducts = IgorBuildCommon.GetBuildProducts();

			foreach(string Product in BuiltProducts)
			{
				LogDetails += "\n" + Product;
			}

			Log(LogDetails);

			ZipFilesCrossPlatform(this, BuiltProducts, ZipFilename);

			return true;
		}

		public static void ZipFilesCrossPlatform(IIgorModule ModuleInst, List<string> FilesToZip, string ZipFilename, bool bUpdateBuildProducts = true, string RootDir = ".")
		{
			if(File.Exists(ZipFilename))
			{
				IgorUtils.DeleteFile(ZipFilename);
			}

#if UNITY_EDITOR_OSX
			ZipFilesMac(ModuleInst, FilesToZip, ZipFilename, bUpdateBuildProducts, RootDir);
#else
			ZipFilesWindows(ModuleInst, FilesToZip, ZipFilename, bUpdateBuildProducts, RootDir);
#endif // UNITY_EDITOR_OSX
		}

		public static void ZipFilesMac(IIgorModule ModuleInst, List<string> FilesToZip, string ZipFilename, bool bUpdateBuildProducts, string RootDir)
		{
			string ZipParams = "-r \"" + ZipFilename + "\" ";

			foreach(string CurrentFile in FilesToZip)
			{
				ZipParams += "\"" + CurrentFile + "\" ";
			}

			string ZipOutput = "";
			string ZipError = "";

			if(IgorUtils.RunProcessCrossPlatform(ModuleInst, "zip", "", ZipParams, Path.GetFullPath(RootDir), "Zipping the files", true) == 0)
			{
				IgorCore.Log(ModuleInst, "Zip file " + ZipFilename + " created successfully!\nOutput:\n" + ZipOutput + "\nError\n" + ZipError);

				if(bUpdateBuildProducts)
				{
					List<string> NewProducts = new List<string>();

					NewProducts.Add(ZipFilename);

					IgorBuildCommon.SetNewBuildProducts(NewProducts);
				}
			}
		}

		public static void ZipFilesWindows(IIgorModule ModuleInst, List<string> FilesToZip, string ZipFilename, bool bUpdateBuildProducts, string RootDir)
		{
			string ZipCommand = "";
			string ZipParams = "";

			string PathX86 = "C:\\Program Files (x86)\\7-Zip\\7z.exe";
			string Path64 = "C:\\Program Files\\7-Zip\\7z.exe";

			if(File.Exists(PathX86))
			{
				ZipCommand = PathX86;
				ZipParams += "a -tzip \"" + ZipFilename + "\" ";
			}
			else
			if(File.Exists(Path64))
			{
				ZipCommand = Path64;
				ZipParams += "a -tzip \"" + ZipFilename + "\" ";
			}
			else
			{
				IgorCore.LogError(ModuleInst, "7Zip is not installed.  Currently 7Zip is the only zip tool supported on Windows.\nPlease download it from here: http://www.7-zip.org/download.html");
				IgorCore.LogError(ModuleInst, "Skipping zip step.");

				return;
			}

			foreach(string CurrentFile in FilesToZip)
			{
				ZipParams += "\"" + CurrentFile + "\" ";
			}

			string ZipOutput = "";
			string ZipError = "";

			if(IgorUtils.RunProcessCrossPlatform(ModuleInst, "", ZipCommand, ZipParams, Path.GetFullPath(RootDir), "Zipping the files") == 0)
			{
				IgorCore.Log(ModuleInst, "Zip file " + ZipFilename + " created successfully!\nOutput:\n" + ZipOutput + "\nError\n" + ZipError);

				if(bUpdateBuildProducts)
				{
					List<string> NewProducts = new List<string>();

					NewProducts.Add(ZipFilename);

					IgorBuildCommon.SetNewBuildProducts(NewProducts);
				}
			}
		}

		public static void UnzipArchiveCrossPlatform(IIgorModule ModuleInst, string ZipFilename, string DirectoryToUnzipTo)
		{
#if UNITY_EDITOR_OSX
			UnzipFileMac(ModuleInst, ZipFilename, DirectoryToUnzipTo);
#else
			UnzipFileWindows(ModuleInst, ZipFilename, DirectoryToUnzipTo);
#endif // UNITY_EDITOR_OSX
		}

		public static void UnzipFileMac(IIgorModule ModuleInst, string ZipFilename, string DirectoryToUnzipTo)
		{
			string ZipParams = "\"" + ZipFilename + "\"";

			if(DirectoryToUnzipTo != "")
			{
				ZipParams += " -d \"" + DirectoryToUnzipTo + "\"";
			}

			string ZipOutput = "";
			string ZipError = "";

			IgorUtils.RunProcessCrossPlatform(ModuleInst, "unzip", "", ZipParams, Path.GetFullPath("."), "Unzipping the archive " + ZipFilename + " to folder " + DirectoryToUnzipTo, true);
		}

		public static void UnzipFileWindows(IIgorModule ModuleInst, string ZipFilename, string DirectoryToUnzipTo)
		{
			IgorAssert.EnsureTrue(ModuleInst, false, "Unzip is not implemented for Windows yet!");
		}
	}
}