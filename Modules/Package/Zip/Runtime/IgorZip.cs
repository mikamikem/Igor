#if IGOR_RUNTIME || UNITY_EDITOR
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
	public class IgorZip : IgorModuleBase
	{
		public static string ZipFlag = "zip";
		public static string UnzipFlag = "unzip";
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
			if(IgorPackageCommon.RunPackageStepsThisJob() && IgorJobConfig.IsBoolParamSet(ZipFlag) && IgorJobConfig.GetStringParam(ZipFilenameFlag) != "")
			{
				IgorCore.SetModuleActiveForJob(this);
				StepHandler.RegisterJobStep(IgorPackageCommon.PackageStep, this, CreateZip);
			}

			if(IgorPackageCommon.RunPackageStepsThisJob() && IgorJobConfig.IsBoolParamSet(UnzipFlag))
			{
				IgorCore.SetModuleActiveForJob(this);
				StepHandler.RegisterJobStep(IgorPackageCommon.UnpackageStep, this, UnzipProducts);
			}
		}

#if UNITY_EDITOR
		public override string DrawJobInspectorAndGetEnabledParams(string CurrentParams)
		{
			string EnabledParams = CurrentParams;

			bool bZip = DrawBoolParam(ref EnabledParams, "Zip the built files", ZipFlag);
			bool bUnzip = DrawBoolParam(ref EnabledParams, "Unzip the requested file", UnzipFlag);

            if(bZip)
            {
			    DrawStringConfigParam(ref EnabledParams, "Zip filename", ZipFilenameFlag);
            }
            else
            {
                EnabledParams = IgorRuntimeUtils.ClearParam(EnabledParams, ZipFilenameFlag);
            }

			return EnabledParams;
		}
