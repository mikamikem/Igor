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
	public class JenkinsFTP : IgorModuleBase
	{
		public static string UploadToFTPNoEnvFlag = "jenkinsuploadftpalways";
		public static string UploadToFTPEnvToggleFlag = "jenkinsuploadftpenvtoggle";
		public static string UploadToFTPFlag = "jenkinsuploadftpfilename";

		public static StepID UploadToFTPStep = new StepID("Jenkins Upload To FTP", 1100);

		public override string GetModuleName()
		{
			return "Distribution.JenkinsFTP";
		}

		public override void RegisterModule()
		{
			IgorCore.RegisterNewModule(this);
		}

		public override void ProcessArgs(IIgorStepHandler StepHandler)
		{
			bool bStepRegistered = false;

			if(IgorDistributionCommon.RunDistributionStepsThisJob())
			{
				if(IgorJobConfig.GetStringParam(UploadToFTPFlag) != "" &&
					(IgorJobConfig.IsBoolParamSet(UploadToFTPNoEnvFlag) ||
						(IgorJobConfig.GetStringParam(UploadToFTPEnvToggleFlag) != "" && IgorRuntimeUtils.GetEnvVariable(IgorJobConfig.GetStringParam(UploadToFTPEnvToggleFlag)) == "true")))
				{
					StepHandler.RegisterJobStep(UploadToFTPStep, this, UploadToFTP);

					bStepRegistered = true;
				}

				if(IgorJobConfig.IsBoolParamSet(UploadToFTPNoEnvFlag) || IgorJobConfig.GetStringParam(UploadToFTPEnvToggleFlag) != "")
				{
					StepHandler.RegisterJobStep(IgorBuildCommon.PreBuildCleanupStep, this, Cleanup);

					bStepRegistered = true;
				}

				if(bStepRegistered)
				{
					IgorCore.SetModuleActiveForJob(this);
				}
			}
		}

		public override string DrawJobInspectorAndGetEnabledParams(string CurrentParams)
		{
			string EnabledParams = CurrentParams;

			DrawBoolParam(ref EnabledParams, "Always Upload", UploadToFTPNoEnvFlag);
			DrawStringConfigParam(ref EnabledParams, "FTP Filename", UploadToFTPFlag);
			DrawStringConfigParam(ref EnabledParams, "Environment Variable Toggle", UploadToFTPEnvToggleFlag);

			return EnabledParams;
		}

		public virtual bool Cleanup()
		{
			string DestinationFile = GetParamOrConfigString(UploadToFTPFlag, "Destination file for JenkinsFTP isn't set so we can't clean it up.");

			if(File.Exists(DestinationFile))
			{
				IgorRuntimeUtils.DeleteFile(DestinationFile);
			}

			return true;
		}

		public virtual bool UploadToFTP()
		{
			List<string> BuiltProducts = IgorCore.GetModuleProducts();

			IgorAssert.EnsureTrue(this, BuiltProducts.Count == 1, "This module requires exactly one built file, but we found " + BuiltProducts.Count + " instead.  Please make sure you've enabled a package step prior to this one.");

			string FileToCopy = "";

			if(BuiltProducts.Count > 0)
			{
				FileToCopy = BuiltProducts[0];
			}

			if(IgorAssert.EnsureTrue(this, File.Exists(FileToCopy), "JenkinsFTP upload was looking for the file " + FileToCopy + " to upload, but the file couldn't be found."))
			{
				Cleanup();

				IgorRuntimeUtils.CopyFile(FileToCopy, GetParamOrConfigString(UploadToFTPFlag, "Destination file for JenkinsFTP isn't set so we can't copy it to the right location."));

				Log("File copied to requested location for Jenkins post build FTP uploading.");
			}

			return true;
		}
	}
}