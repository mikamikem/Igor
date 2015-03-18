using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using System.IO;

namespace Igor
{
	public class IgorConfigWindow : EditorWindow
	{
		[MenuItem("Window/Igor/Igor Configuration", false, 1)]
		public static IgorConfigWindow OpenOrGetConfigWindow()
		{
			System.Reflection.Assembly EditorAssembly = typeof(UnityEditor.EditorWindow).Assembly;
			
			System.Type InspectorViewType = EditorAssembly.GetType("UnityEditor.InspectorWindow");
			
			IgorConfigWindow Window = null;

			if(InspectorViewType != null)
			{
				Window = EditorWindow.GetWindow<IgorConfigWindow>("Igor Project Configuration", InspectorViewType);
			}
			else
			{
				Window = EditorWindow.GetWindow<IgorConfigWindow>("Igor Project Configuration");
			}

			Window.Show();

			return Window;
		}

		public static double DelayAfterCompiling = 10.0;
		public static double TimeToActivate = DelayAfterCompiling;

		private bool bInitialized = false;

		protected List<string> AvailableModuleNames = new List<string>();
		protected bool bAvailableModulesExpanded = false;
		protected bool bModulesChanged = false;

		protected List<IgorPersistentJobConfig> Jobs = new List<IgorPersistentJobConfig>();
		protected static bool bStaticDevMode = false;
		protected bool bDevMode = false;
		protected int CurrentJob = -1;

		protected Dictionary<IIgorModule, bool> IsModuleExpanded = new Dictionary<IIgorModule, bool>(); 
		protected bool bShowParams = true;

		public virtual void Update()
		{
			if(!bInitialized)
			{
				Initialize();
			}

			if(EditorApplication.isCompiling)
			{
				IgorJobConfig.SetBoolParam("wascompiling", true);

				bInitialized = false;

				TimeToActivate = EditorApplication.timeSinceStartup + DelayAfterCompiling;

				Repaint();
			}
			else if(!EditorApplication.isCompiling && EditorApplication.timeSinceStartup > TimeToActivate)
			{
				if(IgorJobConfig.IsBoolParamSet("wascompiling"))
				{
					IgorJobConfig.SetBoolParam("wascompiling", false);
					TimeToActivate = EditorApplication.timeSinceStartup + DelayAfterCompiling;

//					IgorCore.Log(null, "Pausing for " + DelayAfterCompiling + " seconds to make sure Unity is done compiling.");

					Repaint();
				}
				else if(IgorJobConfig.GetIsRunning())
				{
					IgorCore.RunJob();
				}
			}
		}

		protected virtual void Initialize()
		{
			if(IgorJobConfig.IsBoolParamSet("wascompiling"))
			{
				return;
			}

			TimeToActivate = EditorApplication.timeSinceStartup + DelayAfterCompiling;

			GetValuesFromConfig();

			if(Jobs.Count > 0)
			{
				CurrentJob = 0;
			}

			IgorCore.RegisterAllModules();

			GenerateListOfAvailableModules();

			bInitialized = true;
		}

		protected virtual void GenerateListOfAvailableModules()
		{
			if(File.Exists(IgorUpdater.InstalledModulesListPath))
			{
				IgorModuleList ModuleListInst = IgorModuleList.Load(IgorUpdater.InstalledModulesListPath);

				if(ModuleListInst != null)
				{
					AvailableModuleNames.Clear();

					foreach(IgorModuleList.ModuleItem CurrentModule in ModuleListInst.Modules)
					{
						AvailableModuleNames.Add(CurrentModule.ModuleName);
					}
				}
			}
		}

		protected virtual void OnGUI()
		{
			try
			{
				if(!bInitialized)
				{
					Initialize();
				}

				if(IgorJobConfig.GetIsRunning())
				{
					DrawJobRunning();
				}
				else if(IgorJobConfig.IsBoolParamSet("wascompiling"))
				{
					DrawWaitingForCompile();
				}
				else
				{
					DrawConfiguration();
				}
			}
			catch(System.Exception e)
			{
				IgorCore.LogError(null, "ListWindow threw an exception in OnGUI() - " + e.ToString());
			}
		}

		protected virtual void DrawJobRunning()
		{
			EditorGUILayout.LabelField("Igor is busy running a job for you!");
		}

		protected virtual void DrawWaitingForCompile()
		{
			EditorGUILayout.LabelField("Igor is waiting for Unity to finish recompiling!");
		}

