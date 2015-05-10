#if UNITY_4_5 || UNITY_4_6 || UNITY_5_0
#define GREATER_THAN_4_3
#endif

using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditorInternal;

namespace Igor
{
	public class IgorConfigWindow : EditorWindow, IIgorStepHandler
	{
        [System.Serializable]
	    public class FoldoutState : SerializableDictionary<string, bool>
	    { }

	    static IgorConfigWindow _window;
	    static IgorConfigWindow Window
	    {
	        get
	        {
	            if(_window == null)
	            {
	                System.Reflection.Assembly EditorAssembly = typeof(UnityEditor.EditorWindow).Assembly;
			        System.Type InspectorViewType = EditorAssembly.GetType("UnityEditor.InspectorWindow");
			
			        if(InspectorViewType != null)
			        {
				        _window = EditorWindow.GetWindow<IgorConfigWindow>("Igor Project Configuration", InspectorViewType);
			        }
			        else
			        {
				        _window = EditorWindow.GetWindow<IgorConfigWindow>("Igor Project Configuration");
			        }

			        _window.Show();
	            }
                return _window;
	        }
	    }

		[MenuItem("Window/Igor/Igor Configuration", false, 1)]
		public static IgorConfigWindow OpenOrGetConfigWindow() { return Window; }

		public static double DelayAfterCompiling = 10.0;
		public static double TimeToActivate = DelayAfterCompiling;

		private bool bInitialized = false;

		protected List<string> AvailableModuleNames = new List<string>();
		protected Dictionary<string, int> ModuleNamesToCurrentVersions = new Dictionary<string, int>();
		protected Dictionary<string, int> ModuleNamesToAvailableVersions = new Dictionary<string, int>();
		protected Dictionary<string, List<string>> DependencyToDependentModules = new Dictionary<string, List<string>>(); // This maps from ie. Build Common -> { Build.Desktop, Build.iOS }
		protected bool bAvailableModulesExpanded = false;

        [SerializeField]
		protected FoldoutState ModuleCategoryExpanded = new FoldoutState();

		protected bool bModulesChanged = false;

		protected List<IgorPersistentJobConfig> Jobs = new List<IgorPersistentJobConfig>();

        [SerializeField]
		protected int _currentJobIndex = -1;

	    protected int CurrentJobIndex
	    {
            get {  return Mathf.Clamp(_currentJobIndex, 0, Jobs.Count - 1); }
            set { _currentJobIndex = Mathf.Clamp(value, 0, Jobs.Count - 1); }
	    }

		protected FoldoutState ParamsModuleCategoryExpanded = new FoldoutState();
		protected FoldoutState IsModuleExpanded = new FoldoutState(); 
		protected bool bShowParams = true;
		protected bool bTriggerJobByName = false;

		protected bool bJobStepsExpanded = false;
		protected Dictionary<StepID, List<string>> StepIDToFunctions = new Dictionary<StepID, List<string>>();
		protected FoldoutState StepIDExpanded = new FoldoutState();

	    protected Vector2 ScrollPosition ;

	    public IgorPersistentJobConfig CurrentJobInst
	    {
            get
            {
                if(Jobs.Count > 0)
                    return Jobs[CurrentJobIndex];
                return null;
            }
	    }

		public virtual void Update()
		{
			bool bQueueRepaint = false;

			if(!bInitialized || IgorUpdater.bTriggerConfigWindowRefresh)
			{
				IgorConfig.ReGetInstance();
				
				IgorUpdater.bTriggerConfigWindowRefresh = false;

				Initialize();

				bQueueRepaint = true;
			}

			if(EditorApplication.isCompiling)
			{
				IgorJobConfig.SetBoolParam("wascompiling", true);

				bInitialized = false;

				TimeToActivate = EditorApplication.timeSinceStartup + DelayAfterCompiling;

				Repaint();

				bQueueRepaint = false;
			}
			else if(!EditorApplication.isCompiling && EditorApplication.timeSinceStartup > TimeToActivate)
			{
				if(IgorJobConfig.IsBoolParamSet("wascompiling"))
				{
					IgorJobConfig.SetBoolParam("wascompiling", false);
					TimeToActivate = EditorApplication.timeSinceStartup + DelayAfterCompiling;

//					IgorCore.Log(null, "Pausing for " + DelayAfterCompiling + " seconds to make sure Unity is done compiling.");

					Repaint();

					bQueueRepaint = false;
				}
				else if(IgorJobConfig.GetIsRunning())
				{
					IgorCore.RunJob();
				}
			}

			if(bQueueRepaint)
			{
				Repaint();
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

					AppendToModuleList(ModuleListInst, false);

					IgorModuleList LocalModuleListInst = IgorModuleList.Load(IgorUpdater.InstalledLocalModulesListPath);

					if(LocalModuleListInst != null)
					{
						AppendToModuleList(LocalModuleListInst, true);
					}
				}
			}
		}

