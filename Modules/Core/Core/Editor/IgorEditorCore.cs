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
	[InitializeOnLoad]
	public class IgorEditorCore : IIgorEditorCore
	{
		static IgorEditorCore()
		{
			Initialize();
		}

		public static void Initialize()
		{
			IgorConfig.DefaultConfigPath = Path.Combine(IgorUpdater.BaseIgorDirectory, IgorConfig.IgorConfigFilename);
			IgorUpdater.HelperDelegates = StaticGetHelperDelegates();
		}

		public virtual IgorHelperDelegateStub GetHelperDelegates()
		{
			return StaticGetHelperDelegates();
		}

		public static IgorHelperDelegateStub StaticGetHelperDelegates()
		{
			IgorHelperDelegateStub ValidDelegateStub = new IgorHelperDelegateStub();

			ValidDelegateStub.bIsValid = true;
			ValidDelegateStub.DeleteFile = IgorRuntimeUtils.DeleteFile;
			ValidDelegateStub.DeleteDirectory = IgorRuntimeUtils.DeleteDirectory;
			ValidDelegateStub.GetTypesInheritFromIIgorEditorCore = IgorRuntimeUtils.GetTypesInheritFrom<IIgorEditorCore>;
			ValidDelegateStub.IgorJobConfig_SetWasMenuTriggered = IgorJobConfig.SetWasMenuTriggered;
			ValidDelegateStub.IgorJobConfig_GetWasMenuTriggered = IgorJobConfig.GetWasMenuTriggered;
			ValidDelegateStub.IgorJobConfig_IsBoolParamSet = IgorJobConfig.IsBoolParamSet;
			ValidDelegateStub.IgorJobConfig_SetBoolParam = IgorJobConfig.SetBoolParam;

			return ValidDelegateStub;
		}

		public static void UpdateAndRunJob()
		{
            IgorDebug.CoreLog("UpdateAndRunJob invoked from command line.");

			IgorJobConfig.SetBoolParam("updatebeforebuild", true);

			IgorConfigWindow.OpenOrGetConfigWindow();

		    bool DidUpdate = IgorUpdater.CheckForUpdates(false, false, true);
		    if(!DidUpdate)
		    {
		        IgorDebug.CoreLog("Igor did not need to update, running job.");

	            EditorHandleJobStatus(IgorCore.RunJob());
		    }
		    else
		    {
		        IgorDebug.CoreLog("Igor needed to update, waiting for re-compile to run a job...");
		    }
		}

		public static void EditorHandleJobStatus(IgorCore.JobReturnStatus Status)
		{
			if(Status.bDone)
			{
				if(IgorAssert.HasJobFailed())
				{
					IgorDebug.CoreLogError("Job failed!");
				}
				else
				{
					IgorDebug.CoreLog("Job's done!");
				}
			    
                float time = IgorUtils.PlayJobsDoneSound();
			    System.Threading.Thread t = new System.Threading.Thread(() => WaitToExit(time));
			    t.Start();
                
                while(t.IsAlive)
                { }
			}

			if(!Status.bWasStartedManually && (Status.bFailed || Status.bDone))
			{
				if(Status.bFailed)
				{
					EditorApplication.Exit(-1);
				}
				else
				{
					EditorApplication.Exit(0);
				}
			}
		}

		public static void ContinueRunningJob()
		{
			EditorHandleJobStatus(IgorCore.RunJob());
		}

		public virtual List<string> GetEnabledModuleNames()
		{
			return IgorCore.StaticGetEnabledModuleNames();
		}

		public void RunJobInst()
		{
			EditorHandleJobStatus(IgorCore.RunJob());
		}

		public static void CommandLineRunJob()
		{
            IgorDebug.CoreLog("CommandLineRunJob invoked from command line.");

			IgorConfigWindow.OpenOrGetConfigWindow();

			EditorHandleJobStatus(IgorCore.RunJob());
		}

	    static void WaitToExit(float time)
	    {
	        int Seconds = Mathf.FloorToInt(time);
            int Milliseconds = Mathf.FloorToInt((time - Seconds) * 1000f);
	            
            System.DateTime WaitTime = System.DateTime.Now + new TimeSpan(0, 0, 0, Seconds, Milliseconds);
            while(System.DateTime.Now < WaitTime)
	        { }
	    }
	}
}