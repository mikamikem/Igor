#if IGOR_RUNTIME || UNITY_EDITOR
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif // UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.IO;
using System;
using System.Reflection;
using System.Threading;
using System.Xml.Serialization;

namespace Igor
{
	public class IgorCoreModule : IgorModuleBase
	{
		public static string SkipUnityUpdateFlag = "nounityupdate";
		public static string MinUnityVersionFlag = "minunityversion";
		public static string MaxUnityVersionFlag = "maxunityversion";

		public override string GetModuleName()
		{
			return "Core.Core";
		}

		public override void RegisterModule()
		{
			IgorCore.RegisterNewModule(this);
		}

#if UNITY_EDITOR
		public override string DrawJobInspectorAndGetEnabledParams(string CurrentParams)
		{
			string EnabledParams = CurrentParams;

			DrawBoolParam(ref EnabledParams, "Disable Igor updating when running as a job", SkipUnityUpdateFlag);

			DrawVersionInspector(ref EnabledParams);

			return EnabledParams;
		}

		public virtual void DrawVersionInspector(ref string CurrentParams)
		{
			string MinVersionParam = "";
			string MaxVersionParam = "";

			if(IgorRuntimeUtils.IsStringParamSet(CurrentParams, MinUnityVersionFlag))
			{
				MinVersionParam = IgorRuntimeUtils.GetStringParam(CurrentParams, MinUnityVersionFlag);
			}
			else
			{
				MinVersionParam = GetConfigString(MinUnityVersionFlag);
			}

			if(IgorRuntimeUtils.IsStringParamSet(CurrentParams, MaxUnityVersionFlag))
			{
				MaxVersionParam = IgorRuntimeUtils.GetStringParam(CurrentParams, MaxUnityVersionFlag);
			}
			else
			{
				MaxVersionParam = GetConfigString(MaxUnityVersionFlag);
			}

			UnityVersionInfo MinVersion = UnityVersionInfo.FromString(MinVersionParam);
			UnityVersionInfo MaxVersion = UnityVersionInfo.FromString(MaxVersionParam);

			EditorGUILayout.BeginVertical("box");

			EditorGUILayout.BeginHorizontal();

			EditorGUILayout.LabelField("Minimum Unity version");

			EditorGUILayout.BeginHorizontal();

			MinVersion.Platform = MaxVersion.Platform = DrawPlatformDropDown(MinVersion.Platform);

			MinVersion.MajorVersion = MaxVersion.MajorVersion = DrawMajorVersionDropDown(MinVersion.MajorVersion, -2);

			MinVersion.MinorVersion = DrawMinorVersionDropDown(MinVersion.MajorVersion, MinVersion.MinorVersion, MinVersion.MajorVersion, -2);

			MinVersion.ReleaseVersion = DrawReleaseVersionDropDown(MinVersion.MinorVersion, MinVersion.ReleaseVersion, MinVersion.MajorVersion, MinVersion.MinorVersion, -2);

			MinVersion.PatchVersion = DrawPatchVersionDropDown(MinVersion.ReleaseVersion, MinVersion.PatchVersion, MinVersion.MajorVersion, MinVersion.MinorVersion, MinVersion.ReleaseVersion, -2);

			EditorGUILayout.EndHorizontal();

			EditorGUILayout.EndHorizontal();

			EditorGUILayout.BeginHorizontal();

			DrawStringConfigParamUseValue(ref CurrentParams, "", MinUnityVersionFlag, MinVersion.ToString());

			EditorGUILayout.EndHorizontal();

			EditorGUILayout.EndVertical();

			EditorGUILayout.BeginVertical("box");

			EditorGUILayout.BeginHorizontal();

			EditorGUILayout.LabelField("Maximum Unity version");

			EditorGUILayout.BeginHorizontal();

			string PlatformName = MinVersion.Platform;

			if(PlatformName == "")
			{
				PlatformName = "PC";
			}

			EditorGUILayout.LabelField(PlatformName, GUILayout.MaxWidth(100.0f));

//			MaxVersion.MajorVersion = DrawMajorVersionDropDown(MaxVersion.MajorVersion, MinVersion.MajorVersion);

			string MaxMajorVersionNumString = MaxVersion.MajorVersion.ToString();

			if(MaxVersion.MajorVersion == -1)
			{
				MaxMajorVersionNumString = "Latest";
			}

			EditorGUILayout.LabelField(MaxMajorVersionNumString, GUILayout.MaxWidth(65.0f));

			MaxVersion.MinorVersion = DrawMinorVersionDropDown(MaxVersion.MajorVersion, MaxVersion.MinorVersion, MinVersion.MajorVersion, MinVersion.MinorVersion);

			MaxVersion.ReleaseVersion = DrawReleaseVersionDropDown(MaxVersion.MinorVersion, MaxVersion.ReleaseVersion, MinVersion.MajorVersion, MinVersion.MinorVersion, MinVersion.ReleaseVersion);

			MaxVersion.PatchVersion = DrawPatchVersionDropDown(MaxVersion.ReleaseVersion, MaxVersion.PatchVersion, MinVersion.MajorVersion, MinVersion.MinorVersion, MinVersion.ReleaseVersion, MinVersion.PatchVersion);

			EditorGUILayout.EndHorizontal();

			EditorGUILayout.EndHorizontal();

			EditorGUILayout.BeginHorizontal();

			DrawStringConfigParamUseValue(ref CurrentParams, "", MaxUnityVersionFlag, MaxVersion.ToString());

			EditorGUILayout.EndHorizontal();

			EditorGUILayout.EndVertical();
		}

