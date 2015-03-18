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
	public struct StepID : IComparable<StepID>
	{
		public string StepName;
		public int StepPriority;

		public StepID(string Name, int Priority)
		{
			StepName = Name;
			StepPriority = Priority;
		}

		public int CompareTo(StepID Other)
		{
			return StepPriority.CompareTo(Other.StepPriority);
		}
	}

	public class IgorCore : IIgorCore
	{
		public delegate bool JobStepFunc(); // Return true if the function finished

		public class JobStep
		{
			public IIgorModule ModuleInst;
			public JobStepFunc StepFunction;

			public JobStep(IIgorModule Module, JobStepFunc Function)
			{
				ModuleInst = Module;
				StepFunction = Function;
			}
		}

		public static IIgorLogger Logger = null;

		public static List<IIgorModule> EnabledModules = new List<IIgorModule>();
		public static List<IIgorModule> ActiveModulesForJob = new List<IIgorModule>();
		public static Dictionary<StepID, List<JobStep>> JobSteps = new Dictionary<StepID, List<JobStep>>();

		public List<string> GetEnabledModuleNames()
		{
			return IgorConfig.GetInstance().GetEnabledModuleNames();
		}

		public static List<string> StaticGetEnabledModuleNames()
		{
			return IgorConfig.GetInstance().GetEnabledModuleNames();
		}

		public static void RegisterAllModules()
		{
			List<Type> ModuleTypes = IgorUtils.GetTypesInheritFrom<IIgorModule>();

			foreach(Type CurrentType in ModuleTypes)
			{
				IIgorModule CurrentModule = (IIgorModule)Activator.CreateInstance(CurrentType);

				if(CurrentModule != null)
				{
					CurrentModule.RegisterModule();
				}
			}

			List<Type> LoggerTypes = IgorUtils.GetTypesInheritFrom<IIgorLogger>();

			foreach(Type CurrentType in LoggerTypes)
			{
				IIgorLogger CurrentLogger = (IIgorLogger)Activator.CreateInstance(CurrentType);

				if(Logger == null || Logger.LoggerPriority() < CurrentLogger.LoggerPriority())
				{
					Logger = CurrentLogger;
				}
			}
		}

		public static void ProcessArgs()
		{
			ActiveModulesForJob.Clear();

			foreach(IIgorModule CurrentModule in EnabledModules)
			{
				CurrentModule.ProcessArgs();
			}
		}

		public static void UpdateAndRunJob()
		{
			IgorJobConfig.SetBoolParam("updatebeforebuild", true);

			IgorConfigWindow.OpenOrGetConfigWindow();

			if(!IgorUpdater.CheckForUpdates())
			{
				RunJob();
			}
		}

		public void RunJobInst()
		{
			IgorCore.RunJob();
		}

		public static void CommandLineRunJob()
		{
			RunJob();
		}

		public static void RunJob(bool bFromMenu = false)
		{
			bool bWasStartedInEditor = false;
			bool bThrewException = false;
			bool bDone = false;

			try
			{
				if(!IgorJobConfig.GetIsRunning())
				{
					Log("Job is starting!");
				}

				if(IgorJobConfig.GetWasMenuTriggered())
				{
					bWasStartedInEditor = true;
				}
				else
				{
					bWasStartedInEditor = bFromMenu;

					IgorJobConfig.SetWasMenuTriggered(bWasStartedInEditor);
				}

				IgorConfigWindow.OpenOrGetConfigWindow();

				if(!IgorJobConfig.GetIsRunning() || EnabledModules.Count == 0)
				{
					RegisterAllModules();
				}

				if(!IgorJobConfig.GetIsRunning() || ActiveModulesForJob.Count == 0)
				{
					ProcessArgs();
				}

				if(ExecuteSteps())
				{
					IgorJobConfig.SetIsRunning(false);

					bDone = true;
				}
			}
			catch(Exception e)
			{
				LogError("Caught exception while running the job.  Exception is " + (e == null ? "NULL exception!" : e.ToString()));

				bThrewException = true;
			}
			finally
			{
				if(bThrewException || bDone)
				{
					Cleanup();
				}
			}

			if(bDone)
			{
				Log("Job's done!");
			}

			if(!bWasStartedInEditor && (bThrewException || bDone))
			{
				if(bThrewException)
				{
					EditorApplication.Exit(-1);
				}
				else
				{
					EditorApplication.Exit(0);
				}
			}
		}

		public static void Cleanup()
		{
			ActiveModulesForJob.Clear();

			JobSteps.Clear();

			IgorJobConfig.Cleanup();
		}

		public static void RegisterNewModule(IIgorModule NewModule)
		{
			if(StaticGetEnabledModuleNames().Contains(NewModule.GetModuleName()))
			{
				bool bFound = false;

				foreach(IIgorModule CurrentModule in EnabledModules)
				{
					if(CurrentModule.GetModuleName() == NewModule.GetModuleName())
					{
						bFound = true;
					}
				}

				if(!bFound)
				{
					EnabledModules.Add(NewModule);
				}
			}
		}

		public static void SetModuleActiveForJob(IIgorModule NewModule)
		{
			if(EnabledModules.Contains(NewModule) && !ActiveModulesForJob.Contains(NewModule))
			{
				bool bFound = false;
				
				foreach(IIgorModule CurrentModule in ActiveModulesForJob)
				{
					if(CurrentModule.GetModuleName() == NewModule.GetModuleName())
					{
						bFound = true;
					}
				}

				if(!bFound)
				{
					ActiveModulesForJob.Add(NewModule);
				}
			}
		}

		public static void RegisterJobStep(StepID CurrentStep, IIgorModule Module, JobStepFunc StepFunction)
		{
			List<JobStep> NewSteps = new List<JobStep>();
			StepID Priority = new StepID();

			foreach(KeyValuePair<StepID, List<JobStep>> CurrentPriority in JobSteps)
			{
				if(CurrentPriority.Key.StepPriority == CurrentStep.StepPriority)
				{
					NewSteps = CurrentPriority.Value;
					Priority = CurrentPriority.Key;

					break;
				}
			}

			NewSteps.Add(new JobStep(Module, StepFunction));

			if(JobSteps.ContainsKey(Priority))
			{
				JobSteps[Priority] = NewSteps;
			}
			else
			{
				JobSteps.Add(CurrentStep, NewSteps);
			}
		}

		public static bool ExecuteSteps()
		{
			List<StepID> SortedSteps = new List<StepID>(JobSteps.Keys);

			SortedSteps.Sort();

			IgorJobConfig.SetIsRunning(true);

			int StartingPriority = IgorJobConfig.GetLastPriority();
			int StartingIndexInPriority = IgorJobConfig.GetLastIndexInPriority();

			foreach(StepID CurrentStep in SortedSteps)
			{
				if(CurrentStep.StepPriority > StartingPriority)
				{
					IgorJobConfig.SetLastPriority(CurrentStep.StepPriority - 1);

					int LastIndex = 0;

					List<JobStep> CurrentStepFuncs = JobSteps[CurrentStep];

					foreach(JobStep CurrentFunction in CurrentStepFuncs)
					{
						if(LastIndex > StartingIndexInPriority)
						{
							if(CurrentFunction.StepFunction())
							{
								IgorJobConfig.SetLastIndexInPriority(LastIndex);
							}

							return false;
						}

						++LastIndex;
					}

					StartingIndexInPriority = -1;
				}
			}

			return true;
		}

		protected static void Log(string Message)
		{
			if(Logger != null)
			{
				Logger.Log(Message);
			}
			else
			{
				Debug.Log(Message);
			}
		}

		public static void Log(IIgorModule Module, string Message)
		{
			if(Logger != null)
			{
				Logger.Log(Module, Message);
			}
			else
			{
				Debug.Log(Module + " : " + Message);
			}
		}

		protected static void LogWarning(string Message)
		{
			if(Logger != null)
			{
				Logger.LogWarning(Message);
			}
			else
			{
				Debug.LogWarning(Message);
			}
		}

		public static void LogWarning(IIgorModule Module, string Message)
		{
			if(Logger != null)
			{
				Logger.LogWarning(Module, Message);
			}
			else
			{
				Debug.LogWarning(Module + " : " + Message);
			}
		}

		protected static void LogError(string Message)
		{
			if(Logger != null)
			{
				Logger.LogError(Message);
			}
			else
			{
				Debug.LogError(Message);
			}
		}

		public static void LogError(IIgorModule Module, string Message)
		{
			if(Logger != null)
			{
				Logger.LogError(Module, Message);
			}
			else
			{
				Debug.LogError(Module + " : " + Message);
			}
		}

		protected static void CriticalError(string Message)
		{
			if(Logger != null)
			{
				Logger.CriticalError(Message);
			}
			else
			{
				Debug.LogError(Message);

				throw new UnityException(Message);
			}
		}

		public static void CriticalError(IIgorModule Module, string Message)
		{
			if(Logger != null)
			{
				Logger.CriticalError(Module, Message);
			}
			else
			{
				Debug.LogError(Module + " : " + Message);

				throw new UnityException(Module + " : " + Message);
			}
		}
	}
}