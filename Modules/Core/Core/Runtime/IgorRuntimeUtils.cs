using UnityEngine;
using System;
using System.Xml.Serialization;
using System.Reflection;
using System.Collections.Generic;
using System.IO;

namespace Igor
{
    [System.Serializable]
	public class IgorPersistentJobConfig
	{
		public string JobCommandLineParams = "";
		public string JobName = "";
	}

	[XmlRoot("IgorJobConfig")]
	public class IgorJobConfig
	{
		public static string IgorJobConfigPath = "IgorJob.xml";
		public static IgorJobConfig InternalOverride = null;

		public IgorPersistentJobConfig Persistent = new IgorPersistentJobConfig();

		public int LastPriority = -999999;
		public int LastIndexInPriority = -999999;

		public bool bIsRunning = false;
		public bool bMenuTriggered = false;

	 	public void Save(string path)
	 	{
	 		XmlSerializer serializer = new XmlSerializer(typeof(IgorJobConfig));
	 		using(FileStream stream = new FileStream(path, FileMode.Create))
	 		{
	 			serializer.Serialize(stream, this);
	 		}
	 	}
	 
	 	public static IgorJobConfig Load(string path)
	 	{
	 		XmlSerializer serializer = new XmlSerializer(typeof(IgorJobConfig));
	 		using(var stream = new FileStream(path, FileMode.Open))
	 		{
	 			return serializer.Deserialize(stream) as IgorJobConfig;
	 		}
	 	}

	 	public static IgorJobConfig GetConfig()
	 	{
	 		if(InternalOverride != null)
	 		{
	 			return InternalOverride;
	 		}

	 		if(File.Exists(IgorJobConfigPath))
	 		{
				return IgorJobConfig.Load(IgorJobConfigPath);
			}
			else
			{
				IgorJobConfig NewConfig = new IgorJobConfig();

				NewConfig.Save(IgorJobConfigPath);

				return NewConfig;
			}
	 	}

	 	public static void Cleanup()
	 	{
	 		if(File.Exists(IgorJobConfigPath))
	 		{
				IgorRuntimeUtils.DeleteFile(IgorJobConfigPath);
	 		}
	 	}

	 	public static bool IsBoolParamSet(string BoolParam)
	 	{
	 		IgorJobConfig Inst = GetConfig();

	 		if(Inst != null)
	 		{
	 			return IgorRuntimeUtils.IsBoolParamSet(Inst.Persistent.JobCommandLineParams, BoolParam);
	 		}

	 		return false;
	 	}

	 	public static void SetBoolParam(string BoolParam, bool bTrue)
	 	{
	 		IgorJobConfig Inst = GetConfig();

	 		if(Inst != null)
	 		{
	 			Inst.Persistent.JobCommandLineParams = IgorRuntimeUtils.SetBoolParam(Inst.Persistent.JobCommandLineParams, BoolParam, bTrue);

	 			Inst.Save(IgorJobConfigPath);
	 		}
	 	}

		public static bool IsStringParamSet(string StringParam)
	 	{
	 		IgorJobConfig Inst = GetConfig();

	 		if(Inst != null)
	 		{
	 			return IgorRuntimeUtils.IsStringParamSet(Inst.Persistent.JobCommandLineParams, StringParam);
	 		}

	 		return false;
	 	}

	 	public static string GetStringParam(string ParamKey)
	 	{
	 		IgorJobConfig Inst = GetConfig();

	 		if(Inst != null)
	 		{
	 			return IgorRuntimeUtils.GetStringParam(Inst.Persistent.JobCommandLineParams, ParamKey);
	 		}

	 		return "";
	 	}

	 	public static void SetStringParam(string ParamKey, string ParamValue)
	 	{
	 		IgorJobConfig Inst = GetConfig();

	 		if(Inst != null)
	 		{
	 			Inst.Persistent.JobCommandLineParams = IgorRuntimeUtils.SetStringParam(Inst.Persistent.JobCommandLineParams, ParamKey, ParamValue);

	 			Inst.Save(IgorJobConfigPath);
	 		}
	 	}

	 	public static int GetLastPriority()
	 	{
	 		IgorJobConfig Inst = GetConfig();

	 		if(Inst != null)
	 		{
	 			return Inst.LastPriority;
	 		}

	 		return 0;
	 	}