		protected virtual void DrawConfiguration()
		{
			EditorGUILayout.BeginVertical("box");

			EditorGUILayout.LabelField("Igor Global Config");

			IgorConfig ConfigInst = IgorConfig.GetInstance();

			if(ConfigInst != null)
			{
				EditorGUILayout.Separator();

				EditorGUILayout.BeginVertical("box");

				bAvailableModulesExpanded = EditorGUILayout.Foldout(bAvailableModulesExpanded, "Available Modules");

				if(bAvailableModulesExpanded)
				{
					EditorGUI.indentLevel += 1;

					foreach(string CurrentModule in AvailableModuleNames)
					{
						if(CurrentModule == "Core.Core")
						{
							continue;
						}
						
						bool bWasInstalled = false;

						if(ConfigInst.EnabledModules.Contains(CurrentModule))
						{
							bWasInstalled = true;
						}

						bool bInstalled = EditorGUILayout.Toggle(CurrentModule, bWasInstalled);

						if(!bWasInstalled && bInstalled)
						{
							ConfigInst.EnabledModules.Add(CurrentModule);

							bModulesChanged = true;
						}
						else if(bWasInstalled && !bInstalled)
						{
							ConfigInst.EnabledModules.Remove(CurrentModule);

							bModulesChanged = true;
						}
					}

					EditorGUI.indentLevel -= 1;
				}

				EditorGUILayout.EndVertical();

				EditorGUILayout.Separator();
			}

			DrawJobDropdown();

			bDevMode = EditorGUILayout.Toggle("Developer Mode", bDevMode);

			bStaticDevMode = bDevMode;

			EditorGUILayout.Separator();

			EditorGUILayout.BeginHorizontal();

			if(GUILayout.Button("Reload Configuration") && !EditorUtility.DisplayDialog("Reload Igor's Configuration?", "This will reset any unsaved changes for the global values AND ALL JOBS!  Are you sure you want to reload?", "I'm not ready yet...", "Yup clobber my unsaved changes!"))
			{
				GetValuesFromConfig();
			}

			if(GUILayout.Button("Save Configuration"))
			{
				SaveConfiguration();
			}

			EditorGUILayout.EndHorizontal();

			EditorGUILayout.EndVertical();

			EditorGUILayout.Separator();

			EditorGUILayout.BeginVertical("box");

			EditorGUILayout.LabelField("Igor Job Config");

			if(CurrentJob < Jobs.Count && CurrentJob >= 0)
			{
				IgorPersistentJobConfig CurrentJobInst = Jobs[CurrentJob];

				EditorGUILayout.Separator();

				if(GUILayout.Button("Run Job"))
				{
					TriggerJob(CurrentJobInst.JobName);
				}

				EditorGUILayout.Separator();

				if(GUILayout.Button("Delete job") && !EditorUtility.DisplayDialog("Delete this job configuration?", "This will delete the whole job!  Are you sure you want to delete this?", "I'm not ready yet...", "Yup delete the job!"))
				{
					DeleteSelectedJob();
				}

				EditorGUILayout.Separator();

				CurrentJobInst.JobName = EditorGUILayout.TextField("Job Name", CurrentJobInst.JobName);

				EditorGUILayout.Separator();

				EditorGUILayout.BeginVertical("box");

				bShowParams = GUILayout.SelectionGrid(bShowParams ? 0 : 1, new string[] { "Parameters", "Jenkins Job" }, 2) == 0;

				GUIStyle TextAreaStyle = new GUIStyle(EditorStyles.textArea);
				TextAreaStyle.wordWrap = true;

				if(bShowParams)
				{
					EditorGUILayout.TextArea(CurrentJobInst.JobCommandLineParams, TextAreaStyle);
				}
				else
				{
					EditorGUILayout.TextArea(GenerateJenkinsJobForParams(CurrentJobInst.JobCommandLineParams), TextAreaStyle);
				}

				EditorGUILayout.EndVertical();

				EditorGUILayout.Separator();

				foreach(IIgorModule CurrentModule in IgorCore.EnabledModules)
				{
					if(!IsModuleExpanded.ContainsKey(CurrentModule))
					{
						IsModuleExpanded.Add(CurrentModule, false);
					}

					bool bIsExpanded = IsModuleExpanded[CurrentModule];

					EditorGUILayout.BeginVertical("box");

					bIsExpanded = EditorGUILayout.Foldout(bIsExpanded, CurrentModule.GetModuleName());

					IsModuleExpanded[CurrentModule] = bIsExpanded;

					if(bIsExpanded)
					{
						EditorGUI.indentLevel += 1;

						CurrentJobInst.JobCommandLineParams = CurrentModule.DrawJobInspectorAndGetEnabledParams(CurrentJobInst.JobCommandLineParams);

						EditorGUI.indentLevel -= 1;
					}

					EditorGUILayout.EndVertical();
				}
			}
			else
			{
				EditorGUILayout.LabelField("Select a job above.");
			}

			EditorGUILayout.EndVertical();
		}

