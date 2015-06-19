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
			if(((IgorJobConfig.IsBoolParamSet(CopyToSyncExpEnabledFlag) && GetParamOrConfigString(CopyToSyncExplicitFlag) != "") ||
			    (IgorJobConfig.IsBoolParamSet(CopyToSyncEnvEnabledFlag) &&
			   	 (GetParamOrConfigString(CopyToSyncEnvFlag) != "" && IgorUtils.GetEnvVariable(GetParamOrConfigString(CopyToSyncEnvFlag)) != ""))) &&
				GetParamOrConfigString(CopyToSyncFilenameFlag) != "")
			{
				IgorCore.SetModuleActiveForJob(this);
				StepHandler.RegisterJobStep(CopyToSyncStep, this, CopyToSync);
			}
		}

		public override string DrawJobInspectorAndGetEnabledParams(string CurrentParams)
		{
			string EnabledParams = CurrentParams;

			DrawStringConfigParam(ref EnabledParams, "Destination Filename", CopyToSyncFilenameFlag);
			DrawBoolParam(ref EnabledParams, "Use Explicit Base Path", CopyToSyncExpEnabledFlag);
			DrawStringConfigParam(ref EnabledParams, "Explicit Base Path", CopyToSyncExplicitFlag);
			DrawBoolParam(ref EnabledParams, "Use Environment Variable Base Path", CopyToSyncEnvEnabledFlag);
			DrawStringConfigParam(ref EnabledParams, "Environment Base Path Variable Name", CopyToSyncEnvFlag);

			return EnabledParams;
		}

		public virtual bool CopyToSync()
		{
			List<string> BuiltProducts = IgorBuildCommon.GetBuildProducts();

			IgorAssert.EnsureTrue(this, BuiltProducts.Count == 1, "This module requires exactly one built file, but we found " + BuiltProducts.Count + " instead.  Please make sure you've enabled a package step prior to this one.");

			string FileToCopy = "";

			if(BuiltProducts.Count > 0)
			{
				FileToCopy = BuiltProducts[0];
			}

			if(IgorAssert.EnsureTrue(this, File.Exists(FileToCopy), "BitTorrent Sync copy was told to copy file " + FileToCopy + ", but the file doesn't exist!"))
			{
				string DestinationFile = "";

				if(IgorJobConfig.IsBoolParamSet(CopyToSyncExpEnabledFlag))
				{
					DestinationFile = GetParamOrConfigString(CopyToSyncExplicitFlag, "BitTorrent Sync copy to sync explicit is enabled, but the path isn't set.");
				}

				if(DestinationFile == "" && IgorJobConfig.IsBoolParamSet(CopyToSyncEnvEnabledFlag))
				{
					string EnvVariable = GetParamOrConfigString(CopyToSyncEnvFlag, "BitTorrent Sync copy to sync based on environment variable is enabled, but the env variable name isn't set.");

					if(EnvVariable == "")
					{
						return true;
					}

					DestinationFile = IgorUtils.GetEnvVariable(EnvVariable);

					if(!IgorAssert.EnsureTrue(this, DestinationFile != "", "The BitTorrent Sync root path environment variable " + EnvVariable + " isn't set."))
					{
						return true;
					}
				}

				string DestinationFilename = GetParamOrConfigString(CopyToSyncFilenameFlag, "BitTorrent Sync copy to sync destination filename isn't set.");

				if(DestinationFilename == "")
				{
					return true;
				}

				DestinationFile = Path.Combine(DestinationFile, DestinationFilename);

				if(File.Exists(DestinationFile))
				{
					IgorUtils.DeleteFile(DestinationFile);
				}

				File.Copy(FileToCopy, DestinationFile);

				Log("File " + FileToCopy + " copied to requested location " + DestinationFile + " for BitTorrent Sync uploading.");
			}

			return true;
		}
	}
}