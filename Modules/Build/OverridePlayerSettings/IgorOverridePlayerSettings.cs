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
	public class IgorOverridePlayerSettings : IgorModuleBase
	{
	    static string kPlayerSettingsFolder = "Assets/Igor Job Player Settings";

        public static string OverridePlayerSettingsFlag = "override_player_settings";
		public static string OverridePlayerSettingsFlagFilenameFlag = "player_settings_file";

		public override string GetModuleName()
		{
			return "Build.OverridePlayerSettings";
		}

		public override void RegisterModule()
		{
			IgorCore.RegisterNewModule(this);
		}

		public override void ProcessArgs(IIgorStepHandler StepHandler)
		{
			if(IgorJobConfig.IsBoolParamSet(OverridePlayerSettingsFlag) && IgorJobConfig.GetStringParam(OverridePlayerSettingsFlagFilenameFlag) != string.Empty)
			{
				IgorCore.SetModuleActiveForJob(this);
				StepHandler.RegisterJobStep(IgorBuildCommon.OverridePlayerSettings, this, OverridePlayerSettings);
			}
		}

		public override string DrawJobInspectorAndGetEnabledParams(string CurrentParams)
		{
			string EnabledParams = CurrentParams;

            IgorConfigWindow ConfigurationWindow = IgorConfigWindow.OpenOrGetConfigWindow();
            IgorPersistentJobConfig CurrentJob = ConfigurationWindow.CurrentJobInst;
		    string CurrentJobAsString = CurrentJob != null ? CurrentJob.JobName : string.Empty;
            string TargetDirectory = kPlayerSettingsFolder + "/" + CurrentJobAsString;
		    
            GUILayout.BeginHorizontal();
		    {
		        bool bOverridePlayerSettings = DrawBoolParam(ref EnabledParams, "Override", OverridePlayerSettingsFlag);
                if(bOverridePlayerSettings)
                {
                    EnabledParams = IgorUtils.SetStringParam(EnabledParams, OverridePlayerSettingsFlagFilenameFlag, '"' + TargetDirectory + '"');
                }
                else
                {
                    EnabledParams = IgorUtils.ClearParam(EnabledParams, OverridePlayerSettingsFlagFilenameFlag);
                }

		        GUILayout.Label(TargetDirectory);
		    }
            GUILayout.EndHorizontal();

		    GUILayout.BeginHorizontal();
		    {
		        GUI.enabled = CurrentJob != null;
		        if(GUILayout.Button("Save"))
		        {
		            if(!Directory.Exists(kPlayerSettingsFolder))
		            {
		                Directory.CreateDirectory(kPlayerSettingsFolder);
		            }

		            IgorUtils.DeleteDirectory(TargetDirectory);
		            Directory.CreateDirectory(TargetDirectory);

		            string[] SourceFilesPaths = Directory.GetFiles("ProjectSettings");
		            foreach(string SourceFilePath in SourceFilesPaths)
		            {
		                if(!SourceFilePath.EndsWith(".meta"))
		                {
		                    string DestFilePath = SourceFilePath.Replace("ProjectSettings\\", string.Empty);
		                    DestFilePath = TargetDirectory + "/" + Path.ChangeExtension(DestFilePath, ".igorplayersettings");
		                    File.Copy(SourceFilePath, DestFilePath);
		                }
		            }

		            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
		        }

		        string Tooltip = Directory.Exists(TargetDirectory) ? string.Empty : "Expected PlayerSettings directory " + " doesn't exist.";

		        GUI.enabled &= Directory.Exists(TargetDirectory);
		        if(GUILayout.Button(new GUIContent("Load saved settings file", Tooltip)))
		        {
		            CopyStoredPlayerSettingsOverCurrent(TargetDirectory);
		        }

		        GUI.enabled = true;
		    }
            GUILayout.EndHorizontal();

			return EnabledParams;
		}

	    private static void CopyStoredPlayerSettingsOverCurrent(string TargetDirectory)
	    {
	        string[] SourceFilesPaths = Directory.GetFiles(TargetDirectory);
		    foreach(string SourceFilePath in SourceFilesPaths)
		    {
		        string DestFilePath = SourceFilePath.Replace(TargetDirectory, "ProjectSettings");
		        DestFilePath = Path.ChangeExtension(DestFilePath, ".asset");
		        File.Copy(SourceFilePath, DestFilePath, true);

                // We need to find the ProjectSettings file and locate the defines text manually because otherwise
                // the recompile (if it even triggers; it's inconsistent) won't use the new defines.
		        const string ScriptingDefineSymbolsTag = "scriptingDefineSymbols:\n";
		        if(DestFilePath.Contains("ProjectSettings.asset"))
		        {
		            string ProjectSettingsText = File.ReadAllText(SourceFilePath);
		            int StartIndex = ProjectSettingsText.IndexOf(ScriptingDefineSymbolsTag) + ScriptingDefineSymbolsTag.Length;
                    string StartOfDefinesBlock = ProjectSettingsText.Substring(StartIndex);

		            HashSet<BuildTargetGroup> MatchedBuildTargetGroups = new HashSet<BuildTargetGroup>();

		            string NextLine;
                    StringReader StringReader = new StringReader(StartOfDefinesBlock);
		            bool bContinue = true;
		            do
		            {
		                NextLine = StringReader.ReadLine();
		                if(NextLine != null)
		                {
		                    NextLine = NextLine.Trim();
		                    if(NextLine.Length > 0 && char.IsNumber(NextLine[0]))
		                    {
		                        int IndexOfColon = NextLine.IndexOf(':');
		                        string BuildGroupText = NextLine.Substring(0, IndexOfColon);
		                        string Define = NextLine.Substring(IndexOfColon + 1);

		                        int BuildGroupAsInt = 0;
                                Int32.TryParse(BuildGroupText, out BuildGroupAsInt);
		                        BuildTargetGroup TargetGroup = (BuildTargetGroup)BuildGroupAsInt;

		                        if(TargetGroup != BuildTargetGroup.Unknown)
		                        {
		                            PlayerSettings.SetScriptingDefineSymbolsForGroup(TargetGroup, Define);
		                            MatchedBuildTargetGroups.Add(TargetGroup);
		                        }
		                    }
                            else
		                    {
		                        bContinue = false;
		                    }
		                }
		            }
		            while(bContinue);

                    // Make sure we wipe out defines on any other build targets.
		            BuildTargetGroup[] AllTargetGroups = System.Enum.GetValues(typeof(BuildTargetGroup)) as BuildTargetGroup[];
		            foreach(BuildTargetGroup Group in AllTargetGroups)
		            {
		                if(!MatchedBuildTargetGroups.Contains(Group))
		                {
		                    PlayerSettings.SetScriptingDefineSymbolsForGroup(Group, string.Empty);
		                }
		            }
		        }
		    }

            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
	    }

	    public virtual bool OverridePlayerSettings()
	    {
	        string TargetDirectory = IgorJobConfig.GetStringParam(OverridePlayerSettingsFlagFilenameFlag);
	        TargetDirectory = TargetDirectory.Replace("\"", string.Empty);
	        string LogDetails = "Overriding default player settings with settings from directory " + TargetDirectory;
	        Log(LogDetails);

	        CopyStoredPlayerSettingsOverCurrent(TargetDirectory);
	        return true;
	    }
	}
}