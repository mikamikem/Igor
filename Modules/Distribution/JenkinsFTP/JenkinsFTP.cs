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
			if(IgorJobConfig.GetStringParam(UploadToFTPFlag) != "" &&
				(IgorJobConfig.IsBoolParamSet(UploadToFTPNoEnvFlag) ||
					(IgorJobConfig.GetStringParam(UploadToFTPEnvToggleFlag) != "" && IgorUtils.GetEnvVariable(IgorJobConfig.GetStringParam(UploadToFTPEnvToggleFlag)) != "")))
			{
				IgorCore.SetModuleActiveForJob(this);
				StepHandler.RegisterJobStep(IgorBuildCommon.PreBuildCleanupStep, this, Cleanup);
				StepHandler.RegisterJobStep(UploadToFTPStep, this, UploadToFTP);
			}
		}

		public override string DrawJobInspectorAndGetEnabledParams(string CurrentParams)
		{
			string EnabledParams = CurrentParams;

			DrawBoolParam(ref EnabledParams, "Always Upload", UploadToFTPNoEnvFlag);
			DrawStringParam(ref EnabledParams, "FTP Filename", UploadToFTPFlag);
			DrawStringParam(ref EnabledParams, "Environment Variable Toggle", UploadToFTPEnvToggleFlag);

			return EnabledParams;
		}

		public virtual bool Cleanup()
		{
			string DestinationFile = IgorJobConfig.GetStringParam(UploadToFTPFlag);

			if(File.Exists(DestinationFile))
			{
				IgorUtils.DeleteFile(DestinationFile);
			}

			return true;
		}

		public virtual bool UploadToFTP()
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
				Cleanup();

				File.Copy(FileToCopy, IgorJobConfig.GetStringParam(UploadToFTPFlag));

				Log("File copied to requested location for Jenkins post build FTP uploading.");
			}

			return true;
		}
	}
}