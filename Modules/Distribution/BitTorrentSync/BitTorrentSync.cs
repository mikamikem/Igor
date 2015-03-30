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
	public class BittorrentSync : IgorModuleBase
	{
		public static string CopyToSyncEnvEnabledFlag = "bittorrentsyncenvenabled";
		public static string CopyToSyncEnvFlag = "bittorrentsyncenv";
		public static string CopyToSyncExpEnabledFlag = "bittorrentsyncexpenabled";
		public static string CopyToSyncExplicitFlag = "bitttorrentsyncpath";
		public static string CopyToSyncFilenameFlag = "bittorrentsyncfile";

		public static StepID CopyToSyncStep = new StepID("Copy To BitTorrent Sync", 1200);

		public override string GetModuleName()
		{
			return "Distribution.BitTorrentSync";
		}

		public override void RegisterModule()
		{
			IgorCore.RegisterNewModule(this);
		}

		public override void ProcessArgs(IIgorStepHandler StepHandler)
		{
			if(((IgorJobConfig.IsBoolParamSet(CopyToSyncExpEnabledFlag) && IgorJobConfig.GetStringParam(CopyToSyncExplicitFlag) != "") ||
			    (IgorJobConfig.IsBoolParamSet(CopyToSyncEnvEnabledFlag) &&
			   	 (IgorJobConfig.GetStringParam(CopyToSyncEnvFlag) != "" && IgorUtils.GetEnvVariable(IgorJobConfig.GetStringParam(CopyToSyncEnvFlag)) != ""))) &&
				IgorJobConfig.GetStringParam(CopyToSyncFilenameFlag) != "")
			{
				IgorCore.SetModuleActiveForJob(this);
				StepHandler.RegisterJobStep(CopyToSyncStep, this, CopyToSync);
			}
		}

		public override string DrawJobInspectorAndGetEnabledParams(string CurrentParams)
		{
			string EnabledParams = CurrentParams;

			DrawStringParam(ref EnabledParams, "Destination Filename", CopyToSyncFilenameFlag);
			DrawBoolParam(ref EnabledParams, "Use Explicit Base Path", CopyToSyncExpEnabledFlag);
			DrawStringParam(ref EnabledParams, "Explicit Base Path", CopyToSyncExplicitFlag);
			DrawBoolParam(ref EnabledParams, "Use Environment Variable Base Path", CopyToSyncEnvEnabledFlag);
			DrawStringParam(ref EnabledParams, "Environment Base Path Variable Name", CopyToSyncEnvFlag);

			return EnabledParams;
		}

		public virtual bool CopyToSync()
		{
			List<string> BuiltProducts = IgorBuildCommon.GetBuildProducts();

			if(BuiltProducts.Count != 1)
			{
				LogError("This module requires exactly one built file, but we found " + BuiltProducts.Count + " instead.  Please make sure you've enabled a package step prior to this one.");
			}

			string FileToCopy = "";

			if(BuiltProducts.Count > 0)
			{
				FileToCopy = BuiltProducts[0];
			}

			if(File.Exists(FileToCopy))
			{
				string DestinationFile = "";

				if(IgorJobConfig.IsBoolParamSet(CopyToSyncExpEnabledFlag))
				{
					DestinationFile = IgorJobConfig.GetStringParam(CopyToSyncExplicitFlag);
				}

				if(DestinationFile == "" && IgorJobConfig.IsBoolParamSet(CopyToSyncEnvEnabledFlag))
				{
					string EnvVariable = IgorJobConfig.GetStringParam(CopyToSyncEnvFlag);

					if(EnvVariable == "")
					{
						LogError("The name of the BitTorrent Sync in the job config isn't set.");

						return true;
					}

					DestinationFile = IgorUtils.GetEnvVariable(EnvVariable);

					if(DestinationFile == "")
					{
						LogError("The BitTorrent Sync root path environment variable " + EnvVariable + " isn't set.");

						return true;
					}
				}

				if(IgorJobConfig.GetStringParam(CopyToSyncFilenameFlag) == "")
				{
					LogError("You need to set the BitTorrent Sync destination filename.");

					return true;
				}

				DestinationFile = Path.Combine(DestinationFile, IgorJobConfig.GetStringParam(CopyToSyncFilenameFlag));

				if(File.Exists(DestinationFile))
				{
					File.Delete(DestinationFile);
				}

				File.Copy(FileToCopy, DestinationFile);

				Log("File copied to requested location for BitTorrent Sync uploading.");
			}

			return true;
		}
	}
}