	 	public static void SetLastPriority(int NewLastPriority)
	 	{
	 		IgorJobConfig Inst = GetConfig();

	 		if(Inst != null)
	 		{
	 			Inst.LastPriority = NewLastPriority;

	 			Inst.Save(IgorJobConfigPath);
	 		}
	 	}

	 	public static int GetLastIndexInPriority()
	 	{
	 		IgorJobConfig Inst = GetConfig();

	 		if(Inst != null)
	 		{
	 			return Inst.LastIndexInPriority;
	 		}

	 		return 0;
	 	}

	 	public static void SetLastIndexInPriority(int NewLastIndex)
	 	{
	 		IgorJobConfig Inst = GetConfig();

	 		if(Inst != null)
	 		{
	 			Inst.LastIndexInPriority = NewLastIndex;

	 			Inst.Save(IgorJobConfigPath);
	 		}
	 	}

	 	public static bool GetIsRunning()
	 	{
	 		IgorJobConfig Inst = GetConfig();

	 		if(Inst != null)
	 		{
	 			return Inst.bIsRunning;
	 		}

	 		return false;
	 	}

	 	public static void SetIsRunning(bool bRunning)
	 	{
	 		IgorJobConfig Inst = GetConfig();

	 		if(Inst != null)
	 		{
	 			Inst.bIsRunning = bRunning;

	 			Inst.Save(IgorJobConfigPath);
	 		}
	 	}

	 	public static bool GetWasMenuTriggered()
	 	{
	 		IgorJobConfig Inst = GetConfig();

	 		if(Inst != null)
	 		{
	 			return Inst.bMenuTriggered;
	 		}

	 		return false;
	 	}

	 	public static void SetWasMenuTriggered(bool bMenu)
	 	{
	 		IgorJobConfig Inst = GetConfig();

	 		if(Inst != null)
	 		{
	 			Inst.bMenuTriggered = bMenu;

	 			Inst.Save(IgorJobConfigPath);
	 		}
	 	}
	}

	public class IgorRuntimeUtils
	{
		public delegate bool JobStepFunc(); // Return true if the function finished

		// These are duplicated into IgorUpdater.cs.  This was necessary to keep the updater
		// working, but they are overridden by these functions once Core is installed.  If
		// you have any fixes for these two functions, fix them both here and in IgorUpdater.cs.
		public static void DeleteFile(string TargetFile)
		{
			if(File.Exists(TargetFile))
			{
		        File.SetAttributes(TargetFile, System.IO.FileAttributes.Normal);
		        File.Delete(TargetFile);
		    }
		}

		// These are duplicated into IgorUpdater.cs.  This was necessary to keep the updater
		// working, but they are overridden by these functions once Core is installed.  If
		// you have any fixes for these two functions, fix them both here and in IgorUpdater.cs.
		public static void DeleteDirectory(string targetDir)
		{
			if(!Directory.Exists(targetDir))
			{
				return;
			}

		    System.IO.File.SetAttributes(targetDir, System.IO.FileAttributes.Normal);
		
		    string[] files = System.IO.Directory.GetFiles(targetDir);
		    string[] dirs = System.IO.Directory.GetDirectories(targetDir);
		
		    foreach (string file in files)
		    {
		        System.IO.File.SetAttributes(file, System.IO.FileAttributes.Normal);
		        System.IO.File.Delete(file);
		    }
		
		    foreach (string dir in dirs)
		    {
		        DeleteDirectory(dir);
		    }
		
		    System.IO.Directory.Delete(targetDir, false);
		}

        public static string ClearParam(string AllParams, string Param)
        {
            string Query = string.Empty;
            string StringValue = "";
            bool bStringParam = false;

            if(IsBoolParamSet(AllParams, Param))
            {
                Query = "--" + Param + " ";
            }
            else
            if(IsStringParamSet(AllParams, Param))
            {
            	bStringParam = true;
                StringValue = GetStringParam(AllParams, Param);
                Query = "--" + Param + "=" + "\"" + StringValue + "\"";
            }

            int StartParamIndex = AllParams.IndexOf(Query);

            if(StartParamIndex == -1 && !bStringParam)
            {
            	Query = "--" + Param;
            	StartParamIndex = AllParams.IndexOf(Query);

            	if((StartParamIndex + Query.Length) != AllParams.Length)
            	{
            		StartParamIndex = -1;
            	}
            }

            if(StartParamIndex == -1 && bStringParam)
            {
            	Query = "--" + Param + "=" + StringValue;
            	StartParamIndex = AllParams.IndexOf(Query);
            }

            if(StartParamIndex != -1)
            {
	            string TrimTarget = AllParams.Substring(0, StartParamIndex);
	            string TrimmedTarget = TrimTarget.TrimEnd(new char[] { ' ' });
	            int Difference = TrimTarget.Length - TrimmedTarget.Length;
	            string ReplaceText = AllParams.Substring(StartParamIndex - Difference, Query.Length + Difference);
	            
	            if(!string.IsNullOrEmpty(ReplaceText))
	            {
	                AllParams = AllParams.Replace(ReplaceText, string.Empty);
	            }
	        }

            return AllParams;
        }