#endif // UNITY_EDITOR

		public virtual bool CreateZip()
		{
			string ZipFilename = GetParamOrConfigString(ZipFilenameFlag, "Zip destination filename is not set.");

			string LogDetails = "Creating zip file with name " + ZipFilename + " from files:";

			List<string> BuiltProducts = IgorCore.GetModuleProducts();

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
				IgorRuntimeUtils.DeleteFile(ZipFilename);
			}

			IgorRuntimeUtils.PlatformNames CurrentPlatform = IgorRuntimeUtils.RuntimeOrEditorGetPlatform();

			if(CurrentPlatform == IgorRuntimeUtils.PlatformNames.Editor_OSX || CurrentPlatform == IgorRuntimeUtils.PlatformNames.Standalone_OSX)
			{
				ZipFilesMac(ModuleInst, FilesToZip, ZipFilename, bUpdateBuildProducts, RootDir);
			}
			else if(CurrentPlatform == IgorRuntimeUtils.PlatformNames.Editor_Windows || CurrentPlatform == IgorRuntimeUtils.PlatformNames.Standalone_Windows)
			{
				ZipFilesWindows(ModuleInst, FilesToZip, ZipFilename, bUpdateBuildProducts, RootDir);
			}
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

			if(IgorRuntimeUtils.RunProcessCrossPlatform(ModuleInst, "zip", "", ZipParams, Path.GetFullPath(RootDir), "Zipping the files", true) == 0)
			{
				IgorDebug.Log(ModuleInst, "Zip file " + ZipFilename + " created successfully!\nOutput:\n" + ZipOutput + "\nError\n" + ZipError);

				if(bUpdateBuildProducts)
				{
					List<string> NewProducts = new List<string>();

					NewProducts.Add(ZipFilename);

					IgorCore.SetNewModuleProducts(NewProducts);
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
				IgorDebug.LogError(ModuleInst, "7Zip is not installed.  Currently 7Zip is the only zip tool supported on Windows.\nPlease download it from here: http://www.7-zip.org/download.html");
				IgorDebug.LogError(ModuleInst, "Skipping zip step.");

				return;
			}

			foreach(string CurrentFile in FilesToZip)
			{
				ZipParams += "\"" + CurrentFile + "\" ";
			}

			string ZipOutput = "";
			string ZipError = "";

			if(IgorRuntimeUtils.RunProcessCrossPlatform(ModuleInst, "", ZipCommand, ZipParams, Path.GetFullPath(RootDir), "Zipping the files") == 0)
			{
				IgorDebug.Log(ModuleInst, "Zip file " + ZipFilename + " created successfully!\nOutput:\n" + ZipOutput + "\nError\n" + ZipError);

				if(bUpdateBuildProducts)
				{
					List<string> NewProducts = new List<string>();

					NewProducts.Add(ZipFilename);

					IgorCore.SetNewModuleProducts(NewProducts);
				}
			}
		}

		public virtual bool UnzipProducts()
		{
			List<string> ZipFilename = IgorCore.GetModuleProducts();

			if(IgorAssert.EnsureTrue(this, ZipFilename.Count == 1, "Unzipping expected exactly 1 built product, but we found " + ZipFilename.Count))
			{
				Log("Unzipping file " + ZipFilename[0]);

				UnzipArchiveCrossPlatform(this, ZipFilename[0], Path.GetFullPath("."), true);
			}

			return true;
		}

		public static void UnzipArchiveCrossPlatform(IIgorModule ModuleInst, string ZipFilename, string DirectoryToUnzipTo, bool bUpdateBuildProducts = false)
		{
			IgorRuntimeUtils.PlatformNames CurrentPlatform = IgorRuntimeUtils.RuntimeOrEditorGetPlatform();

			if(CurrentPlatform == IgorRuntimeUtils.PlatformNames.Editor_OSX || CurrentPlatform == IgorRuntimeUtils.PlatformNames.Standalone_OSX)
			{
				UnzipFileMac(ModuleInst, ZipFilename, DirectoryToUnzipTo, bUpdateBuildProducts);
			}
			else if(CurrentPlatform == IgorRuntimeUtils.PlatformNames.Editor_Windows || CurrentPlatform == IgorRuntimeUtils.PlatformNames.Standalone_Windows)
			{
				UnzipFileWindows(ModuleInst, ZipFilename, DirectoryToUnzipTo, bUpdateBuildProducts);
			}
		}

		public static void UnzipFileMac(IIgorModule ModuleInst, string ZipFilename, string DirectoryToUnzipTo, bool bUpdateBuildProducts)
		{
			string ZipParams = "-o \"" + ZipFilename + "\"";

			if(DirectoryToUnzipTo != "")
			{
				ZipParams += " -d \"" + DirectoryToUnzipTo + "\"";
			}

			string ZipOutput = "";
			string ZipError = "";

			if(IgorRuntimeUtils.RunProcessCrossPlatform(ModuleInst, "/usr/bin/unzip", "", ZipParams, Path.GetFullPath("."), "Unzipping the archive " + ZipFilename + " to folder " + DirectoryToUnzipTo, false) == 0)
			{
				IgorDebug.Log(ModuleInst, "Zip file " + ZipFilename + " unzipped successfully!\nOutput:\n" + ZipOutput + "\nError\n" + ZipError);

				if(bUpdateBuildProducts)
				{
					if(IgorRuntimeUtils.RunProcessCrossPlatform(ModuleInst, "/usr/bin/unzip", "", "-v \"" + ZipFilename + "\"", Path.GetFullPath("."), "Listing the contents of " + ZipFilename, ref ZipOutput, ref ZipError, false) == 0)
					{
						IgorDebug.Log(ModuleInst, "Zip " + ZipFilename + " contents are:\nOutput:\n" + ZipOutput + "\nError\n" + ZipError);

						List<string> NewProducts = new List<string>();

						string[] Lines = ZipOutput.Split('\n');

						foreach(string ZipLine in Lines)
						{
							if(ZipLine.Contains("Defl") || ZipLine.Contains("Stored"))
							{
								if(!ZipLine.EndsWith("/"))
								{
									NewProducts.Add(ZipLine.Substring(ZipLine.LastIndexOf(' ') + 1));
								}
							}
						}

						IgorCore.SetNewModuleProducts(NewProducts);
					}
				}
			}
		}

		public static void UnzipFileWindows(IIgorModule ModuleInst, string ZipFilename, string DirectoryToUnzipTo, bool bUpdateBuildProducts)
		{
			string ZipParams = "/c unzip -o \"" + ZipFilename + "\"";

			if(DirectoryToUnzipTo != "")
			{
				ZipParams += " -d \"" + DirectoryToUnzipTo + "\"";
			}

			string ZipOutput = "";
			string ZipError = "";

			if(IgorRuntimeUtils.RunProcessCrossPlatform(ModuleInst, "", "c:\\windows\\system32\\cmd.exe", ZipParams, Path.GetFullPath("."), "Unzipping the archive " + ZipFilename + " to folder " + DirectoryToUnzipTo, false) == 0)
			{
				IgorDebug.Log(ModuleInst, "Zip file " + ZipFilename + " unzipped successfully!\nOutput:\n" + ZipOutput + "\nError\n" + ZipError);

				if(bUpdateBuildProducts)
				{
					if(IgorRuntimeUtils.RunProcessCrossPlatform(ModuleInst, "", "c:\\windows\\system32\\cmd.exe", "/c unzip -v \"" + ZipFilename + "\"", Path.GetFullPath("."), "Listing the contents of " + ZipFilename, ref ZipOutput, ref ZipError, false) == 0)
					{
						IgorDebug.Log(ModuleInst, "Zip " + ZipFilename + " contents are:\nOutput:\n" + ZipOutput + "\nError\n" + ZipError);

						List<string> NewProducts = new List<string>();

						string[] Lines = ZipOutput.Split(new char[]{'\n', '\r'});

						foreach(string ZipLine in Lines)
						{
							if(ZipLine.Contains("Defl") || ZipLine.Contains("Stored"))
							{
								if(!ZipLine.EndsWith("/"))
								{
									NewProducts.Add(ZipLine.Substring(ZipLine.LastIndexOf(' ') + 1));
								}
							}
						}

						IgorCore.SetNewModuleProducts(NewProducts);
					}
				}
			}
		}
	}
}

#endif // IGOR_RUNTIME || UNITY_EDITOR
