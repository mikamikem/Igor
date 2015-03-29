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
	public class IgorAmplifyTexture : IgorModuleBase
	{
		public static string UpdateAmplifyFlag = "updateamplify";
		public static string RebuildAmplifyFlag = "rebuildamplify";

		public static StepID BuildAmplifyStep = new StepID("BuildAmplify", 100);

		public override string GetModuleName()
		{
			return "3rdParty.AmplifyTexture";
		}

		public override void RegisterModule()
		{
			IgorCore.RegisterNewModule(this);
		}

		public override void ProcessArgs(IIgorStepHandler StepHandler)
		{
			if(IgorJobConfig.IsBoolParamSet(UpdateAmplifyFlag))
			{
				IgorCore.SetModuleActiveForJob(this);

				StepHandler.RegisterJobStep(BuildAmplifyStep, this, UpdateAmplify);
			}
			else if(IgorJobConfig.IsBoolParamSet(RebuildAmplifyFlag))
			{
				IgorCore.SetModuleActiveForJob(this);

				StepHandler.RegisterJobStep(BuildAmplifyStep, this, RebuildAmplify);
			}
		}

		public override string DrawJobInspectorAndGetEnabledParams(string CurrentParams)
		{
			string EnabledParams = CurrentParams;

			DrawBoolParam(ref EnabledParams, "Update Amplify Texture Cache", UpdateAmplifyFlag);
			DrawBoolParam(ref EnabledParams, "Rebuild Amplify Texture Cache", RebuildAmplifyFlag);

			return EnabledParams;
		}

		public virtual string GetFirstLevelName()
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

		public virtual bool UpdateAmplify()
		{
			string FirstLevelName = GetFirstLevelName();

			if(FirstLevelName != "")
			{
				if(EditorApplication.currentScene != FirstLevelName)
				{
					Log("Opening scene " + FirstLevelName);

					EditorApplication.OpenScene(FirstLevelName);

					return false;
				}
			}

			Log("Triggering update of Amplify Texture VTs.");

			TriggerBuildAllVTsInScene(AmplifyTexture.InternalEditor.Instance);

			return true;
		}

		public virtual bool RebuildAmplify()
		{
			string FirstLevelName = GetFirstLevelName();

			if(FirstLevelName != "")
			{
				if(EditorApplication.currentScene != FirstLevelName)
				{
					Log("Opening scene " + FirstLevelName);

					EditorApplication.OpenScene(FirstLevelName);

					return false;
				}
			}

			Log("Triggering rebuild of Amplify Texture VTs.");

			TriggerRebuildAllVTsInScene(AmplifyTexture.InternalEditor.Instance);

			return true;
		}

		public static T GetPrivateField<T>(object Instance, string FieldName)
		{
		    BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic;
		    Type InstType = Instance.GetType();
		    FieldInfo Field = InstType.GetField(FieldName, Flags);

		    return (T)Field.GetValue(Instance);
		}

		public static void SetPrivateField(object Instance, string FieldName, object Value)
		{
		    BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic;
		    Type InstType = Instance.GetType();
		    FieldInfo Field = InstType.GetField(FieldName, Flags);

		    Field.SetValue(Instance, Value);
		}

		public static T CallPrivateMethod<T>(object Instance, string MethodName, params object[] Params)
		{
		    BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic;
		    Type InstType = Instance.GetType();
		    MethodInfo MethodInst = InstType.GetMethod(MethodName, Flags);

		    return (T)MethodInst.Invoke(Instance, Params);
		}

		public static void CallPrivateVoidMethod(object Instance, string MethodName, params object[] Params)
		{
		    BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic;
		    Type InstType = Instance.GetType();
		    MethodInfo MethodInst = InstType.GetMethod(MethodName, Flags);

		    MethodInst.Invoke(Instance, Params);
		}

		public static void TriggerBuildAllVTsInScene(AmplifyTexture.InternalEditor AmplifyInst)
		{
			if ( AmplifyTextureManagerBase.Instance != null )
			{
				// TODO: use a different update frequency for this
				VirtualTextureBase[] uniqueVirtualTextures = AmplifyTextureManagerBase.Instance.VirtualTextures.Distinct().ToArray();

				foreach ( VirtualTextureBase asset in uniqueVirtualTextures )
				{
					GetPrivateField<HashSet<VirtualTextureBase>>(AmplifyInst, "m_scheduledBuildAssets").Add(asset);
				}

				SetPrivateField(AmplifyInst, "m_scheduledBuild", true);
				SetPrivateField(AmplifyInst, "m_scheduledBuildTime", 0.0f);

				CallPrivateVoidMethod(AmplifyInst, "OnEditorUpdate");
			}
		}

		public static void TriggerRebuildAllVTsInScene(AmplifyTexture.InternalEditor AmplifyInst)
		{
			if ( AmplifyTextureManagerBase.Instance != null )
			{
				// TODO: use a different update frequency for this
				VirtualTextureBase[] uniqueVirtualTextures = AmplifyTextureManagerBase.Instance.VirtualTextures.Distinct().ToArray();
				
				foreach ( VirtualTextureBase asset in uniqueVirtualTextures )
				{
					asset.UpdateProperties();
					asset.RequestRebuild();
					
					AmplifyInst.BuildCheck(asset);
				}
			}
		}
	}
}
