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
	public class IgorGenerateBuildInformation : IgorModuleBase
	{
        const string GenerateBuildInformationFlag = "generatebuildinformation";
        const string ModuleDirectory = "Assets/Igor/Modules/Configure/GenerateBuildInformation";
        const string FileName = ModuleDirectory + "/Resources/IgorBuildInformation_Text.txt";

        static Dictionary<string, string> _buildInformationDictionary;
        static Dictionary<string, string> BuildInformationDictionary
        {
            get
            {
                if(_buildInformationDictionary == null)
                {
                    _buildInformationDictionary = new Dictionary<string,string>();

                    string[] KVPs = ReadTextFile();
                    if(KVPs != null)
                    {
                        foreach(string KVP in KVPs)
                        {
                            string[] KeyAndValue = KVP.Split('=');
                            if(KeyAndValue.Length == 2)
                            {
                                _buildInformationDictionary.Add(KeyAndValue[0], KeyAndValue[1]);
                            }
                        }
                    }
                }
                return _buildInformationDictionary;
            }
        }

        public static StepID GenerateBuildInformationStep = new StepID("GenerateBuildInformation", 350);

        int? CurrentMajorVersion;
        int? CurrentMinorVersion;
        int? CurrentBuildVersion;

		public override string GetModuleName()
		{
			return "Configure.GenerateBuildInformation";
		}

		public override void RegisterModule()
		{
			IgorCore.RegisterNewModule(this);
		}

		public override void ProcessArgs(IIgorStepHandler StepHandler)
		{
			if(IgorJobConfig.IsBoolParamSet(GenerateBuildInformationFlag))
			{
				IgorCore.SetModuleActiveForJob(this);
				StepHandler.RegisterJobStep(GenerateBuildInformationStep, this, PushDataToTextFile);
			}
		}

        bool PushDataToTextFile()
        {
            // Force flush.
            _buildInformationDictionary = null;
            
            // Version.
            CurrentMajorVersion = CurrentMinorVersion = CurrentBuildVersion = null;
            EnsureVersionInfoCached();
            UpdateVersion(CurrentMajorVersion.GetValueOrDefault(), CurrentMinorVersion.GetValueOrDefault(), CurrentBuildVersion.GetValueOrDefault() + 1);

            // Git info.
            GetEnvironmentVariable("GIT_COMMIT");
            GetEnvironmentVariable("GIT_TAG");
            GetEnvironmentVariable("GIT_TAG_MESSAGE");
            GetEnvironmentVariable("GIT_BRANCH");
            GetEnvironmentVariable("GIT_AUTHOR_NAME");

            // System time.
            SetKeyValue("JOB_TIME", System.DateTime.Now.ToString());
            
            SaveTextFile();
            AssetDatabase.SaveAssets();

            return true;
        }

        static void GetEnvironmentVariable(string inEnvironmentVariableName, bool inFlushToDisk = false)
        {
            string Value = System.Environment.GetEnvironmentVariable(inEnvironmentVariableName);
            SetKeyValue(inEnvironmentVariableName, Value, inFlushToDisk);
        }

		public override string DrawJobInspectorAndGetEnabledParams(string CurrentParams)
		{
			string EnabledParams = CurrentParams;
            
            bool IsEnabled = DrawBoolParam(ref EnabledParams, "Generate build information", GenerateBuildInformationFlag);
            if(IsEnabled)
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
                        UpdateVersion(NewMajorVersion, NewMinorVersion, CurrentBuildVersion.GetValueOrDefault());
                    }
                }
                GUILayout.EndHorizontal();
            }

			return EnabledParams;
		}

        void EnsureVersionInfoCached()
        {
            if(string.IsNullOrEmpty(GetValue("VERSION")))
            {
                UpdateVersion(0, 0, 1);
                CurrentMajorVersion = CurrentMinorVersion = CurrentBuildVersion = null;
            }

            if(CurrentMajorVersion == null || CurrentMinorVersion == null || CurrentBuildVersion == null)
            {            
                string VersionText = GetValue("VERSION");
                if(!string.IsNullOrEmpty(VersionText))
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

        void UpdateVersion(int NewMajor, int NewMinor, int NewBuild)
        {
            string VersionText = NewMajor.ToString("0") + "." + NewMinor.ToString("0") + "." + NewBuild.ToString("000");
            SetKeyValue("VERSION", VersionText, true);

            CurrentMajorVersion = NewMajor;
            CurrentMinorVersion = NewMinor;
            CurrentBuildVersion = NewBuild;
        }

        static void SetKeyValue(string inKey, string inValue, bool inFlushToDisk = false)
        {
            if(BuildInformationDictionary.ContainsKey(inKey))
                BuildInformationDictionary[inKey] = inValue;
            else
                BuildInformationDictionary.Add(inKey, inValue);

            if(inFlushToDisk)
                SaveTextFile();
        }

        static string GetValue(string inKey)
        {
            if(BuildInformationDictionary.ContainsKey(inKey))
                return BuildInformationDictionary[inKey];
            return "";
        }

        static void SaveTextFile()
        {
            string FileContents = "";

            foreach(var kvp in BuildInformationDictionary)
            {
                string LineContent = kvp.Key + "=" + kvp.Value;
                if(!string.IsNullOrEmpty(FileContents))
                    FileContents += "\n";
                FileContents += LineContent;
            }

            WriteTextFile(FileContents);
        }
        
        static void WriteTextFile(string FileContents)
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

        static string[] ReadTextFile()
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
 
            string[] FileContents = SteamReader.ReadToEnd().Split('\n');
            SteamReader.Close();
 
            return FileContents;
        }
	}
}