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
	public class IgorModuleBase : IIgorModule
	{
		public virtual string GetModuleName()
		{
			return "";
		}

		public virtual void RegisterModule()
		{
		}

		public virtual void ProcessArgs()
		{
		}

		public virtual string DrawJobInspectorAndGetEnabledParams(string CurrentParams)
		{
			return CurrentParams;
		}

		public virtual void DrawBoolParam(ref string CurrentParams, string BoolLabel, string BoolParam)
		{
			bool bIsEnabled = IgorUtils.IsBoolParamSet(CurrentParams, BoolParam);

			bIsEnabled = EditorGUILayout.Toggle(BoolLabel, bIsEnabled);

			CurrentParams = IgorUtils.SetBoolParam(CurrentParams, BoolParam, bIsEnabled);
		}

		public virtual void DrawStringParam(ref string CurrentParams, string StringLabel, string StringParam, List<string> ValidOptions)
		{
			DrawStringParam(ref CurrentParams, StringLabel, StringParam, ValidOptions.ToArray());
		}

		public virtual void DrawStringParam(ref string CurrentParams, string StringLabel, string StringParam, string[] ValidOptions)
		{
			string CurrentStringValue = IgorUtils.GetStringParam(CurrentParams, StringParam);

			if(CurrentStringValue == "")
			{
				CurrentStringValue = "Not set";
			}

			List<string> AllOptions = new List<string>();

			AllOptions.Add("Not set");
			AllOptions.AddRange(ValidOptions);

			int ChosenIndex = -1;

			for(int CurrentIndex = 0; CurrentIndex < AllOptions.Count; ++CurrentIndex)
			{
				if(AllOptions[CurrentIndex] == CurrentStringValue)
				{
					ChosenIndex = CurrentIndex;
				}
			}

			ChosenIndex = EditorGUILayout.Popup(StringLabel, ChosenIndex, AllOptions.ToArray());

			CurrentStringValue = AllOptions[ChosenIndex];

			if(CurrentStringValue == "Not set")
			{
				CurrentStringValue = "";
			}

			CurrentParams = IgorUtils.SetStringParam(CurrentParams, StringParam, CurrentStringValue);
		}

		public virtual void Log(string Message)
		{
			IgorCore.Log(this, Message);
		}

		public virtual void LogWarning(string Message)
		{
			IgorCore.LogWarning(this, Message);
		}

		public virtual void LogError(string Message)
		{
			IgorCore.LogError(this, Message);
		}

		public virtual void CriticalError(string Message)
		{
			IgorCore.CriticalError(this, Message);
		}

		public virtual bool GetConfigBool(string BoolKey, bool bDefaultValue = false)
		{
			return IgorConfig.GetModuleBool(this, BoolKey, bDefaultValue);
		}

		public virtual void SetConfigBool(string BoolKey, bool bValue)
		{
			IgorConfig.SetModuleBool(this, BoolKey, bValue);
		}

		public virtual string GetConfigString(string StringKey, string DefaultValue = "")
		{
			return IgorConfig.GetModuleString(this, StringKey, DefaultValue);
		}

		public virtual void SetConfigString(string StringKey, string Value)
		{
			IgorConfig.SetModuleString(this, StringKey, Value);
		}
	}
}