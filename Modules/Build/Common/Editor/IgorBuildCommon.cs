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
	public class IgorBuildCommon : IgorModuleBase
	{
		public static string BuildFlag = "build";
		public static string PlatformFlag = "platform";
		public static string BuiltNameFlag = "builtname";
        public static string AppendCommitInfoFlag = "appendcommitinfo";
        public static string BuildOptionsFlag = "buildoptions";

		protected static string ProductsFlag = "buildproducts";

		public static StepID BuildStep = new StepID("Build", 500);
        public static StepID OverridePlayerSettings = new StepID("OverridePlayerSettings", 275);
		public static StepID SwitchPlatformStep = new StepID("SwitchPlatform", 250);
		public static StepID PreBuildCleanupStep = new StepID("PreBuildCleanup", 0);

        public delegate BuildOptions GetExtraBuildOptions(BuildTarget CurrentTarget);
        public static string CommitInfo = string.Empty;

        public static int SetBuildOptionsBitfield = 0;

        static List<BuildOptions> _buildOptionValues = null;
        static List<BuildOptions> BuildOptionValues
        {
            get
            {
                if(_buildOptionValues == null)
                {
                    _buildOptionValues = new List<BuildOptions>();

                    var values = System.Enum.GetValues(typeof(BuildOptions));
                    foreach(BuildOptions value in values)
                    {
                        _buildOptionValues.Add(value);
                    }
                }
                return _buildOptionValues;
            }
        }

        static string[] _buildOptionNames = null;
        static string[] BuildOptionNames
        {
            get
            {
                if(_buildOptionNames == null)
                    _buildOptionNames = System.Enum.GetNames(typeof(BuildOptions));
                return _buildOptionNames;
            }
        }

		protected static List<string> CurrentBuildProducts = new List<string>();

		public static List<string> AvailablePlatforms = new List<string>();

		public override string GetModuleName()
		{
			return "Build.Common";
		}

		public override void RegisterModule()
		{
			bool DidRegister = IgorCore.RegisterNewModule(this);
            if(DidRegister)
            {
                AvailablePlatforms.Clear();
            }
		}

		public override void ProcessArgs(IIgorStepHandler StepHandler)
		{
			IgorCore.SetModuleActiveForJob(this);

            if(IgorJobConfig.IsBoolParamSet(IgorBuildCommon.BuildFlag))
			{
                if(IgorJobConfig.IsStringParamSet(IgorBuildCommon.AppendCommitInfoFlag))
                {
                    CommitInfo = IgorJobConfig.GetStringParam(IgorBuildCommon.AppendCommitInfoFlag);
                }

                if(IgorJobConfig.IsStringParamSet(IgorBuildCommon.BuildOptionsFlag))
                {
                    int OutResult = 0;
                    if(Int32.TryParse(IgorJobConfig.GetStringParam(IgorBuildCommon.BuildOptionsFlag).Trim('"'), out OutResult))
                    {
                        SetBuildOptionsBitfield = OutResult;
                    }
                }
            }
		}

		public static void RegisterBuildPlatforms(string[] Platforms)
		{
            foreach(string Platform in Platforms)
            {
                if(!AvailablePlatforms.Contains(Platform))
                {
                    AvailablePlatforms.Add(Platform);
                }
            }
		}

		public override string DrawJobInspectorAndGetEnabledParams(string CurrentParams)
		{
			string EnabledParams = CurrentParams;

			DrawBoolParam(ref EnabledParams, "Build the game", BuildFlag);
			DrawStringOptionsParam(ref EnabledParams, "Platform to build", PlatformFlag, AvailablePlatforms);

            DrawBoolParam(ref EnabledParams, "Append Commit Info", IgorBuildCommon.AppendCommitInfoFlag);
                
            string BuildOptionsAsString = IgorUtils.GetStringParam(EnabledParams, IgorBuildCommon.BuildOptionsFlag).Trim('"');
            if(!string.IsNullOrEmpty(BuildOptionsAsString))
            {
                int OutResult = 0;
                if(Int32.TryParse(BuildOptionsAsString, out OutResult))
                {
                    SetBuildOptionsBitfield = OutResult;
                }
            }

            int newValue = EditorGUILayout.MaskField(SetBuildOptionsBitfield, BuildOptionNames);
            if(newValue != SetBuildOptionsBitfield)
            {
                SetBuildOptionsBitfield = newValue;

                if(SetBuildOptionsBitfield != 0)
                    EnabledParams = IgorUtils.SetStringParam(EnabledParams, IgorBuildCommon.BuildOptionsFlag, ((int)SetBuildOptionsBitfield).ToString());
                else
                    EnabledParams = IgorUtils.ClearParam(EnabledParams, IgorBuildCommon.BuildOptionsFlag);
            }

			return EnabledParams;
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

		public static void SetNewBuildProducts(List<string> NewBuildProducts)
		{
			CurrentBuildProducts.Clear();
			CurrentBuildProducts.AddRange(NewBuildProducts);

			string CombinedProducts = "";

			foreach(string CurrentProduct in NewBuildProducts)
			{
				CombinedProducts += (CombinedProducts.Length > 0 ? "," : "") + CurrentProduct;
			}

			IgorJobConfig.SetStringParam(ProductsFlag, CombinedProducts);
		}

		public static List<string> GetBuildProducts()
		{
			if(CurrentBuildProducts.Count == 0)
			{
				string CombinedProducts = IgorJobConfig.GetStringParam(ProductsFlag);

				CurrentBuildProducts.Clear();
				CurrentBuildProducts.AddRange(CombinedProducts.Split(','));
			}

			return CurrentBuildProducts;
		}

        public static BuildOptions GetBuildOptions()
        {
            int BuildOptionsBitfield = 0;
            for(int i = 0; i < BuildOptionValues.Count; ++i)
            {
                if((SetBuildOptionsBitfield & (1 << i)) != 0)
                {
                    BuildOptionsBitfield |= (int)BuildOptionValues[i];
                }
            }
            return (BuildOptions)BuildOptionsBitfield;
        }
    }
}