	 	public static bool IsBoolParamSet(string AllParams, string BoolParam)
	 	{
            string ContainsQuery = "--" + BoolParam + " ";
            bool bHasFlag = AllParams.Contains(ContainsQuery);

            if(!bHasFlag)
            {
            	ContainsQuery = "--" + BoolParam;
            	int StartParamIndex = AllParams.IndexOf(ContainsQuery);

            	if((StartParamIndex + ContainsQuery.Length) == AllParams.Length)
            	{
            		bHasFlag = true;
            	}
            }

 			if(bHasFlag)
 			{
                string Substring = AllParams.Substring(AllParams.IndexOf(ContainsQuery) + ContainsQuery.Length);
                if(Substring.Length > 0)
                {
                    return Substring[0] != '=';
                }

 				return true;
 			}

 			return false;
	 	}

	 	public static string SetBoolParam(string OriginalParams, string BoolParam, bool bTrue)
	 	{
	 		string NewParams = OriginalParams;

 			if(NewParams.Contains("--" + BoolParam))
 			{
 				if(!bTrue)
 				{
 					NewParams = NewParams.Replace(" --" + BoolParam, string.Empty);
 					NewParams = NewParams.Replace("--" + BoolParam, string.Empty);
 				}
 			}
 			else
 			{
 				if(bTrue)
 				{
 					NewParams += " --" + BoolParam;
 				}
 			}

 			return NewParams;
	 	}

	 	public static bool IsStringParamSet(string AllParams, string ParamKey)
	 	{
 			if(AllParams.Contains("--" + ParamKey + "="))
 			{
 				return true;
 			}

 			return false;
	 	}

	 	public static string GetStringParam(string AllParams, string ParamKey)
	 	{
            string Result = string.Empty;
 			
            if(AllParams.Contains("--" + ParamKey + "="))
 			{
 			    int StartingPos = 1, EndingPos = -1;

                string Prefix = "--" + ParamKey + "=\"";
 				StartingPos = AllParams.IndexOf(Prefix) + (Prefix).Length;

                string StartSubstring = AllParams.Substring(StartingPos);

 			    EndingPos = StartSubstring.IndexOf("\"");
                if(EndingPos < 0)
                {
                    EndingPos = StartSubstring.Length;
                }
                else
                {
                    while(EndingPos < StartSubstring.Length && (StartSubstring[EndingPos] == '"' || StartSubstring[EndingPos] == ' '))
                        EndingPos++;
                }

                if(EndingPos >= StartSubstring.Length)
                {
                    EndingPos = StartSubstring.Length;
                }
 				
                Result = StartSubstring.Substring(0, EndingPos);

                // Manually trim. We want to remove all space to the left/right of quotation marks, AND the quotation
                // marks themselves, but not the space within the quotation marks.

                while(Result.Length > 0 && (Result[Result.Length - 1] == ' ' || Result[Result.Length - 1] == '"'))
                {
                    bool bIsQuotation = Result[Result.Length - 1] == '"';
                    Result = Result.Remove(Result.Length - 1, 1);
                    if(bIsQuotation)
                        break;
                }

                while(Result.Length > 0 && (Result[0] == ' ' || Result[0] == '"'))
                {
                    bool bIsQuotation = Result[0] == '"';
                    Result = Result.Remove(0, 1);
                    if(bIsQuotation)
                        break;
                }
 			}

 			return Result;
	 	}

	 	public static string SetStringParam(string AllParams, string ParamKey, string ParamValue)
	 	{
	 		string NewParams = AllParams;

 			if(NewParams.Contains("--" + ParamKey + "="))
 			{
 				NewParams = ClearParam(AllParams, ParamKey);
 			}

 			if(ParamValue != string.Empty)
 			{
                string MungedParamValue = ParamValue;
                if(!MungedParamValue.StartsWith("\""))
                    MungedParamValue = "\"" + MungedParamValue;
                if(!MungedParamValue.EndsWith("\""))
                    MungedParamValue = MungedParamValue + "\"";
 				NewParams += " --" + ParamKey + "=" + MungedParamValue;
 			}

 			return NewParams;
	 	}

