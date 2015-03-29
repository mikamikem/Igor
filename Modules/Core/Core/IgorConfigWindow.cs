using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using System.IO;

namespace Igor
{
	public class IgorConfigWindow : EditorWindow, IIgorStepHandler
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
		protected Dictionary<string, int> ModuleNamesToCurrentVersions = new Dictionary<string, int>();
		protected Dictionary<string, int> ModuleNamesToAvailableVersions = new Dictionary<string, int>();
		protected Dictionary<string, List<string>> DependencyToDependentModules = new Dictionary<string, List<string>>(); // This maps from ie. Build Common -> { Build.Desktop, Build.iOS }
		protected bool bAvailableModulesExpanded = false;
		protected Dictionary<string, bool> ModuleCategoryExpanded = new Dictionary<string, bool>();
		protected bool bModulesChanged = false;

		protected List<IgorPersistentJobConfig> Jobs = new List<IgorPersistentJobConfig>();
		protected static bool bStaticDevMode = false;
		protected bool bDevMode = false;
		protected int CurrentJob = -1;

		protected Dictionary<string, bool> ParamsModuleCategoryExpanded = new Dictionary<string, bool>();
		protected Dictionary<IIgorModule, bool> IsModuleExpanded = new Dictionary<IIgorModule, bool>(); 
		protected bool bShowParams = true;

		protected bool bJobStepsExpanded = false;
		protected Dictionary<StepID, List<string>> StepIDToFunctions = new Dictionary<StepID, List<string>>();
		protected Dictionary<StepID, bool> StepIDExpanded = new Dictionary<StepID, bool>();

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
					ModuleNamesToAvailableVersions.Clear();
					ModuleNamesToCurrentVersions.Clear();

