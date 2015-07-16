#if IGOR_RUNTIME || UNITY_EDITOR
using UnityEngine;
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
		public static string CopyFromSyncFlag = "copyfromsync";
		public static string CopyToLocalDirFlag = "copytolocaldir";

		public static StepID CopyFromSyncStep = new StepID("Copy From BitTorrent Sync", 200);
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
			if(IgorDistributionCommon.RunDistributionStepsThisJob() &&
				((IgorJobConfig.IsBoolParamSet(CopyToSyncExpEnabledFlag) && GetParamOrConfigString(CopyToSyncExplicitFlag) != "") ||
			    (IgorJobConfig.IsBoolParamSet(CopyToSyncEnvEnabledFlag) &&
			   	 (GetParamOrConfigString(CopyToSyncEnvFlag) != "" && IgorRuntimeUtils.GetEnvVariable(GetParamOrConfigString(CopyToSyncEnvFlag)) != ""))) &&
				GetParamOrConfigString(CopyToSyncFilenameFlag) != "")
			{
				IgorCore.SetModuleActiveForJob(this);

				if(IgorJobConfig.IsBoolParamSet(CopyFromSyncFlag))
				{
					StepHandler.RegisterJobStep(CopyFromSyncStep, this, CopyFromSync);
				}
				else
				{
					StepHandler.RegisterJobStep(CopyToSyncStep, this, CopyToSync);
				}
			}
		}

#if UNITY_EDITOR
		public override string DrawJobInspectorAndGetEnabledParams(string CurrentParams)
		{
			string EnabledParams = CurrentParams;

			DrawStringConfigParam(ref EnabledParams, "Destination Filename", CopyToSyncFilenameFlag);
			DrawBoolParam(ref EnabledParams, "Use Explicit Base Path", CopyToSyncExpEnabledFlag);
			DrawStringConfigParam(ref EnabledParams, "Explicit Base Path", CopyToSyncExplicitFlag);
			DrawBoolParam(ref EnabledParams, "Use Environment Variable Base Path", CopyToSyncEnvEnabledFlag);
			DrawStringConfigParam(ref EnabledParams, "Environment Base Path Variable Name", CopyToSyncEnvFlag);

			if(DrawBoolParam(ref EnabledParams, "Copy from Sync Path", CopyFromSyncFlag))
			{
				DrawStringConfigParam(ref EnabledParams, "Copy from Sync Destination Local Directory", CopyToLocalDirFlag);
			}

			return EnabledParams;
		}
#endif // UNITY_EDITOR

		public virtual bool CopyToSync()
		{
			return CopyToFromSync(true);
		}

		public virtual bool CopyFromSync()
		{
			return CopyToFromSync(false);
		}

		public virtual bool CopyToFromSync(bool bToSync)
		{
			string LocalFile = "";

			if(bToSync)
			{
				List<string> BuiltProducts = IgorCore.GetModuleProducts();

				IgorAssert.EnsureTrue(this, BuiltProducts.Count == 1, "This module requires exactly one built file, but we found " + BuiltProducts.Count + " instead.  Please make sure you've enabled a package step prior to this one.");

				if(BuiltProducts.Count > 0)
				{
					LocalFile = BuiltProducts[0];
				}
			}
			else
			{
				LocalFile = GetParamOrConfigString(CopyToLocalDirFlag, "", Path.GetFullPath("."), false);
			}

			if(IgorAssert.EnsureTrue(this, !bToSync || File.Exists(LocalFile), "BitTorrent Sync copy was told to copy file " + LocalFile + ", but the file doesn't exist!"))
			{
				string SyncFile = "";

				if(IgorJobConfig.IsBoolParamSet(CopyToSyncExpEnabledFlag))
				{
					SyncFile = GetParamOrConfigString(CopyToSyncExplicitFlag, "BitTorrent Sync copy to sync explicit is enabled, but the path isn't set.");
				}

				if(SyncFile == "" && IgorJobConfig.IsBoolParamSet(CopyToSyncEnvEnabledFlag))
				{
					string EnvVariable = GetParamOrConfigString(CopyToSyncEnvFlag, "BitTorrent Sync copy to sync based on environment variable is enabled, but the env variable name isn't set.");

					if(EnvVariable == "")
					{
						return true;
					}

					SyncFile = IgorRuntimeUtils.GetEnvVariable(EnvVariable);

					if(!IgorAssert.EnsureTrue(this, SyncFile != "", "The BitTorrent Sync root path environment variable " + EnvVariable + " isn't set."))
					{
						return true;
					}
				}

				string SyncFilename = GetParamOrConfigString(CopyToSyncFilenameFlag, (bToSync ?
					"BitTorrent Sync copy to sync destination filename isn't set." : "BitTorrent Sync copy from sync source filename isn't set."));

				if(SyncFilename == "")
				{
					return true;
				}

				SyncFile = Path.Combine(SyncFile, SyncFilename);

				if(bToSync)
				{
					if(File.Exists(SyncFile))
					{
						IgorRuntimeUtils.DeleteFile(SyncFile);
					}

					File.Copy(LocalFile, SyncFile);

					Log("File " + LocalFile + " copied to requested location " + SyncFile + " for BitTorrent Sync uploading.");
				}
				else
				{
					LocalFile = Path.Combine(LocalFile, Path.GetFileName(SyncFile));

					if(File.Exists(LocalFile))
					{
						IgorRuntimeUtils.DeleteFile(LocalFile);
					}

					File.Copy(SyncFile, LocalFile);

					Log("File " + SyncFile + " copied from the BitTorrent Sync share to requested location " + LocalFile + ".");

					List<string> NewProducts = new List<string>();

					NewProducts.Add(LocalFile);

					IgorCore.SetNewModuleProducts(NewProducts);
				}
			}

			return true;
		}
	}
}

#endif // IGOR_RUNTIME || UNITY_EDITOR