	 	public class EnvMapping
		{
			public string Key;
			public string EnvKey;
			public bool bStaticValue;
			
			public EnvMapping(string NewKey, string NewEnvKey, bool bStaticValueMap = false)
			{
				Key = NewKey;
				EnvKey = NewEnvKey;
				bStaticValue = bStaticValueMap;
			}
		}

		public static List<EnvMapping> EnvVars = new List<EnvMapping>();

		public static string UpdatePathForEnvVariables(string OriginalPath)
		{
			string CurrentPath = OriginalPath;

			if(CurrentPath.Length > 0 && CurrentPath[0] == '~')
			{
				CurrentPath = System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal) + CurrentPath.Substring(1);
			}
			
			foreach(EnvMapping CurrentMap in EnvVars)
			{
				if(CurrentPath.Contains("$" + CurrentMap.Key))
				{
					string EnvironmentValue = "";
					
					if(CurrentMap.bStaticValue)
					{
						EnvironmentValue = CurrentMap.EnvKey;
					}
					else
					{
						EnvironmentValue = System.Environment.GetEnvironmentVariable(CurrentMap.EnvKey);
					}
					
					if(EnvironmentValue != null)
					{
						CurrentPath = CurrentPath.Replace("$" + CurrentMap.Key, EnvironmentValue);
					}
				}
			}
			
			return CurrentPath;
		}

		public static int RunProcessCrossPlatform(IIgorModule ModuleInst, string OSXCommand, string WindowsCommand, string Parameters, string Directory, string CommandLogDescription, bool bUseShell = false)
		{
			string ProcessOutput = "";
			string ProcessError = "";

			int RunProcessExitCode = RunProcessCrossPlatform(OSXCommand, WindowsCommand, Parameters, Directory, ref ProcessOutput, ref ProcessError, bUseShell);

			if(!IgorAssert.EnsureTrue(ModuleInst, RunProcessExitCode == 0, CommandLogDescription + " failed!\nOutput:\n" + ProcessOutput + "\n\n\nError:\n" + ProcessError))
			{
				return RunProcessExitCode;
			}

			IgorDebug.Log(ModuleInst, CommandLogDescription + " succeeded!\nOutput:\n" + ProcessOutput + "\n\n\nError:\n" + ProcessError);

			return RunProcessExitCode;
		}

		public static int RunProcessCrossPlatform(string OSXCommand, string WindowsCommand, string Parameters, string Directory, ref string Output, ref string Error, bool bUseShell = false)
		{
			string Command = "";
			
			IgorRuntimeUtils.PlatformNames CurrentPlatform = IgorRuntimeUtils.RuntimeOrEditorGetPlatform();

			if(CurrentPlatform == IgorRuntimeUtils.PlatformNames.Editor_OSX || CurrentPlatform == IgorRuntimeUtils.PlatformNames.Standalone_OSX)
			{
				Command = OSXCommand;
			}
			else if(CurrentPlatform == IgorRuntimeUtils.PlatformNames.Editor_Windows || CurrentPlatform == IgorRuntimeUtils.PlatformNames.Standalone_Windows)
			{
				Command = WindowsCommand;
			}
			
			Command = UpdatePathForEnvVariables(Command);
			
			System.Diagnostics.ProcessStartInfo NewStartInfo = new System.Diagnostics.ProcessStartInfo();
			NewStartInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
			
			if(!bUseShell)
			{
				System.IO.FileInfo NewFileInfo = new System.IO.FileInfo(Command);
			
				NewStartInfo.FileName = NewFileInfo.FullName;
			}
			else
			{
				NewStartInfo.FileName = Command;
			}

			NewStartInfo.Arguments = Parameters;

			NewStartInfo.WorkingDirectory = "";

			if(CurrentPlatform == IgorRuntimeUtils.PlatformNames.Editor_OSX || CurrentPlatform == IgorRuntimeUtils.PlatformNames.Standalone_OSX)
			{
				NewStartInfo.WorkingDirectory = Directory;
			}
			else if(CurrentPlatform == IgorRuntimeUtils.PlatformNames.Editor_Windows || CurrentPlatform == IgorRuntimeUtils.PlatformNames.Standalone_Windows)
			{
				NewStartInfo.WorkingDirectory = Directory.Replace("/", "\\");
			}

			NewStartInfo.UseShellExecute = bUseShell;
			NewStartInfo.RedirectStandardOutput = !bUseShell;
			NewStartInfo.RedirectStandardError = !bUseShell;
			
//			Debug.Log("Attempting to start process:\n" + NewStartInfo.FileName + "\nWith parameters:\n" + NewStartInfo.Arguments + "\nIn directory:\n" + NewStartInfo.WorkingDirectory);
			
			System.Diagnostics.Process NewProcess = System.Diagnostics.Process.Start(NewStartInfo);

			if(!bUseShell)
			{
				Output = NewProcess.StandardOutput.ReadToEnd();
				Error = NewProcess.StandardError.ReadToEnd();
			}

			NewProcess.WaitForExit();

			return NewProcess.ExitCode;
		}

