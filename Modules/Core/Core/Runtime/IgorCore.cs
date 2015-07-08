using UnityEngine;
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
	public class IgorCore : IIgorCore, IIgorStepHandler
	{
		public class JobStep
		{
			public IIgorModule ModuleInst;
			public IgorRuntimeUtils.JobStepFunc StepFunction;

			public JobStep(IIgorModule Module, IgorRuntimeUtils.JobStepFunc Function)
			{
				ModuleInst = Module;
				StepFunction = Function;
			}
		}

		public static List<IIgorModule> EnabledModules = new List<IIgorModule>();
		public static List<IIgorModule> ActiveModulesForJob = new List<IIgorModule>();
		public static Dictionary<StepID, List<JobStep>> JobSteps = new Dictionary<StepID, List<JobStep>>();

		public static string NamedJobFlag = "ExecuteJob";
		protected static string ProductsFlag = "moduleproducts";

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
                    _ModuleTypes = IgorRuntimeUtils.GetTypesInheritFrom<IIgorModule>();
                return _ModuleTypes;
            }
        }

        public static IIgorCore RuntimeCore = null;

		protected static List<string> CurrentModuleProducts = new List<string>();

        public static void InitializeRuntimeCoreIfNeeded()
        {
        	if(RuntimeCore == null)
        	{
        		List<Type> RuntimeCoreTypes = IgorRuntimeUtils.GetTypesInheritFrom<IIgorCore>();

				if(RuntimeCoreTypes.Count > 0)
        		{
					RuntimeCore = (IIgorCore)Activator.CreateInstance(RuntimeCoreTypes[0]);
        		}
        	}
        }

		public static void SetNewModuleProducts(List<string> NewModuleProducts)
		{
			CurrentModuleProducts.Clear();
			CurrentModuleProducts.AddRange(NewModuleProducts);

			string CombinedProducts = "";

			foreach(string CurrentProduct in NewModuleProducts)
			{
				CombinedProducts += (CombinedProducts.Length > 0 ? "," : "") + CurrentProduct;
			}

			IgorJobConfig.SetStringParam(ProductsFlag, CombinedProducts);
		}

		public static List<string> GetModuleProducts()
		{
			if(CurrentModuleProducts.Count == 0)
			{
				string CombinedProducts = IgorJobConfig.GetStringParam(ProductsFlag);

				CurrentModuleProducts.Clear();
				CurrentModuleProducts.AddRange(CombinedProducts.Split(','));
			}

			return CurrentModuleProducts;
		}

		public virtual void RegisterJobStep(StepID CurrentStep, IIgorModule Module, IgorRuntimeUtils.JobStepFunc StepFunction)
		{
			StaticRegisterJobStep(CurrentStep, Module, StepFunction);
		}

		protected static void StaticRegisterJobStep(StepID CurrentStep, IIgorModule Module, IgorRuntimeUtils.JobStepFunc StepFunction)
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
		}

		public static void ProcessArgs()
		{
			InitializeRuntimeCoreIfNeeded();

			if(RuntimeCore != null)
			{
				if(typeof(IgorCore).IsAssignableFrom(RuntimeCore.GetType()))
				{
					IgorCore CoreInst = (IgorCore)RuntimeCore;

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
						IgorDebug.CoreCriticalError("Core could not be case to type IgorCore!");
					}
				}
				else
				{
					IgorDebug.CoreCriticalError("Core is not of type IgorCore.  Did you make your own?  You may need to change how this works.");
				}
			}
			else
			{
				IgorDebug.CoreCriticalError("Core not found so we bailed out of processing arguments!");
			}
		}

		public struct JobReturnStatus
		{
			public bool bDone;
			public bool bFailed;
			public bool bWasStartedManually;
		}

		public static JobReturnStatus RunJob(bool bFromMenu = false)
		{
			bool bWasStartedManually = false;
			bool bThrewException = false;
			bool bDone = false;

			try
			{
				if(!IgorJobConfig.GetIsRunning())
				{
					IgorDebug.CoreLog("Job is starting!");

					IgorAssert.StartJob();

					CheckForNamedJobFlag();
				}

				if(IgorJobConfig.GetWasMenuTriggered())
				{
					bWasStartedManually = true;
				}
				else
				{
					bWasStartedManually = bFromMenu;

					IgorJobConfig.SetWasMenuTriggered(bWasStartedManually);
				}

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
				IgorDebug.CoreLogError("Caught exception while running the job.  Exception is " + (e == null ? "NULL exception!" : e.ToString()));

				bThrewException = true;
			}
			finally
			{
				if(bThrewException || bDone)
				{
					Cleanup();
				}
			}

			JobReturnStatus NewStatus = new JobReturnStatus();

			NewStatus.bDone = bDone;
			NewStatus.bFailed = bThrewException || IgorAssert.HasJobFailed();
			NewStatus.bWasStartedManually = bWasStartedManually;

			return NewStatus;
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

		public static bool ExecuteSteps()
		{
			if(IgorAssert.HasJobFailed())
			{
				IgorDebug.CoreLogError("Job failed so we are bailing out early and not finishing the remaining steps!");
			}

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

						IgorDebug.CoreLog("Starting named job " + JobToStart + ".");

						return;
					}
				}

				IgorDebug.CoreLogError("Couldn't find named job " + JobToStart + "!");
			}
		}
	}
}