					foreach(IgorModuleList.ModuleItem CurrentModule in ModuleListInst.Modules)
					{
						AvailableModuleNames.Add(CurrentModule.ModuleName);

						string ModuleDescriptor = IgorUtils.DownloadFileForUpdate(IgorUpdater.RemoteRelativeModuleRoot + CurrentModule.ModuleDescriptorRelativePath);
						string CurrentModuleDescriptor = Path.Combine(IgorUpdater.LocalModuleRoot, CurrentModule.ModuleDescriptorRelativePath);

						if(File.Exists(ModuleDescriptor))
						{
							IgorModuleDescriptor CurrentModuleDescriptorInst = null;
							IgorModuleDescriptor NewModuleDescriptorInst = IgorModuleDescriptor.Load(ModuleDescriptor);

							if(File.Exists(CurrentModuleDescriptor))
							{
								CurrentModuleDescriptorInst = IgorModuleDescriptor.Load(CurrentModuleDescriptor);

								if(CurrentModuleDescriptorInst != null)
								{
									ModuleNamesToCurrentVersions.Add(CurrentModule.ModuleName, CurrentModuleDescriptorInst.ModuleVersion);
								}
							}

							if(NewModuleDescriptorInst != null)
							{
								ModuleNamesToAvailableVersions.Add(CurrentModule.ModuleName, NewModuleDescriptorInst.ModuleVersion);

								foreach(string Dependency in NewModuleDescriptorInst.ModuleDependencies)
								{
									if(!DependencyToDependentModules.ContainsKey(Dependency))
									{
										List<string> NewList = new List<string>();

										NewList.Add(CurrentModule.ModuleName);

										DependencyToDependentModules.Add(Dependency, NewList);
									}
									else
									{
										DependencyToDependentModules[Dependency].Add(CurrentModule.ModuleName);
									}
								}
							}
						}
					}
				}
			}
		}

		protected virtual Dictionary<string, List<string>> GetModuleCategoriesAndNames(List<string> FullNames)
		{
			Dictionary<string, List<string>> ModuleCategoriesToNames = new Dictionary<string, List<string>>();

			foreach(string CurrentName in FullNames)
			{
				if(CurrentName.Contains("."))
				{
					string[] Parts = CurrentName.Split('.');

					if(ModuleCategoriesToNames.ContainsKey(Parts[0]))
					{
						ModuleCategoriesToNames[Parts[0]].Add(Parts[1]);
					}
					else
					{
						List<string> NewList = new List<string>();

						NewList.Add(Parts[1]);

						ModuleCategoriesToNames.Add(Parts[0], NewList);
					}
				}
			}

			return ModuleCategoriesToNames;
		}

		public virtual void RegisterJobStep(StepID CurrentStep, IIgorModule Module, IgorCore.JobStepFunc StepFunction)
		{
			if(StepIDToFunctions.ContainsKey(CurrentStep))
			{
				StepIDToFunctions[CurrentStep].Add(Module.GetModuleName() + ":" + StepFunction.Method.Name);
			}
			else
			{
				List<string> NewList = new List<string>();

				NewList.Add(Module.GetModuleName() + ":" + StepFunction.Method.Name);

				StepIDToFunctions.Add(CurrentStep, NewList);
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

			EditorGUILayout.LabelField("Igor Global Config (Igor Updater Version - " + IgorUpdater.GetCurrentVersion() + ")");

			IgorConfig ConfigInst = IgorConfig.GetInstance();

			if(ConfigInst != null)
			{
				EditorGUILayout.Separator();

				EditorGUILayout.BeginVertical("box");

				bAvailableModulesExpanded = EditorGUILayout.Foldout(bAvailableModulesExpanded, "Available Modules");

				if(bAvailableModulesExpanded)
				{
					EditorGUI.indentLevel += 1;

					Dictionary<string, List<string>> AvailableModuleGroupsAndNames = GetModuleCategoriesAndNames(AvailableModuleNames);

					foreach(KeyValuePair<string, List<string>> CurrentModules in AvailableModuleGroupsAndNames)
					{
						bool bCurrentCategoryExpanded = ModuleCategoryExpanded.ContainsKey(CurrentModules.Key) && ModuleCategoryExpanded[CurrentModules.Key];

						bCurrentCategoryExpanded = EditorGUILayout.Foldout(bCurrentCategoryExpanded, CurrentModules.Key);

						if(!ModuleCategoryExpanded.ContainsKey(CurrentModules.Key))
						{
							ModuleCategoryExpanded.Add(CurrentModules.Key, bCurrentCategoryExpanded);
						}

						ModuleCategoryExpanded[CurrentModules.Key] = bCurrentCategoryExpanded;

						if(bCurrentCategoryExpanded)
						{
							EditorGUI.indentLevel += 1;

							foreach(string CurrentModuleName in CurrentModules.Value)
							{
								bool bWasInstalled = false;
								string MergedName = CurrentModules.Key + "." + CurrentModuleName;
								bool bIsDependency = CurrentModuleName == "Core" && CurrentModules.Key == "Core";
								string DependentOf = bIsDependency ? "Everything" : "";

								if(!bIsDependency && DependencyToDependentModules.ContainsKey(MergedName))
								{
									foreach(string CurrentDependent in DependencyToDependentModules[MergedName])
									{
										if(ConfigInst.EnabledModules.Contains(CurrentDependent))
										{
											if(bIsDependency)
											{
												DependentOf += ", ";
											}

											DependentOf += CurrentDependent;

											bIsDependency = true;
										}
									}
								}

								string VersionString = "";

								if(ModuleNamesToCurrentVersions.ContainsKey(MergedName))
								{
									VersionString += " - Inst (" + ModuleNamesToCurrentVersions[MergedName] + ")";
								}

								if(ModuleNamesToAvailableVersions.ContainsKey(MergedName))
								{
									VersionString += " Avail (" + ModuleNamesToAvailableVersions[MergedName] + ")";
								}

								if(ConfigInst.EnabledModules.Contains(MergedName))
								{
									bWasInstalled = true;
								}

								GUI.enabled = !bIsDependency;

								EditorGUILayout.BeginHorizontal();

								bool bInstalled = EditorGUILayout.ToggleLeft(CurrentModuleName + VersionString + (bIsDependency ? " - Required by (" + DependentOf + ")" : ""), bWasInstalled || bIsDependency);

								EditorGUILayout.EndHorizontal();

								GUI.enabled = true;

								if(!bIsDependency)
								{
									if(!bWasInstalled && bInstalled)
									{
										ConfigInst.EnabledModules.Add(MergedName);

										bModulesChanged = true;
									}
									else if(bWasInstalled && !bInstalled)
									{
										ConfigInst.EnabledModules.Remove(MergedName);

										bModulesChanged = true;
									}
								}
							}

							EditorGUI.indentLevel -= 1;
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

				EditorGUILayout.BeginVertical("box");

				Dictionary<string, IIgorModule> ModuleNameToInst = new Dictionary<string, IIgorModule>();
				List<string> EnabledModuleNames = new List<string>();

				foreach(IIgorModule CurrentModule in IgorCore.EnabledModules)
				{
					ModuleNameToInst.Add(CurrentModule.GetModuleName(), CurrentModule);
					EnabledModuleNames.Add(CurrentModule.GetModuleName());
				}

				Dictionary<string, List<string>> AvailableModuleGroupsAndNames = GetModuleCategoriesAndNames(EnabledModuleNames);

				bJobStepsExpanded = EditorGUILayout.Foldout(bJobStepsExpanded, "Enabled Job Steps");

				if(bJobStepsExpanded)
				{
					EditorGUI.indentLevel += 1;

					StepIDToFunctions.Clear();

					IgorJobConfig TempConfig = new IgorJobConfig();

					TempConfig.Persistent = CurrentJobInst;

					IgorJobConfig.InternalOverride = TempConfig;

					foreach(IIgorModule CurrentModule in IgorCore.EnabledModules)
					{
						CurrentModule.ProcessArgs(this);
					}

					IgorJobConfig.InternalOverride = null;

					List<StepID> SortedSteps = new List<StepID>(StepIDToFunctions.Keys);

					SortedSteps.Sort();

					foreach(StepID CurrentStep in SortedSteps)
					{
						bool bCurrentStepExpanded = StepIDExpanded.ContainsKey(CurrentStep) && StepIDExpanded[CurrentStep];

						EditorGUILayout.BeginVertical("box");

						bCurrentStepExpanded = EditorGUILayout.Foldout(bCurrentStepExpanded, CurrentStep.StepPriority.ToString() + " - " + CurrentStep.StepName);

						if(!StepIDExpanded.ContainsKey(CurrentStep))
						{
							StepIDExpanded.Add(CurrentStep, bCurrentStepExpanded);
						}

						StepIDExpanded[CurrentStep] = bCurrentStepExpanded;

						if(bCurrentStepExpanded)
						{
							List<string> CurrentStepFuncs = StepIDToFunctions[CurrentStep];

							EditorGUI.indentLevel += 1;

							foreach(string CurrentStepFunctionName in CurrentStepFuncs)
							{
								EditorGUILayout.LabelField(CurrentStepFunctionName);
							}

							EditorGUI.indentLevel -= 1;
						}

						EditorGUILayout.EndVertical();
					}

					EditorGUI.indentLevel -= 1;
				}

				EditorGUILayout.EndVertical();

				EditorGUILayout.Separator();

				EditorGUILayout.BeginVertical("box");

				EditorGUILayout.LabelField("Module Options");

				foreach(KeyValuePair<string, List<string>> CurrentModules in AvailableModuleGroupsAndNames)
				{
					if(CurrentModules.Key == "Core" && CurrentModules.Value.Count == 1)
					{
						continue;
					}

					bool bCurrentCategoryExpanded = ParamsModuleCategoryExpanded.ContainsKey(CurrentModules.Key) && ParamsModuleCategoryExpanded[CurrentModules.Key];

					bCurrentCategoryExpanded = EditorGUILayout.Foldout(bCurrentCategoryExpanded, CurrentModules.Key);

					if(!ParamsModuleCategoryExpanded.ContainsKey(CurrentModules.Key))
					{
						ParamsModuleCategoryExpanded.Add(CurrentModules.Key, bCurrentCategoryExpanded);
					}

					ParamsModuleCategoryExpanded[CurrentModules.Key] = bCurrentCategoryExpanded;

					if(bCurrentCategoryExpanded)
					{
						EditorGUI.indentLevel += 1;

						foreach(string CurrentModuleName in CurrentModules.Value)
						{
							string FullModuleName = CurrentModules.Key + "." + CurrentModuleName;

							IIgorModule CurrentModule = ModuleNameToInst.ContainsKey(FullModuleName) ? ModuleNameToInst[FullModuleName] : null;

							if(CurrentModule == null)
							{
								continue;
							}

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

						EditorGUI.indentLevel -= 1;
					}
				}

				EditorGUILayout.EndVertical();
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