		public virtual string DrawPlatformDropDown(string CurrentVersion)
		{
			string[] PlatformOptions = { "PC", "PS4", "PSVITA", "XBOXONE" };
			int SelectedOption = 0;

			for(int CurrentPlatformIndex = 1; CurrentPlatformIndex < PlatformOptions.Length; ++CurrentPlatformIndex)
			{
				if(PlatformOptions[CurrentPlatformIndex] == CurrentVersion)
				{
					SelectedOption = CurrentPlatformIndex;

					break;
				}
			}

			SelectedOption = EditorGUILayout.Popup(SelectedOption, PlatformOptions, GUILayout.MaxWidth(100.0f));

			if(SelectedOption >= 0 && SelectedOption < PlatformOptions.Length)
			{
				string PlatformName = PlatformOptions[SelectedOption];

				return PlatformName.Replace("PC", "");
			}

			return "";
		}

		public virtual int DrawMajorVersionDropDown(int CurrentMajorVersion, int MinMajorVersion)
		{
			int[] AllValidVersionOptions = { 5, 4 };

			List<string> VersionOptions = new List<string>();
			int SelectedOption = 0;

			VersionOptions.Add("Latest");

			if(MinMajorVersion != -1)
			{
				int CurrentVersionOptionIndex = 1;

				foreach(int CurrentVersionValue in AllValidVersionOptions)
				{
					if(CurrentVersionValue >= MinMajorVersion)
					{
						if(CurrentVersionValue == CurrentMajorVersion)
						{
							SelectedOption = CurrentVersionOptionIndex;
						}

						VersionOptions.Add(CurrentVersionValue.ToString());

						++CurrentVersionOptionIndex;
					}
				}
			}

			if(VersionOptions.Count == 1)
			{
				EditorGUILayout.LabelField(VersionOptions[0], GUILayout.MaxWidth(65.0f));
			}
			else
			{
				SelectedOption = EditorGUILayout.Popup(SelectedOption, VersionOptions.ToArray(), GUILayout.MaxWidth(65.0f));
			}

			if(SelectedOption >= 0 && SelectedOption < VersionOptions.Count)
			{
				string VersionString = VersionOptions[SelectedOption];

				if(VersionString == "Latest")
				{
					return -1;
				}

				int TestInt = -1;

				if(int.TryParse(VersionString, out TestInt))
				{
					return TestInt;
				}
			}

			return -1;
		}

		public virtual int DrawMinorVersionDropDown(int CurrentMajorVersion, int CurrentMinorVersion, int MinMajorVersion, int MinMinorVersion)
		{
			int[] AllValidVersionOptions = { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 };

			List<string> VersionOptions = new List<string>();
			int SelectedOption = 0;

			VersionOptions.Add("Latest");

			if(MinMinorVersion != -1 && MinMajorVersion != -1 && CurrentMajorVersion != -1)
			{
				int CurrentVersionOptionIndex = 1;

				foreach(int CurrentVersionValue in AllValidVersionOptions)
				{
					if(CurrentVersionValue >= MinMinorVersion)
					{
						if(CurrentVersionValue == CurrentMinorVersion)
						{
							SelectedOption = CurrentVersionOptionIndex;
						}

						VersionOptions.Add(CurrentVersionValue.ToString());

						++CurrentVersionOptionIndex;
					}
				}
			}

			if(VersionOptions.Count == 1)
			{
				EditorGUILayout.LabelField(VersionOptions[0], GUILayout.MaxWidth(65.0f));
			}
			else
			{
				SelectedOption = EditorGUILayout.Popup(SelectedOption, VersionOptions.ToArray(), GUILayout.MaxWidth(65.0f));
			}

			if(SelectedOption >= 0 && SelectedOption < VersionOptions.Count)
			{
				string VersionString = VersionOptions[SelectedOption];

				if(VersionString == "Latest")
				{
					return -1;
				}

				int TestInt = -1;

				if(int.TryParse(VersionString, out TestInt))
				{
					return TestInt;
				}
			}

			return -1;
		}
		
