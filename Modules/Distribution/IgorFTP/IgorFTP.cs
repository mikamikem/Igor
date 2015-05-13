using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.IO;
using System;
using System.Reflection;
using System.Xml.Serialization;
using System.Text;

namespace Igor
{
	public class IgorFTP : IgorModuleBase
	{
		public static string UploadToFTPNoEnvFlag = "uploadftpalways";
		public static string UploadToFTPEnvEnableFlag = "uploadftpenvenable";
		public static string UploadToFTPEnvNameFlag = "uploadftpenvname";
		public static string UploadToFTPHostFlag = "uploadftphost";
		public static string UploadToFTPUserFlag = "uploadftpuser";
		public static string UploadToFTPPassFlag = "uploadftppass";
		public static string UploadToFTPDirectoryFlag = "uploadftpdir";

		public static StepID UploadToFTPStep = new StepID("Upload To FTP", 1100);

		public override string GetModuleName()
		{
			return "Distribution.IgorFTP";
		}

		public override void RegisterModule()
		{
			IgorCore.RegisterNewModule(this);
		}

		public override void ProcessArgs(IIgorStepHandler StepHandler)
		{
			bool bStepRegistered = false;

			if(GetParamOrConfigString(UploadToFTPHostFlag) != "" && GetParamOrConfigString(UploadToFTPUserFlag) != "" && GetParamOrConfigString(UploadToFTPPassFlag) != "" && 
			   GetParamOrConfigString(UploadToFTPDirectoryFlag) != "" && 
				(IgorJobConfig.IsBoolParamSet(UploadToFTPNoEnvFlag) ||
					(IgorJobConfig.IsBoolParamSet(UploadToFTPEnvEnableFlag) && GetParamOrConfigString(UploadToFTPEnvNameFlag) != ""
						&& IgorUtils.GetEnvVariable(GetParamOrConfigString(UploadToFTPEnvNameFlag)) == "true")))
			{
				StepHandler.RegisterJobStep(UploadToFTPStep, this, UploadToFTP);

				IgorCore.SetModuleActiveForJob(this);
			}
		}

		public override string DrawJobInspectorAndGetEnabledParams(string CurrentParams)
		{
			string EnabledParams = CurrentParams;

			DrawBoolParam(ref EnabledParams, "Always Upload", UploadToFTPNoEnvFlag);

			if(DrawBoolParam(ref EnabledParams, "Upload if environment variable is set to true", UploadToFTPEnvEnableFlag))
			{
				DrawStringConfigParam(ref EnabledParams, "Environment variable name", UploadToFTPEnvNameFlag);
			}

			DrawStringConfigParam(ref EnabledParams, "FTP Host", UploadToFTPHostFlag);
			DrawStringConfigParam(ref EnabledParams, "FTP Username", UploadToFTPUserFlag);
			DrawStringConfigParam(ref EnabledParams, "FTP Password", UploadToFTPPassFlag);
			DrawStringConfigParam(ref EnabledParams, "FTP Destination Directory", UploadToFTPDirectoryFlag);

			return EnabledParams;
		}

		public virtual bool UploadToFTP()
		{
			List<string> BuiltProducts = IgorBuildCommon.GetBuildProducts();

			string FTPRoot = GetParamOrConfigString(UploadToFTPHostFlag, "FTP Upload host is not set!  Can't upload without a host!");
			string FTPUsername = GetParamOrConfigString(UploadToFTPUserFlag, "FTP Upload username is not set!  Can't upload without a username!");
			string FTPPassword = GetParamOrConfigString(UploadToFTPPassFlag, "FTP Upload password is not set!  Can't upload without a password!");
			string FTPDirectory = GetParamOrConfigString(UploadToFTPDirectoryFlag);

			if(!FTPRoot.StartsWith("ftp://"))
			{
				FTPRoot = "ftp://" + FTPRoot;
			}

			if(!FTPRoot.EndsWith("/"))
			{
				FTPRoot += "/";
			}

			if(FTPDirectory.Length > 0 && !FTPDirectory.EndsWith("/"))
			{
				FTPDirectory += "/";
			}

			if(FTPRoot != "" && FTPUsername != "" && FTPPassword != "")
			{
				bool bFailedAtLeastOnce = false;

				foreach(string CurrentProduct in BuiltProducts)
				{
					string DestinationFilename = FTPRoot + FTPDirectory + Path.GetFileName(CurrentProduct);
		            // Get the object used to communicate with the server.
		            FtpWebRequest request = (FtpWebRequest)WebRequest.Create(DestinationFilename);
		            request.Method = WebRequestMethods.Ftp.UploadFile;
		            request.UseBinary = true;

		            request.Credentials = new NetworkCredential (FTPUsername, FTPPassword);
		            
		            // Copy the contents of the file to the request stream.
		            byte [] fileContents = File.ReadAllBytes(CurrentProduct);
		            request.ContentLength = fileContents.Length;

		            Stream requestStream = request.GetRequestStream();
		            requestStream.Write(fileContents, 0, fileContents.Length);
		            requestStream.Close();

		            FtpWebResponse response = (FtpWebResponse)request.GetResponse();
		    
		    		if(response.StatusCode == FtpStatusCode.CommandOK || response.StatusCode == FtpStatusCode.ClosingData ||
		    		   response.StatusCode == FtpStatusCode.FileActionOK || response.StatusCode == FtpStatusCode.ClosingControl)
		    		{
			    		Log("Successfully uploaded file " + CurrentProduct + " to " + DestinationFilename);
			    	}
			    	else
			    	{
			    		Log("Failed to upload file " + CurrentProduct + " to " + DestinationFilename + " with error code " + response.StatusCode + " and exit message " + response.ExitMessage);

			    		bFailedAtLeastOnce = true;
			    	}
		    
		            response.Close();
		        }

		        if(!bFailedAtLeastOnce)
		        {
		        	Log("All files were successfully uploaded!");
		        }
		        else
		        {
		        	Log("Some files were not successfully uploaded.  Please check the logs to see what went wrong.");
		        }
	        }

			return true;
		}
	}
}