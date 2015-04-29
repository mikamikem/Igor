using System;
using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;

namespace Igor
{
	public partial class IgorUtils
	{
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

			if(CurrentPath[0] == '~')
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

		public static int RunProcessCrossPlatform(string OSXCommand, string WindowsCommand, string Parameters, string Directory, ref string Output, ref string Error, bool bUseShell = false)
		{
			string Command = "";
			
#if UNITY_EDITOR_OSX
			Command = OSXCommand;
#else
			Command = WindowsCommand;
#endif // UNITY_EDITOR_OSX
			
			Command = UpdatePathForEnvVariables(Command);
			
			System.Diagnostics.ProcessStartInfo NewStartInfo = new System.Diagnostics.ProcessStartInfo();
//			NewStartInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
			
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

			NewStartInfo.WorkingDirectory = 
#if UNITY_EDITOR_OSX
			Directory
#else
			Directory.Replace("/", "\\")
#endif // UNITY_EDITOR_OSX
			;

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

	    public static float PlayJobsDoneSound()
	    {
	        AudioClip Clip = AssetDatabase.LoadAssetAtPath("Assets/Editor/Igor/Modules/Core/Core/jobs_done.wav", typeof(AudioClip)) as AudioClip;
	        if(Clip != null)
	        {
	            PlayAudioClip(Clip);
	            return Clip.length;
	        }
	        return 0f;
	    }

	    static void PlayAudioClip(AudioClip Clip)
	    {
	        if(Clip != null)
	        {
	            System.Reflection.Assembly UnityEditorAssembly = typeof(AudioImporter).Assembly;
	            if(UnityEditorAssembly != null)
	            {
	                System.Type AudioUtilClass = UnityEditorAssembly.GetType("UnityEditor.AudioUtil");
	                if(AudioUtilClass != null)
	                {
	                    System.Reflection.MethodInfo Method = AudioUtilClass.GetMethod(
	                        "PlayClip",
	                        System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public,
	                        null,
	                        new System.Type[]
	                        {
	                            typeof(AudioClip)
	                        },
	                        null
	                        );
	                    Method.Invoke(
	                        null,
	                        new object[]
	                        {
	                            Clip
	                        }
	                        );
	                }
	            }
	        }
	    }
	}
}