		public virtual void SaveConfiguration()
		{
			List<IgorPersistentJobConfig> NewJobs = new List<IgorPersistentJobConfig>();

			foreach(IgorPersistentJobConfig CurrentJob in Jobs)
			{
				IgorPersistentJobConfig NewJob = new IgorPersistentJobConfig();

				NewJob.JobName = string.Copy(CurrentJob.JobName);
				NewJob.JobCommandLineParams = string.Copy(CurrentJob.JobCommandLineParams);

				NewJobs.Add(NewJob);
			}

			IgorConfig.GetInstance().JobConfigs = NewJobs;
			IgorConfig.GetInstance().Save();

			IgorConfig.SetIsDevMode(bDevMode);

			GenerateEditorMenuOptions();

			if(bModulesChanged)
			{
				IgorUpdater.CheckForUpdates(false, true);
			}

			AssetDatabase.Refresh();
		}

		public virtual void DrawJobDropdown()
		{
			List<string> JobNames = new List<string>();

			foreach(IgorPersistentJobConfig CurrentJobInst in Jobs)
			{
				JobNames.Add(CurrentJobInst.JobName);
			}

			JobNames.Add("Create new job");

			CurrentJob = EditorGUILayout.Popup("Job to configure", CurrentJob, JobNames.ToArray());

			if(CurrentJob == (JobNames.Count - 1))
			{
				IgorPersistentJobConfig NewJob = new IgorPersistentJobConfig();

				NewJob.JobName = "New job";

				Jobs.Add(NewJob);
			}
		}

		public virtual void DeleteSelectedJob()
		{
			Jobs.RemoveAt(CurrentJob);

			if(CurrentJob >= Jobs.Count)
			{
				CurrentJob = Jobs.Count - 1;
			}
		}

		public static void TriggerJob(string JobName)
		{
			IgorConfig.TriggerJobByName(JobName, true);
		}

		public virtual void GetValuesFromConfig()
		{
			List<IgorPersistentJobConfig> NewJobs = new List<IgorPersistentJobConfig>();

			foreach(IgorPersistentJobConfig CurrentJob in IgorConfig.GetAllJobs())
			{
				IgorPersistentJobConfig NewJob = new IgorPersistentJobConfig();

				NewJob.JobName = string.Copy(CurrentJob.JobName);
				NewJob.JobCommandLineParams = string.Copy(CurrentJob.JobCommandLineParams);

				NewJobs.Add(NewJob);
			}

			Jobs = NewJobs;

			bDevMode = IgorConfig.GetIsDevMode();

			bModulesChanged = false;
		}

		protected string JenkinsJobHeader = "python " + Path.Combine(IgorUpdater.BaseIgorDirectory, IgorRunner) + " ";
		protected string JenkinsJobFooter = "";

		public static string IgorRunner = "IgorRun.py";

		public virtual string GenerateJenkinsJobForParams(string Params)
		{
			return JenkinsJobHeader + Params + JenkinsJobFooter;
		}

		protected string EditorMenuOptionsFilePath = Path.Combine(IgorUpdater.LocalModuleRoot, Path.Combine("Core", Path.Combine("Core", "IgorMenuOptions.cs")));
		protected string EditorMenuOptionHeader = "using UnityEngine;\nusing UnityEditor;\n\nnamespace Igor\n{\n\tpublic class IgorJobMenu\n\t{\n";
		protected string EditorMenuOptionFunction = "\t\t[MenuItem(\"Window/Igor/{0}\", false, {1})]\n\t\tpublic static void {2}()\n\t\t{\n\t\t\tIgorConfigWindow.TriggerJob(\"{0}\");\n\t\t}\n";
		protected string EditorMenuOptionFooter = "\t}\n}\n\n";

		public virtual string GenerateFunctionStringForJob(string FunctionName, int FunctionID)
		{
			string NewFunction = EditorMenuOptionFunction;

			NewFunction = NewFunction.Replace("{0}", FunctionName);
			NewFunction = NewFunction.Replace("{1}", FunctionID.ToString());
			NewFunction = NewFunction.Replace("{2}", FunctionName.Replace(" ", "_"));

			return NewFunction;
		}

		public virtual void GenerateEditorMenuOptions()
		{
			string FullEditorMenuOptionFile = EditorMenuOptionHeader;
			int Index = 50;

			foreach(IgorPersistentJobConfig CurrentConfig in Jobs)
			{
				FullEditorMenuOptionFile += GenerateFunctionStringForJob(CurrentConfig.JobName, Index);

				++Index;
			}

			FullEditorMenuOptionFile += EditorMenuOptionFooter;

			File.WriteAllText(EditorMenuOptionsFilePath, FullEditorMenuOptionFile);
		}

		[MenuItem("Window/Igor/Dev/GenerateModuleDescriptor", true)]
		public static bool IsInDevMode()
		{
			return bStaticDevMode;
		}

		[MenuItem("Window/Igor/Dev/GenerateModuleDescriptor", false, 3)]
		public static void GenerateModuleDescriptor()
		{
			IgorModuleDescriptor NewInst = new IgorModuleDescriptor();

			NewInst.ModuleName = "Core.Core";
			NewInst.ModuleVersion = 1;
			NewInst.ModuleFiles.Add("Core.cs");

			NewInst.Save("Test.mod");
		}
	}
}