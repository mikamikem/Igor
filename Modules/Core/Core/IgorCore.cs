using UnityEngine;
using UnityEditor;
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

	public class IgorCore : IIgorCore, IIgorStepHandler
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

		public static string NamedJobFlag = "ExecuteJob";

		public List<string> GetEnabledModuleNames()
		{
			return IgorConfig.GetInstance().GetEnabledModuleNames();
		}

		public static List<string> StaticGetEnabledModuleNames()
		{
			return IgorConfig.GetInstance().GetEnabledModuleNames();
		}

        static List<Type> _ModuleTypes;
        static List<Type> ModuleTypes
        {
            get
            {
                if(_ModuleTypes == null)
                    _ModuleTypes = IgorUtils.GetTypesInheritFrom<IIgorModule>();
                return _ModuleTypes;
            }
        }

        static List<Type> _LoggerTypes;
        static List<Type> LoggerTypes
        {
            get
            {
                if(_LoggerTypes == null)
                    _LoggerTypes = IgorUtils.GetTypesInheritFrom<IIgorLogger>();
                return _LoggerTypes;
            }
        }

		public static void RegisterAllModules()
		{
			foreach(Type CurrentType in ModuleTypes)
			{
				IIgorModule CurrentModule = (IIgorModule)Activator.CreateInstance(CurrentType);

				if(CurrentModule != null)
				{
					CurrentModule.RegisterModule();
				}
			}

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
			IgorUpdater.FindCore();

			if(IgorUpdater.Core != null)
			{
				IgorCore CoreInst = (IgorCore)IgorUpdater.Core;

				if(CoreInst != null)
				{
					ActiveModulesForJob.Clear();

					foreach(IIgorModule CurrentModule in EnabledModules)
					{
						CurrentModule.ProcessArgs(CoreInst);
					}
				}
				else
				{
					CriticalError("Core was found, but couldn't be converted to IgorCore.  Did you create a custom one?  You may need to fix up IgorCore::ProcessArgs()!");
				}
			}
			else
			{
				CriticalError("Core not found so we bailed out of processing arguments!");
			}
		}

		public static void UpdateAndRunJob()
		{
            Log("UpdateAndRunJob invoked from command line.");

			IgorJobConfig.SetBoolParam("updatebeforebuild", true);

			IgorConfigWindow.OpenOrGetConfigWindow();

		    bool DidUpdate = IgorUpdater.CheckForUpdates(false, false, true);
		    if(!DidUpdate)
		    {
		        Log("Igor did not need to update, running job.");

	            RunJob();
		    }
		    else
		    {
		        Log("Igor needed to update, waiting for re-compile to run a job...");
		    }
		}

		public void RunJobInst()
		{
			IgorCore.RunJob();
		}

		public static void CommandLineRunJob()
		{
            Log("CommandLineRunJob invoked from command line.");

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

					CheckForNamedJobFlag();
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
			    
                float time = IgorUtils.PlayJobsDoneSound();
			    System.Threading.Thread t = new System.Threading.Thread(() => WaitToExit(time));
			    t.Start();
                
                while(t.IsAlive)
                { }
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

	    static void WaitToExit(float time)
	    {
	        int Seconds = Mathf.FloorToInt(time);
            int Milliseconds = Mathf.FloorToInt((time - Seconds) * 1000f);
	            
            System.DateTime WaitTime = System.DateTime.Now + new TimeSpan(0, 0, 0, Seconds, Milliseconds);
            while(System.DateTime.Now < WaitTime)
	        { }
	    }

		public static void Cleanup()
		{
            foreach(IIgorModule module in ActiveModulesForJob)
            {
                module.PostJobCleanup();
            }

			ActiveModulesForJob.Clear();

			JobSteps.Clear();

			IgorJobConfig.Cleanup();
		}

		public static bool RegisterNewModule(IIgorModule NewModule)
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
                    return true;
				}
			}

            return false;
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

		public virtual void RegisterJobStep(StepID CurrentStep, IIgorModule Module, JobStepFunc StepFunction)
		{
			StaticRegisterJobStep(CurrentStep, Module, StepFunction);
		}

		protected static void StaticRegisterJobStep(StepID CurrentStep, IIgorModule Module, JobStepFunc StepFunction)
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

		public static void CheckForNamedJobFlag()
		{
			if(IgorJobConfig.IsStringParamSet(NamedJobFlag))
			{
				string JobToStart = IgorJobConfig.GetStringParam(NamedJobFlag);

				foreach(IgorPersistentJobConfig Job in IgorConfig.GetInstance().JobConfigs)
				{
					if(Job.JobName == JobToStart)
					{
						IgorJobConfig ConfigInst = IgorJobConfig.GetConfig();
						
						ConfigInst.Persistent = Job;

						ConfigInst.Save(IgorJobConfig.IgorJobConfigPath);

						Log("Starting named job " + JobToStart + ".");

						return;
					}
				}

				LogError("Couldn't find named job " + JobToStart + "!");
			}
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