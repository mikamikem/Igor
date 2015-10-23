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
	public class IgorBuildAndroid : IgorModuleBase
	{
		public static StepID FixupAndroidProjStep = new StepID("FixupAndroidProj", 600);
		public static StepID CustomFixupAndroidProjStep = new StepID("3rdPartyFixupAndroidProj", 650);
		public static StepID BuildAndroidProjStep = new StepID("BuildAndroidProj", 700);

		public static string AndroidBuiltNameFlag = "BuiltAndroidName";
		public static string AndroidResignInReleaseFlag = "AndroidResignInRelease";
		public static string AndroidKeystoreFilenameFlag = "AndroidKeystoreFilename";
		public static string AndroidKeyAliasFlag = "AndroidKeyAlias";
		public static string AndroidKeystorePassFlag = "AndroidKeystorePass";
		public static string AndroidKeyAliasPassFlag = "AndroidKeyAliasPass";

		public override string GetModuleName()
		{
			return "Build.Android";
		}

		public override void RegisterModule()
		{
			bool DidRegister = IgorCore.RegisterNewModule(this);

			BuildOptionsDelegates.Clear();

		    IgorBuildCommon.RegisterBuildPlatforms(new string[] {"Android"});
		}

		public override void ProcessArgs(IIgorStepHandler StepHandler)
		{
			if(IgorJobConfig.IsBoolParamSet(IgorBuildCommon.BuildFlag))
			{
				IgorCore.SetModuleActiveForJob(this);

				string Platform = IgorJobConfig.GetStringParam(IgorBuildCommon.PlatformFlag);

				if(Platform.Contains("Android"))
				{
					JobBuildTarget = BuildTarget.Android;

					StepHandler.RegisterJobStep(IgorBuildCommon.SwitchPlatformStep, this, SwitchPlatforms);
					StepHandler.RegisterJobStep(IgorBuildCommon.BuildStep, this, BuildAndroid);
					StepHandler.RegisterJobStep(BuildAndroidProjStep, this, BuildAndroidProj);
				}
			}
		}

		public virtual string GetBuiltNameConfigKeyForPlatform(string PlatformName)
		{
			return "Built" + PlatformName + "Name";
		}

		public override bool ShouldDrawInspectorForParams(string CurrentParams)
		{
			bool bBuilding = IgorRuntimeUtils.IsBoolParamSet(CurrentParams, IgorBuildCommon.BuildFlag);
			bool bRecognizedPlatform = false;

			if(bBuilding)
			{
				string Platform = IgorRuntimeUtils.GetStringParam(CurrentParams, IgorBuildCommon.PlatformFlag);

				if(Platform == "Android")
				{
					bRecognizedPlatform = true;
				}
			}

			return bBuilding && bRecognizedPlatform;
		}

        public override string DrawJobInspectorAndGetEnabledParams(string CurrentParams)
		{
			string EnabledParams = CurrentParams;

			DrawStringConfigParamDifferentOverride(ref EnabledParams, "Built name", IgorBuildCommon.BuiltNameFlag, AndroidBuiltNameFlag);

			DrawStringConfigParam(ref EnabledParams, "Android Keystore Filename", AndroidKeystoreFilenameFlag);
			DrawStringConfigParam(ref EnabledParams, "Android Key Alias", AndroidKeyAliasFlag);
			DrawStringConfigParam(ref EnabledParams, "Android Keystore Password", AndroidKeystorePassFlag);
			DrawStringConfigParam(ref EnabledParams, "Android Key Alias Password", AndroidKeyAliasPassFlag);

			DrawBoolParam(ref EnabledParams, "Re-sign the APK for release", AndroidResignInReleaseFlag);

			return EnabledParams;
		}

		public BuildTarget JobBuildTarget = BuildTarget.StandaloneOSXIntel;
		public List<IgorBuildCommon.GetExtraBuildOptions> BuildOptionsDelegates = new List<IgorBuildCommon.GetExtraBuildOptions>();
		protected static string AndroidProjectUpdateAdditionalArgs = "";

		public virtual void AddDelegateCallback(IgorBuildCommon.GetExtraBuildOptions NewDelegate)
		{
			if(!BuildOptionsDelegates.Contains(NewDelegate))
			{
				BuildOptionsDelegates.Add(NewDelegate);
			}
		}
		
		public virtual string GetBuiltNameForTarget(BuildTarget NewTarget)
		{
			string BuiltName = "";

			bool bAndroid = false;

			if(NewTarget == BuildTarget.Android)
			{
				BuiltName = GetParamOrConfigString("BuiltAndroidName");
				bAndroid = true;
			}

			if(BuiltName == "")
			{
				BuiltName = Path.GetFileName(EditorUserBuildSettings.GetBuildLocation(NewTarget));
			}

			if(BuiltName == "")
			{
				if(bAndroid)
				{
					BuiltName = "Android";
				}
			}

			if(!BuiltName.EndsWith(".apk"))
			{
				BuiltName += ".apk";
			}

			return BuiltName;
		}

		public virtual BuildOptions GetExternalBuildOptions(BuildTarget CurrentTarget)
		{
			BuildOptions ExtraOptions = BuildOptions.None;

			foreach(IgorBuildCommon.GetExtraBuildOptions CurrentDelegate in BuildOptionsDelegates)
			{
				ExtraOptions |= CurrentDelegate(CurrentTarget);
			}

			return ExtraOptions;
		}

		public virtual bool SwitchPlatforms()
		{
			Log("Switching platforms to " + JobBuildTarget);

			EditorUserBuildSettings.SwitchActiveBuildTarget(JobBuildTarget);

			AndroidProjectUpdateAdditionalArgs = "";

			return true;
		}

		public virtual bool BuildAndroid()
		{
			Log("Building Android build (Target:" + JobBuildTarget + ")");

			return Build(BuildOptions.SymlinkLibraries);
		}

		public virtual bool Build(BuildOptions PlatformSpecificOptions)
		{
			if(!IgorAssert.EnsureTrue(this, GetAndroidSDKPath(this) != "", "Android SDK path is not set!"))
			{
				return true;
			}

			PlayerSettings.Android.keystorePass = GetParamOrConfigString(AndroidKeystorePassFlag, "Your Android Keystore Password isn't set!  We won't be able to sign your application!");
			PlayerSettings.Android.keyaliasPass = GetParamOrConfigString(AndroidKeyAliasPassFlag, "Your Android Key Alias Password isn't set!  We won't be able to sign your application!");

			if(PlayerSettings.Android.keystorePass == "" || PlayerSettings.Android.keyaliasPass == "")
			{
				return true;
			}

			FixReadOnlyFilesIn3rdPartyLibs();

			string AndroidProjDirectory = Path.Combine(Path.GetFullPath("."), "Android");
			
			if(AndroidProjDirectory.Contains(" "))
			{
				AndroidProjDirectory = Path.Combine(Path.GetTempPath() + PlayerSettings.productName, "Android");
			}
			
			if(Directory.Exists(AndroidProjDirectory))
			{
				IgorRuntimeUtils.DeleteDirectory(AndroidProjDirectory);
			}

			string FullBuiltPath = System.IO.Path.Combine(System.IO.Path.GetFullPath("."), GetBuiltNameForTarget(BuildTarget.Android));

			if(File.Exists(FullBuiltPath))
			{
				IgorRuntimeUtils.DeleteFile(FullBuiltPath);
			}
			
			// We need to force create the directory before we use it or it will prompt us for a path to build to
			Directory.CreateDirectory(AndroidProjDirectory);
			
			Log("Android project destination directory is: " + AndroidProjDirectory);

			EditorUserBuildSettings.symlinkLibraries = true;
			EditorUserBuildSettings.exportAsGoogleAndroidProject = true;

			EditorUserBuildSettings.SetBuildLocation(BuildTarget.Android, AndroidProjDirectory);

			BuildOptions AllOptions = PlatformSpecificOptions | BuildOptions.AcceptExternalModificationsToPlayer;

			AllOptions |= GetExternalBuildOptions(JobBuildTarget);

			BuildPipeline.BuildPlayer(IgorUtils.GetLevels(), AndroidProjDirectory, BuildTarget.Android, AllOptions);

			CopyActivityOverrideSourceFiles(AndroidProjDirectory);

			List<string> BuiltFiles = new List<string>();

			BuiltFiles.Add(AndroidProjDirectory);

			IgorCore.SetNewModuleProducts(BuiltFiles);

			Log("Android Eclipse project has been created.");

			return true;
		}

		public virtual void CopyActivityOverrideSourceFiles(string RootDirectory)
		{
			IgorRuntimeUtils.DirectoryCopy(Path.Combine(".", Path.Combine("Assets", Path.Combine("Plugins", Path.Combine("Android", "src")))), Path.Combine(RootDirectory, Path.Combine(PlayerSettings.productName, "src")), true);
		}

		public virtual bool BuildAndroidProj()
		{
			List<string> BuildProducts = IgorCore.GetModuleProducts();

			if(IgorAssert.EnsureTrue(this, BuildProducts.Count > 0, "Building the Android project, but there were no previous built products."))
			{
				Log("Project should be saved to " + EditorUserBuildSettings.GetBuildLocation(BuildTarget.Android));
				string BuiltProjectDir = Path.Combine(BuildProducts[0], PlayerSettings.productName);
				if(!RunAndroidCommandLineUtility(this, BuiltProjectDir, "update project --path ." + AndroidProjectUpdateAdditionalArgs))
				{
					return true;
				}

				string BuildXML = Path.Combine(BuiltProjectDir, "build.xml");
		    	if(!IgorAssert.EnsureTrue(this, File.Exists(BuildXML), "Can't check " + BuildXML + " for APK name because it doesn't exist."))
		    	{
		    		return false;
		    	}

				string BuildXMLFileContents = File.ReadAllText(BuildXML);

				int ProjectNameParamStart = BuildXMLFileContents.IndexOf("<project name=\"") + "<project name=\"".Length;
				int ProjectNameParamEnd = BuildXMLFileContents.IndexOf("\"", ProjectNameParamStart);

				string APKName = BuildXMLFileContents.Substring(ProjectNameParamStart, ProjectNameParamEnd - ProjectNameParamStart);

				if(!RunAnt(this, BuiltProjectDir, "clean debug"))
				{
					return true;
				}

				Log("Debug APK built!");

				string DebugSignedAPK = Path.Combine(BuiltProjectDir, Path.Combine("bin", APKName + "-debug.apk"));
				string AppropriatelySignedAPK = DebugSignedAPK;

				if(IgorJobConfig.IsBoolParamSet(AndroidResignInReleaseFlag))
				{
					Log("Re-signing the APK for release.");

					string RepackageDir = Path.Combine(BuildProducts[0], "Repackage");

					if(!ResignAPK(this, DebugSignedAPK, RepackageDir, ref AppropriatelySignedAPK,
						GetParamOrConfigString(AndroidKeystoreFilenameFlag, "Android Keystore filename isn't set, but you want to re-sign the APK!"),
						GetParamOrConfigString(AndroidKeystorePassFlag, "Android Keystore password isn't set, but you want to re-sign the APK!"),
						GetParamOrConfigString(AndroidKeyAliasFlag, "Android Key Alias isn't set, but you want to re-sign the APK!"),
						GetParamOrConfigString(AndroidKeyAliasPassFlag, "Android Key Alias password isn't set, but you want to re-sign the APK!")))
					{
						return true;
					}

					Log("Re-signing the APK succeeded!");
				}

				string FinalBuildProductName = GetBuiltNameForTarget(BuildTarget.Android);

				if(File.Exists(FinalBuildProductName))
				{
					IgorRuntimeUtils.DeleteFile(FinalBuildProductName);
				}

				File.Copy(AppropriatelySignedAPK, FinalBuildProductName);

				List<string> NewBuildProducts = new List<string>();

				if(IgorAssert.EnsureTrue(this, File.Exists(AppropriatelySignedAPK), "The built APK " + AppropriatelySignedAPK + " doesn't exist.  Something went wrong during the build step.  Please check the logs!"))
				{
					NewBuildProducts.Add(AppropriatelySignedAPK);
				}

				IgorCore.SetNewModuleProducts(NewBuildProducts);

				Log("APK built and renamed to " + AppropriatelySignedAPK + ".");
			}

			return true;
		}

		public static bool ResignAPK(IIgorModule ModuleInst, string SourceAPK, string RepackagingDirectory, ref string FinalFilename, string KeystoreFilename,
									 string KeystorePassword, string KeyAlias, string KeyAliasPassword)
		{
			if(Directory.Exists(RepackagingDirectory))
			{
				IgorRuntimeUtils.DeleteDirectory(RepackagingDirectory);
			}

			Directory.CreateDirectory(RepackagingDirectory);

			IgorZip.UnzipArchiveCrossPlatform(ModuleInst, SourceAPK, RepackagingDirectory);

			IgorRuntimeUtils.DeleteDirectory(Path.Combine(RepackagingDirectory, "META-INF"));

			string UnsignedAPK = Path.Combine(RepackagingDirectory, "Repackaged.unsigned.apk");

			List<string> APKContents = IgorRuntimeUtils.GetListOfFilesAndDirectoriesInDirectory(RepackagingDirectory);

			IgorZip.ZipFilesCrossPlatform(ModuleInst, APKContents, UnsignedAPK, false, RepackagingDirectory);

			string SignedAPK = Path.Combine(RepackagingDirectory, "Repackaged.signed.apk");

//			IgorCore.LogError(ModuleInst, "jarsigner command running from " + Path.GetFullPath(".") + " is\n" + "-verbose -keystore \"" + KeystoreFilename + "\" -storepass " + KeystorePassword +
//				" -keypass " + KeyAliasPassword + " -signedjar \"" + SignedAPK + "\" \"" + UnsignedAPK + "\" " + KeyAlias);

			if(IgorRuntimeUtils.RunProcessCrossPlatform(ModuleInst, "jarsigner", "jarsigner", "-verbose -sigalg SHA1withDSA -digestalg SHA1 -keystore \"" + KeystoreFilename + "\" -storepass " + KeystorePassword + " -keypass " +
				KeyAliasPassword + " -signedjar \"" + SignedAPK + "\" \"" + UnsignedAPK + "\" " + KeyAlias, Path.GetFullPath("."), "Running jarsigner", true) != 0)
			{
				return false;
			}

			string ZipAlignPath = GetZipAlignPath(ModuleInst);
			string AlignedAPK = Path.Combine(RepackagingDirectory, "Repackaged.aligned.apk");

			if(IgorRuntimeUtils.RunProcessCrossPlatform(ModuleInst, ZipAlignPath, ZipAlignPath, "-v 4 \"" + SignedAPK + "\" \"" + AlignedAPK + "\"", Path.GetFullPath("."), "Running zipalign") != 0)
			{
				return false;
			}

			FinalFilename = AlignedAPK;

			return true;
		}

		public static void AddNewLibrary(string LibraryFolderName)
		{
			AndroidProjectUpdateAdditionalArgs += " --library ../" + LibraryFolderName;
		}

		public static bool RunAnt(IIgorModule ModuleInst, string ProjectDirectory, string Targets)
		{
			string ANT_ROOT = IgorRuntimeUtils.GetEnvVariable("ANT_ROOT");
			string AntCommand = "";

			string FinalParams = "";

#if UNITY_EDITOR_OSX
			if(ANT_ROOT != "")
			{
				AntCommand = Path.Combine(ANT_ROOT, Path.Combine("bin", "ant"));
			}
			else
			{
				AntCommand = "/usr/bin/ant";
			}

			FinalParams += Targets + " -lib " +
				Path.Combine(EditorApplication.applicationPath, Path.Combine("Contents", Path.Combine("PlaybackEngines", Path.Combine("AndroidPlayer", Path.Combine("bin", "classes.jar")))));
#else
			AntCommand = "C:\\Windows\\System32\\cmd.exe";

			FinalParams += "/C " + ANT_ROOT + "bin\\ant.bat " + Targets + " -lib " +
				Path.Combine(EditorApplication.applicationPath, Path.Combine("Data", Path.Combine("PlaybackEngines", Path.Combine("androidplayer", Path.Combine("bin", "classes.jar")))));
#endif // UNITY_EDITOR_OSX

			if(!IgorAssert.EnsureTrue(ModuleInst, File.Exists(AntCommand), "Can't find the Ant executable!  Did you set your ANT_ROOT?"))
			{
				return false;
			}

//			IgorCore.LogError(ModuleInst, "Ant params are " + FinalParams);

			return IgorRuntimeUtils.RunProcessCrossPlatform(ModuleInst, AntCommand, AntCommand, FinalParams, ProjectDirectory, "Running Ant build") == 0;
		}

		public static string GetZipAlignPath(IIgorModule ModuleInst)
		{
			string AndroidSDKPath = GetAndroidSDKPath(ModuleInst);
			string ZipAlignPath = "";

			if(IgorAssert.EnsureTrue(ModuleInst, Directory.Exists(AndroidSDKPath), "The Android SDK path " + AndroidSDKPath + " doesn't exist!"))
			{
				string BuildToolsPath = Path.Combine(AndroidSDKPath, "build-tools");

				if(IgorAssert.EnsureTrue(ModuleInst, Directory.Exists(BuildToolsPath), "The Android build tools path " + BuildToolsPath + " doesn't exist!"))
				{
					List<string> BuildToolVersions = IgorRuntimeUtils.GetListOfFilesAndDirectoriesInDirectory(BuildToolsPath, false, true, false, true, true);

					foreach(string CurrentVersion in BuildToolVersions)
					{
						string ZipAlignVersionPath = Path.Combine(BuildToolsPath, Path.Combine(CurrentVersion, "zipalign"));

						if(File.Exists(ZipAlignVersionPath))
						{
							ZipAlignPath = ZipAlignVersionPath;

							break;
						}
					}

					IgorAssert.EnsureTrue(ModuleInst, ZipAlignPath != "", "ZipAlign couldn't be found!  Have you downloaded the android build-tools?");
				}
			}

			return ZipAlignPath;
		}

		public static string GetAndroidSDKPath(IIgorModule ModuleInst)
		{
			IgorAssert.EnsureTrue(ModuleInst, EditorPrefs.HasKey("AndroidSdkRoot"), "You haven't set your Android SDK root yet!  This build will fail!");

			return EditorPrefs.GetString("AndroidSdkRoot");
		}

		public static bool RunAndroidCommandLineUtility(IIgorModule ModuleInst, string ProjectDirectory, string Command)
		{
			return IgorRuntimeUtils.RunProcessCrossPlatform(ModuleInst, GetAndroidSDKPath(ModuleInst) + "/tools/android", GetAndroidSDKPath(ModuleInst) + "/tools/android.bat", Command, ProjectDirectory, "Running Android project helper utility") == 0;
		}

		public static void SwapStringValueInStringsXML(string Filename, string StringKey, string NewStringValue, string OldStringValue = null)
		{
			if(File.Exists(Filename))
			{
				StreamReader OriginalFileReader = new StreamReader(Filename);
				string OriginalFile = OriginalFileReader.ReadToEnd();
				
				OriginalFileReader.Close();
				
				string StartString = "<string name=\"" + StringKey + "\">";
				string OptionalOldStringValue = "";
				
				if(OldStringValue != null)
				{
					OptionalOldStringValue = OldStringValue + "</string>";
				}
				else
				{
					int StartPosition = OriginalFile.IndexOf(StartString) + StartString.Length;
					OptionalOldStringValue = OriginalFile.Substring(StartPosition, OriginalFile.IndexOf("</string>", StartPosition) - StartPosition);
				}
				
				string ToReplace = StartString + OptionalOldStringValue;
				
				if(OriginalFile.Contains(ToReplace))
				{
					string NewValue = StartString + NewStringValue + "</string>";
					string NewFile = OriginalFile.Replace(ToReplace, NewValue);

					StreamWriter NewFileWriter = new StreamWriter(Filename);
				
					NewFileWriter.Write(NewFile);
					
					NewFileWriter.Close();
				}
			}
		}

		// This is necessary as of 5.x  If the 3rd party libraries that you include are marked read only (like when you use Perforce as a version control system)
		// your build will fail in the built-in Unity postprocess step, so we just set them all to read/write before we build.
		public static void FixReadOnlyFilesIn3rdPartyLibs()
		{
			string AndroidPluginPathRoot = Path.Combine(Path.GetFullPath("."), Path.Combine("Assets", Path.Combine("Plugins", "Android")));
			List<string> Android3rdPartyLibFiles = IgorRuntimeUtils.GetListOfFilesAndDirectoriesInDirectory(AndroidPluginPathRoot, true, false, true);

			foreach(string CurrentFilePath in Android3rdPartyLibFiles)
			{
				string CurrentFullPath = Path.Combine(AndroidPluginPathRoot, CurrentFilePath);
				if(File.Exists(CurrentFullPath))
				{
			        File.SetAttributes(CurrentFullPath, System.IO.FileAttributes.Normal);
				}
			}
		}
	}
}