		public virtual int DrawReleaseVersionDropDown(int CurrentMinorVersion, int CurrentReleaseVersion, int MinMajorVersion, int MinMinorVersion, int MinReleaseVersion)
		{
			int[] AllValidVersionOptions = { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 };

			List<string> VersionOptions = new List<string>();
			int SelectedOption = 0;

			VersionOptions.Add("Latest");

			if(MinReleaseVersion != -1 && MinMinorVersion != -1 && CurrentMinorVersion != -1)
			{
				int CurrentVersionOptionIndex = 1;

				foreach(int CurrentVersionValue in AllValidVersionOptions)
				{
					if(CurrentVersionValue >= MinReleaseVersion)
					{
						if(CurrentVersionValue == CurrentReleaseVersion)
						{
							SelectedOption = CurrentVersionOptionIndex;
						}

						VersionOptions.Add(CurrentVersionValue.ToString());

						++CurrentVersionOptionIndex;
					}
				}
			}

			if(VersionOptions.Count == 1)
			{
				EditorGUILayout.LabelField(VersionOptions[0], GUILayout.MaxWidth(65.0f));
			}
			else
			{
				SelectedOption = EditorGUILayout.Popup(SelectedOption, VersionOptions.ToArray(), GUILayout.MaxWidth(65.0f));
			}

			if(SelectedOption >= 0 && SelectedOption < VersionOptions.Count)
			{
				string VersionString = VersionOptions[SelectedOption];

				if(VersionString == "Latest")
				{
					return -1;
				}

				int TestInt = -1;

				if(int.TryParse(VersionString, out TestInt))
				{
					return TestInt;
				}
			}

			return -1;
		}
		
		public virtual int DrawPatchVersionDropDown(int CurrentReleaseVersion, int CurrentPatchVersion, int MinMajorVersion, int MinMinorVersion, int MinReleaseVersion, int MinPatchVersion)
		{
			int[] AllValidVersionOptions = { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 };

			List<string> VersionOptions = new List<string>();
			int SelectedOption = 0;

			VersionOptions.Add("Latest");

			if(MinPatchVersion != -1 && MinReleaseVersion != -1 && CurrentReleaseVersion != -1)
			{
				int CurrentVersionOptionIndex = 1;

				foreach(int CurrentVersionValue in AllValidVersionOptions)
				{
					if(CurrentVersionValue >= MinPatchVersion)
					{
						if(CurrentVersionValue == CurrentPatchVersion)
						{
							SelectedOption = CurrentVersionOptionIndex;
						}

						VersionOptions.Add(CurrentVersionValue.ToString());

						++CurrentVersionOptionIndex;
					}
				}
			}

			if(VersionOptions.Count == 1)
			{
				EditorGUILayout.LabelField(VersionOptions[0], GUILayout.MaxWidth(65.0f));
			}
			else
			{
				SelectedOption = EditorGUILayout.Popup(SelectedOption, VersionOptions.ToArray(), GUILayout.MaxWidth(65.0f));
			}

			if(SelectedOption >= 0 && SelectedOption < VersionOptions.Count)
			{
				string VersionString = VersionOptions[SelectedOption];

				if(VersionString == "Latest")
				{
					return -1;
				}

				int TestInt = -1;

				if(int.TryParse(VersionString, out TestInt))
				{
					return TestInt;
				}
			}

			return -1;
		}
#endif // UNITY_EDITOR
		
		public class UnityVersionInfo
		{
			public static int Latest = -1;
			public string Platform = "";
			public int MajorVersion = -1;
			public int MinorVersion = -1;
			public int ReleaseVersion = -1;
			public int PatchVersion = -1;

			public static UnityVersionInfo FromString(string ParamValue)
			{
				UnityVersionInfo NewInfo = new UnityVersionInfo();

				string[] ParamComponents = ParamValue.Split('_');

				if(ParamComponents.Length >= 1)
				{
					NewInfo.Platform = ParamComponents[0].Replace("UNITY", "");

					int TempResult = -1;

					if(ParamComponents.Length >= 2 && int.TryParse(ParamComponents[1], out TempResult))
					{
						NewInfo.MajorVersion = TempResult;

						if(ParamComponents.Length >= 3 && int.TryParse(ParamComponents[2], out TempResult))
						{
							NewInfo.MinorVersion = TempResult;
		
							if(ParamComponents.Length >= 4 && int.TryParse(ParamComponents[3], out TempResult))
							{
								NewInfo.ReleaseVersion = TempResult;
		
								if(ParamComponents.Length >= 5 && int.TryParse(ParamComponents[4], out TempResult))
								{
									NewInfo.PatchVersion = TempResult;
								}
							}
						}
					}
				}

				return NewInfo;
			}

			public override string ToString()
			{
				string FinalString = "UNITY" + Platform;

				if(MajorVersion != -1)
				{
					FinalString += "_" + MajorVersion;

					if(MinorVersion != -1)
					{
						FinalString += "_" + MinorVersion;

						if(ReleaseVersion != -1)
						{
							FinalString += "_" + ReleaseVersion;

							if(PatchVersion != -1)
							{
								FinalString += "_" + PatchVersion;
							}
							else
							{
								FinalString += "_LATEST";
							}
						}
						else
						{
							FinalString += "_LATEST";
						}
					}
					else
					{
						FinalString += "_LATEST";
					}
				}
				else
				{
					FinalString += "_LATEST";
				}

				return FinalString;
			}
		}
	}
}

#endif // IGOR_RUNTIME || UNITY_EDITOR