		public static List<Type> GetTypesInheritFrom<InheritType>(bool bExcludeTemplateType = true)
		{
			List<Type> AllTypes = new List<Type>();

			Assembly[] Assemblies = AppDomain.CurrentDomain.GetAssemblies();

			foreach(Assembly CurrentAssembly in Assemblies)
			{
				foreach(Type CurrentType in CurrentAssembly.GetTypes())
				{
					if(typeof(InheritType).IsAssignableFrom(CurrentType) && (!bExcludeTemplateType || typeof(InheritType) != CurrentType))
					{
						AllTypes.Add(CurrentType);
					}
				}
			}

			return AllTypes;
		}

		public static string GetEnvVariable(string EnvironmentVariableName)
		{
			string Value = "";

			Value = System.Environment.GetEnvironmentVariable(EnvironmentVariableName);

			if(Value == null)
			{
				Value = "";
			}

			return Value;
		}

		public enum PlatformNames
		{
			Editor_Windows,
			Editor_OSX,
			Standalone_Windows,
			Standalone_OSX,
			Standalone_Linux,
			Standalone_iOS,
			Standalone_Android,
			Unknown
		}

		public static PlatformNames RuntimeOrEditorGetPlatform()
		{
#if UNITY_EDITOR
			string FullPath = Path.GetFullPath(".");

			if(FullPath.Length > 1)
			{
				if(FullPath[1] == ':')
				{
					return PlatformNames.Editor_Windows;
				}
				else
				{
					return PlatformNames.Editor_OSX;
				}
			}
#else
#if UNITY_STANDALONE_WIN
			return PlatformNames.Standalone_Windows;
#elif UNITY_STANDALONE_OSX
			return PlatformNames.Standalone_OSX;
#elif UNITY_STANDALONE_LINUX
			return PlatformNames.Standalone_Linux;
#elif UNITY_IOS
			return PlatformNames.Standalone_iOS;
#elif UNITY_ANDROID
			return PlatformNames.Standalone_Android;
#endif
#endif // UNITY_EDITOR

			return PlatformNames.Unknown;
		}

	    public static List<string> GetListOfFilesAndDirectoriesInDirectory(string RootDir, bool bFiles = true, bool bDirectories = true, bool bRecursive = false, bool bFilterOutUnitySpecialFiles = true, bool bAbsolutePath = false)
	    {
	    	List<string> FilesAndDirs = new List<string>();

	    	string[] Results = { };

	    	if(bFiles)
	    	{
				FilesAndDirs.AddRange(Directory.GetFiles(RootDir, "*", bRecursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly));
	    	}
	    	
	    	if(bDirectories)
	    	{
	    		FilesAndDirs.AddRange(Directory.GetDirectories(RootDir, "*", bRecursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly));
	    	}

	    	Results = FilesAndDirs.ToArray();

	    	FilesAndDirs.Clear();

			foreach(string CurrentFile in Results)
			{
				if(!bFilterOutUnitySpecialFiles || (!CurrentFile.EndsWith(".meta") && !CurrentFile.EndsWith("~") && !CurrentFile.EndsWith(".class")))
				{
					string RequestedPath = CurrentFile;

					if(!bAbsolutePath)
					{
						RequestedPath = RequestedPath.Replace(Path.GetFullPath(RootDir) + Path.DirectorySeparatorChar, "");
					}

					FilesAndDirs.Add(RequestedPath);
				}
			}

	    	return FilesAndDirs;
	    }
	}
}