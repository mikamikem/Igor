using System;
using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using System.IO;

namespace Igor
{
	public partial class IgorUtils
	{
		public static string GetFirstLevelName()
		{
			foreach(EditorBuildSettingsScene CurrentScene in EditorBuildSettings.scenes)
			{
				if(CurrentScene.enabled)
				{
					return CurrentScene.path;
				}
			}

			return "";
		}

		public static string[] GetLevels()
		{
			List<string> LevelNames = new List<string>();
			
			foreach(EditorBuildSettingsScene CurrentScene in EditorBuildSettings.scenes)
			{
				if(CurrentScene.enabled)
				{
					LevelNames.Add(CurrentScene.path);
				}
			}
			
			return LevelNames.ToArray();
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

	    public static void ReplaceStringsInFile(IIgorModule ModuleInst, string FilePath, string OriginalString, string NewString)
	    {
	    	string FullFilePath = FilePath;

	    	if(!File.Exists(FullFilePath))
	    	{
	    		FullFilePath = Path.Combine(Path.GetFullPath("."), FilePath);
	    	}

	    	if(File.Exists(FullFilePath))
	    	{
			    File.SetAttributes(FullFilePath, System.IO.FileAttributes.Normal);
			}

	    	if(IgorAssert.EnsureTrue(ModuleInst, File.Exists(FullFilePath), "Replace string in file failed because " + FullFilePath + " doesn't exist."))
	    	{
				string FileContents = File.ReadAllText(FilePath);

				FileContents = FileContents.Replace(OriginalString, NewString);

				IgorRuntimeUtils.DeleteFile(FileContents);

				File.WriteAllText(FilePath, FileContents);
	    	}
	    }
	}
}