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
	public class IgorVersioning : IgorModuleBase
	{
        public static StepID AddVersioningStep = new StepID("AddVersioning", 350);
        
        static string IgorVersionTextPath = "Assets/Editor/Igor/Modules/Configure/AddVersioning/version.txt";
        static string DefaultPathForVersioningOutput = "Assets/Resources/version.txt";
		static string PathToVersioning = "path_for_versioning_output";

        int? CurrentMajorVersion;
        int? CurrentMinorVersion;
        int? CurrentBuildVersion;

		public override string GetModuleName()
		{
			return "Configure.AddVersioning";
		}

		public override void RegisterModule()
		{
			IgorCore.RegisterNewModule(this);
		}

		public override void ProcessArgs(IIgorStepHandler StepHandler)
		{
			if(IgorJobConfig.IsStringParamSet(PathToVersioning))
			{
				IgorCore.SetModuleActiveForJob(this);
				StepHandler.RegisterJobStep(AddVersioningStep, this, IncrementBuildVersion);
			}
		}

        bool IncrementBuildVersion()
        {
            CurrentMajorVersion = CurrentMinorVersion = CurrentBuildVersion = null;
            EnsureVersionInfoCached();
            UpdateVersion(CurrentMajorVersion.GetValueOrDefault(), CurrentMinorVersion.GetValueOrDefault(), CurrentBuildVersion.GetValueOrDefault() + 1, IgorVersionTextPath);
            UpdateVersion(CurrentMajorVersion.GetValueOrDefault(), CurrentMinorVersion.GetValueOrDefault(), CurrentBuildVersion.GetValueOrDefault(), IgorJobConfig.GetStringParam(PathToVersioning));
            return true;
        }

		public override string DrawJobInspectorAndGetEnabledParams(string CurrentParams)
		{
			string EnabledParams = CurrentParams;

            if(!IgorUtils.IsStringParamSet(EnabledParams, PathToVersioning))
            {
                EnabledParams = IgorUtils.SetStringParam(EnabledParams, PathToVersioning, DefaultPathForVersioningOutput);
            }
            
            DrawStringParam(ref EnabledParams, "Version Info Output Path", PathToVersioning);
            
            if(!string.IsNullOrEmpty(IgorUtils.GetStringParam(EnabledParams, PathToVersioning)))
            {
                EnsureVersionInfoCached();

                int MajorVersion = CurrentMajorVersion.GetValueOrDefault();
                int MinorVersion = CurrentMinorVersion.GetValueOrDefault();

                GUILayout.BeginHorizontal();
                {
                    int NewMajorVersion = EditorGUILayout.IntField("Major", CurrentMajorVersion.GetValueOrDefault(), GUILayout.ExpandWidth(false));
                    int NewMinorVersion = EditorGUILayout.IntField("Minor", CurrentMinorVersion.GetValueOrDefault(), GUILayout.ExpandWidth(false));
                    if(NewMajorVersion != MajorVersion || NewMinorVersion != MinorVersion)
                    {
                        UpdateVersion(NewMajorVersion, NewMinorVersion, CurrentBuildVersion.GetValueOrDefault(), IgorVersionTextPath);
                    }
                }
                GUILayout.EndHorizontal();
            }

			return EnabledParams;
		}

        void EnsureVersionInfoCached()
        {
            if(!File.Exists(IgorVersionTextPath))
            {
                UpdateVersion(0, 0, 1, IgorVersionTextPath);
                Debug.Log("No version file detected, creating new one.");
                CurrentMajorVersion = CurrentMinorVersion = CurrentBuildVersion = null;
            }

            if(CurrentMajorVersion == null || CurrentMinorVersion == null || CurrentBuildVersion == null)
            {            
                string VersionText = ReadTextFile(IgorVersionTextPath);
                if(VersionText != null)
                {
                    VersionText = VersionText.Trim();			
			    
                    string[] VersionComponents = VersionText.Split('.');
			        if(VersionComponents.Length == 3)
                    {
			            CurrentMajorVersion = int.Parse(VersionComponents[0]);
			            CurrentMinorVersion = int.Parse(VersionComponents[1]);
                        CurrentBuildVersion = int.Parse(VersionComponents[2]);
                    }
                }
            }
        }

        bool UpdateVersion(int NewMajor, int NewMinor, int NewBuild, string PathToWrite)
        {
            if(!string.IsNullOrEmpty(PathToWrite))
            {
                string VersionText = NewMajor.ToString("0") + "." + NewMinor.ToString("0") + "." + NewBuild.ToString("000");
                WriteTextFile(PathToWrite, VersionText);

                CurrentMajorVersion = NewMajor;
                CurrentMinorVersion = NewMinor;
                CurrentBuildVersion = NewBuild;
            }
            
            return false;
        }
	
        static string ReadTextFile(string FileName)
        {
            // Check to see if the filename specified exists, if not try adding '.txt', otherwise fail.
            string FoundFileName = "";
            if (File.Exists(FileName))
            {
                FoundFileName = FileName;
            }
            else if (File.Exists(FileName + ".txt"))
            {
                FoundFileName = FileName + ".txt";
            }
            else
            {
                return null;
            }
 
            StreamReader SteamReader;
            try
            {
                SteamReader = new StreamReader(FoundFileName);
            }
            catch(System.Exception e)
            {
                Debug.LogWarning("Something went wrong with read: " + e.Message);
                return null;
            }
 
            string FileContents = SteamReader.ReadToEnd();
            SteamReader.Close();
 
            return FileContents;
        }

	    static void WriteTextFile(string FileName, string FileContents)
	    {
		    if(File.Exists(FileName))
		    {
			    File.SetAttributes(FileName, FileAttributes.Normal);
			    File.Delete(FileName);
		    }

            string FullPath = Application.dataPath.Substring(0, Application.dataPath.LastIndexOf("Assets")) + FileName;
            if(!Directory.Exists(Path.GetDirectoryName(FullPath)))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(FullPath));
            }
		    File.WriteAllText(FileName, FileContents);

            AssetDatabase.Refresh();
	    }
	}
}