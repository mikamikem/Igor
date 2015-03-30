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

		protected static string ProductsFlag = "buildproducts";

		public static StepID BuildStep = new StepID("Build", 500);
		public static StepID SwitchPlatformStep = new StepID("SwitchPlatform", 250);
		public static StepID PreBuildCleanupStep = new StepID("PreBuildCleanup", 0);

		public delegate BuildOptions GetExtraBuildOptions(BuildTarget CurrentTarget);

		protected static List<string> CurrentBuildProducts = new List<string>();

		public override string GetModuleName()
		{
			return "Build.Common";
		}

		public override void RegisterModule()
		{
			IgorCore.RegisterNewModule(this);
		}

		public override void ProcessArgs(IIgorStepHandler StepHandler)
		{
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
	}
}