		protected virtual void AppendToModuleList(IgorModuleList ModuleListInst, bool bLocal)
		{
			foreach(IgorModuleList.ModuleItem CurrentModule in ModuleListInst.Modules)
			{
				if(!AvailableModuleNames.Contains(CurrentModule.ModuleName))
				{
					AvailableModuleNames.Add(CurrentModule.ModuleName);
				}

				string ModuleDescriptor = "";

				if(!bLocal)
				{
					ModuleDescriptor = IgorUtils.DownloadFileForUpdate(IgorUpdater.RemoteRelativeModuleRoot + CurrentModule.ModuleDescriptorRelativePath);
				}

				string CurrentModuleDescriptor = Path.Combine(IgorUpdater.LocalModuleRoot, CurrentModule.ModuleDescriptorRelativePath);
				IgorModuleDescriptor CurrentModuleDescriptorInst = null;
				
				if(File.Exists(CurrentModuleDescriptor))
				{
					CurrentModuleDescriptorInst = IgorModuleDescriptor.Load(CurrentModuleDescriptor);

					if(CurrentModuleDescriptorInst != null)
					{
						if(!ModuleNamesToCurrentVersions.ContainsKey(CurrentModule.ModuleName))
						{
							ModuleNamesToCurrentVersions.Add(CurrentModule.ModuleName, CurrentModuleDescriptorInst.ModuleVersion);
						}
					}
				}

				IgorModuleDescriptor NewModuleDescriptorInst = null;

				if(!bLocal && File.Exists(ModuleDescriptor))
				{
					NewModuleDescriptorInst = IgorModuleDescriptor.Load(ModuleDescriptor);

					if(NewModuleDescriptorInst != null)
					{
						if(!ModuleNamesToAvailableVersions.ContainsKey(CurrentModule.ModuleName))
						{
							ModuleNamesToAvailableVersions.Add(CurrentModule.ModuleName, NewModuleDescriptorInst.ModuleVersion);
						}
					}
				}

				if(CurrentModuleDescriptorInst != null || NewModuleDescriptorInst != null)
				{
					IgorModuleDescriptor ValidDescriptor = NewModuleDescriptorInst;

					if(ValidDescriptor == null)
					{
						ValidDescriptor = CurrentModuleDescriptorInst;
					}

					foreach(string Dependency in ValidDescriptor.ModuleDependencies)
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

                if(!IgorJobConfig.GetIsRunning() || IgorCore.EnabledModules.Count == 0)
				{
					IgorCore.RegisterAllModules();
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
				// Sometimes when transitioning to running the GUI freaks out and throws exceptions, so we can ignore those
				if(!IgorJobConfig.GetIsRunning())
				{
					IgorCore.LogError(null, "ListWindow threw an exception in OnGUI() - " + e.ToString());
				}
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
			ScrollPosition = EditorGUILayout.BeginScrollView(ScrollPosition);

		    EditorGUILayout.BeginVertical("box");
		    {
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

		                List<string> SortedGroups = new List<string>();

		                SortedGroups.AddRange(AvailableModuleGroupsAndNames.Keys);

		                SortedGroups.Sort();

		                foreach(string CurrentGroup in SortedGroups)
		                {
		                    List<string> SortedModules = new List<string>();

		                    SortedModules.AddRange(AvailableModuleGroupsAndNames[CurrentGroup]);

		                    SortedModules.Sort();

		                    bool bCurrentCategoryExpanded = ModuleCategoryExpanded.ContainsKey(CurrentGroup) && ModuleCategoryExpanded[CurrentGroup];

		                    bCurrentCategoryExpanded = EditorGUILayout.Foldout(bCurrentCategoryExpanded, CurrentGroup);

		                    if(!ModuleCategoryExpanded.ContainsKey(CurrentGroup))
		                    {
		                        ModuleCategoryExpanded.Add(CurrentGroup, bCurrentCategoryExpanded);
		                    }

		                    ModuleCategoryExpanded[CurrentGroup] = bCurrentCategoryExpanded;

		                    if(bCurrentCategoryExpanded)
		                    {
		                        EditorGUI.indentLevel += 1;

		                        foreach(string CurrentModuleName in SortedModules)
		                        {
		                            bool bWasInstalled = false;
		                            string MergedName = CurrentGroup + "." + CurrentModuleName;
		                            bool bIsDependency = CurrentModuleName == "Core" && CurrentGroup == "Core";
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
		                            bool bNeedsDash = true;

		                            if(ModuleNamesToCurrentVersions.ContainsKey(MergedName))
		                            {
		                                VersionString += " - Inst (" + ModuleNamesToCurrentVersions[MergedName] + ")";

		                                bNeedsDash = false;
		                            }

		                            if(ModuleNamesToAvailableVersions.ContainsKey(MergedName))
		                            {
		                            	if(bNeedsDash)
		                            	{
		                            		VersionString += " -";
		                            	}

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
								else
								{
									if(!bWasInstalled)
									{
										ConfigInst.EnabledModules.Add(MergedName);

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

		        EditorGUILayout.Separator();

		        EditorGUILayout.BeginHorizontal();

		        if(GUILayout.Button("Reload Configuration") && !EditorUtility.DisplayDialog("Reload Igor's Configuration?", "This will reset any unsaved changes for the global values AND ALL JOBS!  Are you sure you want to reload?", "I'm not ready yet...", "Yup clobber my unsaved changes!"))
		        {
		        	IgorUpdater.bTriggerConfigWindowRefresh = true;
		        }

		        if(GUILayout.Button("Save Configuration"))
		        {
		            SaveConfiguration();
		        }

		        EditorGUILayout.EndHorizontal();

                EditorGUILayout.Separator();

                GUI.enabled = CurrentJobInst != null;
                GUILayout.BeginHorizontal();
                {
				    if(GUILayout.Button("Run Job"))
				    {
					    TriggerJob(CurrentJobInst.JobName);
				    }

				    if(GUILayout.Button("Delete job") && !EditorUtility.DisplayDialog("Delete this job configuration?", "This will delete the whole job!  Are you sure you want to delete this?", "I'm not ready yet...", "Yup delete the job!"))
				    {
					    DeleteSelectedJob();
				    }
                }
                GUILayout.EndHorizontal();
                GUI.enabled = true;
		    }
		    EditorGUILayout.EndVertical();

			EditorGUILayout.Separator();

			EditorGUILayout.BeginVertical("box");

			EditorGUILayout.LabelField("Igor Job Config");

			if(CurrentJobInst != null)
			{
				EditorGUILayout.Separator();

				CurrentJobInst.JobName = EditorGUILayout.TextField("Job Name", CurrentJobInst.JobName);

				EditorGUILayout.Separator();

				EditorGUILayout.BeginVertical("box");

				bShowParams = GUILayout.SelectionGrid(bShowParams ? 0 : 1, new string[] { "Parameters", "Jenkins Job" }, 2) == 0;

				bTriggerJobByName = EditorGUILayout.Toggle("Trigger Job By Name", bTriggerJobByName);

				GUIStyle TextAreaStyle = new GUIStyle(
#if GREATER_THAN_4_3
                    EditorStyles.textArea
#else
                    EditorStyles.textField
#endif
                );
				TextAreaStyle.wordWrap = true;

				if(bShowParams)
				{
					EditorGUILayout.TextArea(GenerateCommandLineParams(CurrentJobInst.JobCommandLineParams, CurrentJobInst.JobName), TextAreaStyle);
				}
				else
				{
					EditorGUILayout.TextArea(GenerateJenkinsJobForParams(CurrentJobInst.JobCommandLineParams, CurrentJobInst.JobName), TextAreaStyle);
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
						bool bCurrentStepExpanded = StepIDExpanded.ContainsKey(CurrentStep.StepName) && StepIDExpanded[CurrentStep.StepName];

						EditorGUILayout.BeginVertical("box");

						bCurrentStepExpanded = EditorGUILayout.Foldout(bCurrentStepExpanded, CurrentStep.StepPriority.ToString() + " - " + CurrentStep.StepName);

						if(!StepIDExpanded.ContainsKey(CurrentStep.StepName))
						{
							StepIDExpanded.Add(CurrentStep.StepName, bCurrentStepExpanded);
						}

						StepIDExpanded[CurrentStep.StepName] = bCurrentStepExpanded;

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

				string GlobalOptionsKey = "Global Options";

				bool bGlobalOptionsExpanded = ParamsModuleCategoryExpanded.ContainsKey(GlobalOptionsKey) && ParamsModuleCategoryExpanded[GlobalOptionsKey];

				bGlobalOptionsExpanded = EditorGUILayout.Foldout(bGlobalOptionsExpanded, "Global Options");

				if(!ParamsModuleCategoryExpanded.ContainsKey(GlobalOptionsKey))
				{
					ParamsModuleCategoryExpanded.Add(GlobalOptionsKey, bGlobalOptionsExpanded);
				}

				ParamsModuleCategoryExpanded[GlobalOptionsKey] = bGlobalOptionsExpanded;

				if(bGlobalOptionsExpanded)
				{
					EditorGUI.indentLevel += 1;

					bool bIsEnabled = IgorUtils.IsBoolParamSet(CurrentJobInst.JobCommandLineParams, IgorCore.SkipUnityUpdateFlag);

					bIsEnabled = EditorGUILayout.Toggle("Disable Igor updating when running as a job", bIsEnabled);

					CurrentJobInst.JobCommandLineParams = IgorUtils.SetBoolParam(CurrentJobInst.JobCommandLineParams, IgorCore.SkipUnityUpdateFlag, bIsEnabled);

					EditorGUI.indentLevel -= 1;
				}

				EditorGUILayout.EndVertical();

				EditorGUILayout.Separator();

				EditorGUILayout.BeginVertical("box");

				EditorGUILayout.LabelField("Module Options");

				List<string> SortedGroups = new List<string>();

				SortedGroups.AddRange(AvailableModuleGroupsAndNames.Keys);

				SortedGroups.Sort();

				foreach(string CurrentGroup in SortedGroups)
				{
					List<string> SortedModules = new List<string>();

					SortedModules.AddRange(AvailableModuleGroupsAndNames[CurrentGroup]);

					if(CurrentGroup == "Core" && SortedModules.Count == 1)
					{
						continue;
					}

					SortedModules.Sort();

					bool bCurrentCategoryExpanded = ParamsModuleCategoryExpanded.ContainsKey(CurrentGroup) && ParamsModuleCategoryExpanded[CurrentGroup];

					bCurrentCategoryExpanded = EditorGUILayout.Foldout(bCurrentCategoryExpanded, CurrentGroup);

					if(!ParamsModuleCategoryExpanded.ContainsKey(CurrentGroup))
					{
						ParamsModuleCategoryExpanded.Add(CurrentGroup, bCurrentCategoryExpanded);
					}

					ParamsModuleCategoryExpanded[CurrentGroup] = bCurrentCategoryExpanded;

					if(bCurrentCategoryExpanded)
					{
						EditorGUI.indentLevel += 1;

						IIgorModule CommonModule = ModuleNameToInst.ContainsKey(CurrentGroup + ".Common") ? ModuleNameToInst[CurrentGroup + ".Common"] : null;

						if(CommonModule != null)
						{
							CurrentJobInst.JobCommandLineParams = CommonModule.DrawJobInspectorAndGetEnabledParams(CurrentJobInst.JobCommandLineParams);
						}

						foreach(string CurrentModuleName in SortedModules)
						{
							if(CurrentModuleName == "Common")
							{
								continue;
							}

							string FullModuleName = CurrentGroup + "." + CurrentModuleName;

							IIgorModule CurrentModule = ModuleNameToInst.ContainsKey(FullModuleName) ? ModuleNameToInst[FullModuleName] : null;

							if(CurrentModule == null || !CurrentModule.ShouldDrawInspectorForParams(CurrentJobInst.JobCommandLineParams))
							{
								continue;
							}

							if(!IsModuleExpanded.ContainsKey(CurrentModule.GetModuleName()))
							{
								IsModuleExpanded.Add(CurrentModule.GetModuleName(), false);
							}

							bool bIsExpanded = IsModuleExpanded[CurrentModule.GetModuleName()];

							EditorGUILayout.BeginVertical("box");

							bIsExpanded = EditorGUILayout.Foldout(bIsExpanded, CurrentModule.GetModuleName());

							IsModuleExpanded[CurrentModule.GetModuleName()] = bIsExpanded;

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
			
            EditorGUILayout.EndScrollView();
		}

		public virtual void SaveConfiguration()
		{
			List<IgorPersistentJobConfig> NewJobs = new List<IgorPersistentJobConfig>();

			foreach(IgorPersistentJobConfig Job in Jobs)
			{
				IgorPersistentJobConfig NewJob = new IgorPersistentJobConfig();

				NewJob.JobName = string.Copy(Job.JobName);
				NewJob.JobCommandLineParams = string.Copy(Job.JobCommandLineParams);

				NewJobs.Add(NewJob);
			}

			IgorConfig.GetInstance().JobConfigs = NewJobs;
			IgorConfig.GetInstance().Save();

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

			int NewJobIndex = EditorGUILayout.Popup("Job to configure", CurrentJobIndex, JobNames.ToArray());
            if(NewJobIndex != CurrentJobIndex || JobNames.Count == 1)
            {
			    if(_currentJobIndex == (JobNames.Count - 1))
			    {
				    IgorPersistentJobConfig NewJob = new IgorPersistentJobConfig();

				    NewJob.JobName = "New job";

				    Jobs.Add(NewJob);
			    }
                
                _currentJobIndex = NewJobIndex;
            }
		}

		public virtual void DeleteSelectedJob()
		{
			Jobs.RemoveAt(CurrentJobIndex);

			if(CurrentJobIndex >= Jobs.Count)
			{
				CurrentJobIndex = Jobs.Count - 1;
			}
		}

		public static void TriggerJob(string JobName)
		{
			IgorConfig.TriggerJobByName(JobName, true, false);
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

			bModulesChanged = false;
		}

		public virtual string FilterOutNonTriggerByNameFlags(string OriginalParams)
		{
			string NewParams = "";

			if(IgorUtils.IsBoolParamSet(OriginalParams, IgorCore.SkipUnityUpdateFlag))
			{
				NewParams = IgorUtils.SetBoolParam(NewParams, IgorCore.SkipUnityUpdateFlag, true);
			}

			return NewParams;
		}

		public static string IgorRunner = "IgorRun.py";

		public virtual string GenerateJenkinsJobForParams(string Params, string JobName)
		{
            var path = Path.Combine("Assets", Path.Combine("Editor", Path.Combine("Igor", IgorRunner)));

            if(bTriggerJobByName)
            {
            	Params = "--" + IgorCore.NamedJobFlag + "=\"" + JobName + "\"" + FilterOutNonTriggerByNameFlags(Params);
            }

			string JenkinsString = "python " + path + " " + Params;
		    return JenkinsString;
		}

		public virtual string GenerateCommandLineParams(string Params, string JobName)
		{
            if(bTriggerJobByName)
            {
            	Params = "--" + IgorCore.NamedJobFlag + "=\"" + JobName + "\"" + FilterOutNonTriggerByNameFlags(Params);
            }

		    return Params;
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

			if(File.Exists(EditorMenuOptionsFilePath))
			{
				IgorUtils.DeleteFile(EditorMenuOptionsFilePath);
			}

			File.WriteAllText(EditorMenuOptionsFilePath, FullEditorMenuOptionFile);
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