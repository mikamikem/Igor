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
	public class IgorLightingTimeOfDay : IgorModuleBase
	{
		public static string SetLightmapsFlag = "setlightmaps";

		public static StepID SetLightmapsStep = new StepID("Set Lightmaps", 300);

		public override string GetModuleName()
		{
			return "Configure.LightingTimeOfDay";
		}

		public override void RegisterModule()
		{
			bool DidRegister = IgorCore.RegisterNewModule(this);
		}

		public override void ProcessArgs(IIgorStepHandler StepHandler)
		{
			if(IgorJobConfig.IsBoolParamSet(SetLightmapsFlag))
			{
				IgorCore.SetModuleActiveForJob(this);

				StepHandler.RegisterJobStep(SetLightmapsStep, this, SetLightmaps);

				CurrentLevelIndex = 0;
			}
		}

        public override string DrawJobInspectorAndGetEnabledParams(string CurrentParams)
		{
			string EnabledParams = CurrentParams;

			DrawBoolParam(ref EnabledParams, "Set Lightmaps Before Build", SetLightmapsFlag);

			return EnabledParams;
		}

		public int CurrentLevelIndex = 0;
		public string PendingLevelLoad = "";

		public virtual bool SetLightmaps()
		{
			EditorBuildSettingsScene CurrentScene = null;

			int EnabledIndex = 0;

			for(int CurrentIndex = 0; CurrentIndex < EditorBuildSettings.scenes.Count(); ++CurrentIndex)
			{
				if(EditorBuildSettings.scenes[CurrentIndex].enabled)
				{
					if(EnabledIndex == CurrentLevelIndex)
					{
						CurrentScene = EditorBuildSettings.scenes[CurrentIndex];
					}

					++EnabledIndex;
				}
			}


			if(CurrentScene != null)
			{
				EditorApplication.OpenScene(CurrentScene.path);

				PendingLevelLoad = CurrentScene.path;
			}

			bool bDone = PendingLevelLoad == "";

			if(EditorApplication.currentScene == PendingLevelLoad)
			{
				SceneInfoManager InfoManagerInst = UnityEngine.Object.FindObjectOfType<SceneInfoManager>();

				if(InfoManagerInst != null)
				{
					InfoManagerInst.LoadBuildLightmaps();

					EditorApplication.SaveScene();
				}

				++CurrentLevelIndex;

				PendingLevelLoad = "";
			}

			return bDone;
		}
	}
}