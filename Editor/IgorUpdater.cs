using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.IO;
using System;
using System.Reflection;
using System.Xml.Serialization;
using System.Security.Cryptography.X509Certificates;

namespace Igor
{
	public interface IIgorEditorCore
	{
		List<string> GetEnabledModuleNames();
		IgorHelperDelegateStub GetHelperDelegates();

		void RunJobInst();
	}

	public class IgorHelperDelegateStub
	{
		public bool bIsValid = false;

		public delegate void VoidOneStringParam(string Param1);

		// These are duplicated from IgorRuntimeUtils.cs in the Core module.  This was necessary to
		// keep the updater working, but they are overridden by the RuntimeUtils version once Core is
		// installed.  If you have any fixes for these two functions, fix them both here and in
		// IgorRuntimeUtils.cs.
		public static void UpdaterDeleteFile(string TargetFile)
		{
			if(File.Exists(TargetFile))
			{
		        File.SetAttributes(TargetFile, System.IO.FileAttributes.Normal);
		        File.Delete(TargetFile);
		    }
		}

		// These are duplicated from IgorRuntimeUtils.cs in the Core module.  This was necessary to
		// keep the updater working, but they are overridden by the RuntimeUtils version once Core is
		// installed.  If you have any fixes for these two functions, fix them both here and in
		// IgorRuntimeUtils.cs.
		public static void UpdaterDeleteDirectory(string targetDir)
		{
			if(!Directory.Exists(targetDir))
			{
				return;
			}

		    System.IO.File.SetAttributes(targetDir, System.IO.FileAttributes.Normal);
		
		    string[] files = System.IO.Directory.GetFiles(targetDir);
		    string[] dirs = System.IO.Directory.GetDirectories(targetDir);
		
		    foreach (string file in files)
		    {
		        System.IO.File.SetAttributes(file, System.IO.FileAttributes.Normal);
		        System.IO.File.Delete(file);
		    }
		
		    foreach (string dir in dirs)
		    {
		        UpdaterDeleteDirectory(dir);
		    }
		
		    System.IO.Directory.Delete(targetDir, false);
		}

		public delegate List<Type> TemplatedTypeListOneBoolParam<TemplatedType>(bool bParam1);
		public List<Type> DoNothingTemplatedTypeListOneBoolParam<TemplatedType>(bool bParam1)
		{
			return new List<Type>();
		}

		public delegate void VoidOneBoolParam(bool Param1);
		public void DoNothingVoidOneBoolParam(bool Param1)
		{}

		public delegate bool BoolNoParams();
		public bool DoNothingBoolNoParams()
		{
			return false;
		}

		public delegate bool BoolStringParam(string Param1);
		public bool DoNothingBoolStringParam(string Param1)
		{
			return false;
		}

		public delegate void VoidStringBoolParams(string Param1, bool Param2);
		public void DoNothingVoidStringBoolParams(string Param1, bool Param2)
		{}

		public VoidOneStringParam DeleteFile;
		public VoidOneStringParam DeleteDirectory;
		public TemplatedTypeListOneBoolParam<IIgorEditorCore> GetTypesInheritFromIIgorEditorCore;
		public VoidOneBoolParam IgorJobConfig_SetWasMenuTriggered;
		public BoolNoParams IgorJobConfig_GetWasMenuTriggered;
		public BoolStringParam IgorJobConfig_IsBoolParamSet;
		public VoidStringBoolParams IgorJobConfig_SetBoolParam;

		public IgorHelperDelegateStub()
		{
			DeleteFile = UpdaterDeleteFile;
			DeleteDirectory = UpdaterDeleteDirectory;
			GetTypesInheritFromIIgorEditorCore = DoNothingTemplatedTypeListOneBoolParam<IIgorEditorCore>;
			IgorJobConfig_SetWasMenuTriggered = DoNothingVoidOneBoolParam;
			IgorJobConfig_GetWasMenuTriggered = DoNothingBoolNoParams;
			IgorJobConfig_IsBoolParamSet = DoNothingBoolStringParam;
			IgorJobConfig_SetBoolParam = DoNothingVoidStringBoolParams;
		}
	}

	public partial class IgorUtils
	{
		public static List<Type> GetTypesWith<TAttribute>(bool bInherit) where TAttribute : Attribute
		{
			List<Type> AllTypes = new List<Type>();

			Assembly[] Assemblies = AppDomain.CurrentDomain.GetAssemblies();

			foreach(Assembly CurrentAssembly in Assemblies)
			{
				foreach(Type CurrentType in CurrentAssembly.GetTypes())
				{
					if(CurrentType.IsDefined(typeof(TAttribute), bInherit))
					{
						AllTypes.Add(CurrentType);
					}
				}
			}

			return AllTypes;
		}

        public static string GetLocalFileFromModuleFilename(string Filename)
		{
			string LocalFileName = Filename;

			if(LocalFileName.StartsWith("("))
			{
				LocalFileName = LocalFileName.Substring(LocalFileName.IndexOf(")") + 1);
			}

			LocalFileName = LocalFileName.Replace("[", "");
			LocalFileName = LocalFileName.Replace("]", "");

			return LocalFileName;
		}

		public static string GetRemoteFileFromModuleFilename(string RootPath, string Filename, ref bool bIsRemote)
		{
			string RemoteFileName = Filename;

			bIsRemote = false;

			if(RemoteFileName.StartsWith("("))
			{
				RemoteFileName = RemoteFileName.Replace("(", "");
				RemoteFileName = RemoteFileName.Replace(")", "");

				bIsRemote = true;

				RemoteFileName = RemoteFileName.Replace("../", "");
			}
			else
			{
				if(RemoteFileName.StartsWith("."))
				{
					RemoteFileName = RemoteFileName.Substring(RemoteFileName.LastIndexOf("/") + 1);
				}

				RemoteFileName = RootPath + RemoteFileName;
			}

			if(RemoteFileName.Contains("["))
			{
				int IndexStart = RemoteFileName.IndexOf("[");
				int IndexEnd = RemoteFileName.LastIndexOf("]") + 1;

				RemoteFileName = RemoteFileName.Replace(RemoteFileName.Substring(IndexStart, IndexEnd - IndexStart), "");
			}

			return RemoteFileName;
		}

		public static string DownloadFileForUpdate(string RelativePath, string AbsolutePath = "", bool bIgnoreErrors = false)
		{
			string DestFilePath = Path.Combine(IgorUpdater.TempLocalDirectory, RelativePath);
			
			try
			{
				if(File.Exists(DestFilePath))
				{
					IgorUpdater.HelperDelegates.DeleteFile(DestFilePath);
				}

				if(!Directory.Exists(Path.GetDirectoryName(DestFilePath)))
				{
					Directory.CreateDirectory(Path.GetDirectoryName(DestFilePath));
				}

				if((IgorUpdater.bLocalDownload || IgorUpdater.bDontUpdate) && AbsolutePath == "")
				{
					string ParentDirectory = Directory.GetCurrentDirectory();
					string NewLocalPrefix = IgorUpdater.LocalPrefix;

					while(NewLocalPrefix.StartsWith(".."))
					{
						ParentDirectory = Directory.GetParent(ParentDirectory).ToString();

						NewLocalPrefix = NewLocalPrefix.Substring(3);
					}

					NewLocalPrefix = Path.Combine(ParentDirectory, NewLocalPrefix);
                    string LocalFilePath = Path.Combine(NewLocalPrefix, RelativePath);

                    if(File.Exists(LocalFilePath))
					    File.Copy(LocalFilePath, DestFilePath);
				}
				else
				{
                    string PathToSource = string.Empty;
                    if(AbsolutePath == "")
					{
						PathToSource = IgorUpdater.RemotePrefix + RelativePath;
					}
					else
					{
						PathToSource = AbsolutePath;
                    }
                    
                    PathToSource = PathToSource.Replace("\\", "/");
				    WWW www = new WWW(PathToSource);
					while(!www.isDone)
                    { }

                    if(www.error != null && www.error != "")
                    {
                    	if(!bIgnoreErrors)
                    	{
	                    	Debug.LogError("Igor Error: Downloading " + PathToSource + " failed.  Error is \"" + www.error + "\"");
	                    }
                    }
                    else
                    {
                    	File.WriteAllBytes(DestFilePath, www.bytes);
                    }

                    www.Dispose();
				}
			}
			catch(Exception e)
			{
				Debug.LogError("Igor Error: Failed to download file " + RelativePath + " with error " + e.ToString());
			}

			return DestFilePath;
		}
	}

	[XmlRoot("IgorModuleList")]
	public class IgorModuleList
	{
		public class ModuleItem
		{
			[XmlAttribute("ModuleName")]
			public string ModuleName;
			[XmlAttribute("ModuleDescriptorRelativePath")]
			public string ModuleDescriptorRelativePath;
		}

		[XmlArray("Modules")]
		[XmlArrayItem("Module")]
		public List<ModuleItem> Modules = new List<ModuleItem>();

	 	public void Save(string path)
	 	{
	 		XmlSerializer serializer = new XmlSerializer(typeof(IgorModuleList));

	 		IgorUpdater.HelperDelegates.DeleteFile(path);

	 		using(FileStream stream = new FileStream(path, FileMode.Create))
	 		{
	 			serializer.Serialize(stream, this);
	 		}
	 	}
	 
	 	public static IgorModuleList Load(string path)
	 	{
	 		XmlSerializer serializer = new XmlSerializer(typeof(IgorModuleList));

	 		if(File.Exists(path))
			{
				File.SetAttributes(path, System.IO.FileAttributes.Normal);
			
	 		    using(var stream = new FileStream(path, FileMode.Open))
	 		    {
	 			    return serializer.Deserialize(stream) as IgorModuleList;
	 		    }
            }
            
            return null;
	 	}
	}

	[XmlRoot("IgorModuleDescriptor")]
	public class IgorModuleDescriptor
	{
		public string ModuleName;
		public int ModuleVersion;

		[XmlArray("ModuleFiles")]
		[XmlArrayItem("File")]
		public List<string> ModuleFiles = new List<string>();

		[XmlArray("ModuleDependencies")]
		[XmlArrayItem("Module")]
		public List<string> ModuleDependencies = new List<string>();

	 	public void Save(string path)
	 	{
	 		XmlSerializer serializer = new XmlSerializer(typeof(IgorModuleDescriptor));

	 		IgorUpdater.HelperDelegates.DeleteFile(path);

	 		using(FileStream stream = new FileStream(path, FileMode.Create))
	 		{
	 			serializer.Serialize(stream, this);
	 		}
	 	}
	 
	 	public static IgorModuleDescriptor Load(string path)
	 	{
	 		XmlSerializer serializer = new XmlSerializer(typeof(IgorModuleDescriptor));

	 		if(File.Exists(path))
			{
				File.SetAttributes(path, System.IO.FileAttributes.Normal);
			}

	 		using(var stream = new FileStream(path, FileMode.Open))
	 		{
	 			return serializer.Deserialize(stream) as IgorModuleDescriptor;
	 		}
	 	}
	}

	[InitializeOnLoad]
	public class IgorUpdater
	{
		private static IgorHelperDelegateStub _HelperDelegates = null;
		public static IgorHelperDelegateStub HelperDelegates {
			get {
				if(_HelperDelegates == null)
				{
					Type IgorEditorCoreType = Type.GetType("IgorEditorCore");

					if(IgorEditorCoreType != null)
					{
						IIgorEditorCore EditorCoreInst = (IIgorEditorCore)Activator.CreateInstance(IgorEditorCoreType);

						_HelperDelegates = EditorCoreInst.GetHelperDelegates();
					}
					else
					{
						_HelperDelegates = new IgorHelperDelegateStub();
					}
				}

				return _HelperDelegates;
			}
			set {
				_HelperDelegates = value;
			}
		}

		static string kPrefix = "Igor_";

		[PreferenceItem("Igor")]
		static void PreferencesGUI()
		{
			bDontUpdate = EditorGUILayout.Toggle(new GUIContent("Don't auto-update", "No updating Igor files from local or remote sources"), bDontUpdate);
			bAlwaysUpdate = EditorGUILayout.Toggle(new GUIContent("Always update", "Update even if the versions match"), bAlwaysUpdate);
			bLocalDownload = EditorGUILayout.Toggle(new GUIContent("Local update", "Update from local directory instead of remotely from GitHub"), bLocalDownload);
			bDownloadRemoteWhenLocal = EditorGUILayout.Toggle(new GUIContent("Download remote repos in local", "For modules that have externally hosted content.  Enable this if you want Igor to pull the latest version from the remote host even during a local update."), bDownloadRemoteWhenLocal);
			LocalPrefix = EditorGUILayout.TextField(new GUIContent("Local Update Directory", "This is the local directory (relative to the project root) to pull from when Local Update is enabled."), LocalPrefix);
		}

		public static bool bDontUpdate
		{
			get { return EditorPrefs.GetBool(kPrefix + "bDontUpdate", false); }
			set { EditorPrefs.SetBool(kPrefix + "bDontUpdate", value); }
		}

		public static bool bAlwaysUpdate
		{
			get { return EditorPrefs.GetBool(kPrefix + "bAlwaysUpdate", false); }
			set { EditorPrefs.SetBool(kPrefix + "bAlwaysUpdate", value); }
		}

		public static bool bLocalDownload
		{
			get { return EditorPrefs.GetBool(kPrefix + "bLocalDownload", false); }
			set { EditorPrefs.SetBool(kPrefix + "bLocalDownload", value); }
		}

		public static bool bDownloadRemoteWhenLocal
		{
			get { return EditorPrefs.GetBool(kPrefix + "bDownloadRemoteWhenLocal", false); }
			set { EditorPrefs.SetBool(kPrefix + "bDownloadRemoteWhenLocal", value); }
		}

		public static string LocalPrefix
		{
			get { return EditorPrefs.GetString(kPrefix + "LocalPrefix", ""); }
			set { EditorPrefs.SetString(kPrefix + "LocalPrefix", value); }
		}

		static IgorUpdater()
		{
			ServicePointManager.ServerCertificateValidationCallback +=
			delegate(object sender, X509Certificate certificate,
			                        X509Chain chain,
			                        System.Net.Security.SslPolicyErrors sslPolicyErrors)
			    {
			        return true;
			    };

			EditorApplication.update += CheckIfResuming;
		}

		private const int Version = 30;
		private const int MajorUpgrade = 2;

		private static string OldBaseIgorDirectory = Path.Combine("Assets", Path.Combine("Editor", "Igor"));
		public static string BaseIgorDirectory = Path.Combine("Assets", "Igor");
		public static string RemotePrefix = "https://raw.githubusercontent.com/mikamikem/Igor/master/";
		public static string TempLocalDirectory = "IgorTemp/";

		private static string IgorUpdaterBaseFilename = "IgorUpdater.cs";
		public static string IgorUpdaterFilename = Path.Combine("Editor", IgorUpdaterBaseFilename);
        public static string IgorUpdaterURL = "Editor/" +  IgorUpdaterBaseFilename;
		public static string IgorModulesListFilename = "IgorModuleList.xml";
		public static string InstalledModulesListPath = Path.Combine(BaseIgorDirectory, IgorModulesListFilename);
		public static string IgorLocalModulesListFilename = "IgorLocalModulesList.xml";
		public static string InstalledLocalModulesListPath = Path.Combine(BaseIgorDirectory, IgorLocalModulesListFilename);
		private static IgorModuleList SharedModuleListInst = null;

		public static string LocalModuleRoot = Path.Combine(BaseIgorDirectory, "Modules");
		public static string RemoteRelativeModuleRoot = "Modules/";
		public static string CoreModuleName = "Core.Core";

		public static IIgorEditorCore Core = null;

        private static List<string> UpdatedContent = new List<string>(); 
		private static List<string> UpdatedModules = new List<string>();

		public static bool bTriggerConfigWindowRefresh = false;

		[MenuItem("Window/Igor/Check For Updates %i", false, 2)]
		public static void MenuCheckForUpdates()
		{
			IgorUpdater.bTriggerConfigWindowRefresh = true;
			
			IgorUpdater.CheckForUpdates(true, true);
		}

		// Returns true if files were updated
		public static bool CheckForUpdates(bool bForce = false, bool bFromMenu = false, bool bSynchronous = false)
		{
			HelperDelegates.IgorJobConfig_SetWasMenuTriggered(bFromMenu);

			if(CheckForOneTimeForcedUpgrade())
			{
				MoveConfigsFromOldLocation();

				bForce = true;
			}

			if(!bDontUpdate || bForce)
			{
                UpdatedContent.Clear();

                bool bMajorUpgrade = false;

                if(!HelperDelegates.bIsValid)
                {
                	UpdateCore();

                	bMajorUpgrade = true;
                }

				bool bNeedsRebuild = bMajorUpgrade || SelfUpdate(out bMajorUpgrade);

				bNeedsRebuild = bMajorUpgrade || UpdateCore() || bNeedsRebuild;
				bNeedsRebuild = bMajorUpgrade || UpdateModules() || bNeedsRebuild;

				if(CheckForOneTimeForcedUpgrade())
				{
					RemoveOldDirectory();

					bNeedsRebuild = true;
				}

				if(bNeedsRebuild)
				{
					HelperDelegates.IgorJobConfig_SetBoolParam("restartingfromupdate", true);

                    string UpdatedContentString = "The following Igor content was ";

                    if(bAlwaysUpdate)
                    {
                    	UpdatedContentString += "forcibly ";
                    }

                    UpdatedContentString += "updated via ";

                    if(bLocalDownload)
                    {
                    	UpdatedContentString += "local copy from " + LocalPrefix;
                    }
                    else
                    {
                    	UpdatedContentString += "remote copy from GitHub";
                    }

                    UpdatedContentString += ", refreshing AssetDatabase:\n";

                    foreach(var content in UpdatedContent)
                    {
                        UpdatedContentString += content + "\n";
                    }

                    Debug.Log("Igor Log: " + UpdatedContentString);

                    ImportAssetOptions options = bSynchronous ? ImportAssetOptions.ForceSynchronousImport : ImportAssetOptions.Default;
					AssetDatabase.Refresh(options);
				}

				return bNeedsRebuild;
			}

			return false;
		}

		public static void FindCore()
		{
			if(Core == null)
			{
				List<Type> ActiveCores = HelperDelegates.GetTypesInheritFromIIgorEditorCore(true);

				if(ActiveCores.Count > 0)
				{
					Core = (IIgorEditorCore)Activator.CreateInstance(ActiveCores[0]);
				}
			}
		}

		public static void CleanupTemp()
		{
			IgorUpdater.HelperDelegates.DeleteDirectory(TempLocalDirectory);
		}

		public static int GetCurrentVersion()
		{
			return Version;
		}

		public static int GetVersionFromUpdaterFile(string Filename)
		{
			if(File.Exists(Filename))
			{
				string FileContents = File.ReadAllText(Filename);

			    if(!string.IsNullOrEmpty(FileContents))
			    {
			        int StartOfVersionNumber = FileContents.IndexOf("private const int Version = ") + "private const int Version = ".Length;

			        string VersionNumberString = FileContents.Substring(StartOfVersionNumber, FileContents.IndexOf(";", StartOfVersionNumber) - StartOfVersionNumber);

			        int VersionNumber = -1;

			        int.TryParse(VersionNumberString, out VersionNumber);

			        return VersionNumber;
			    }
			}

			return -1;
		}

		public static int GetMajorUpgradeFromUpdaterFile(string Filename)
		{
			if(File.Exists(Filename))
			{
				string FileContents = File.ReadAllText(Filename);

			    if(!string.IsNullOrEmpty(FileContents))
			    {
			        int StartOfVersionNumber = FileContents.IndexOf("private const int MajorUpgrade = ") + "private const int MajorUpgrade = ".Length;

			        string VersionNumberString = FileContents.Substring(StartOfVersionNumber, FileContents.IndexOf(";", StartOfVersionNumber) - StartOfVersionNumber);

			        int VersionNumber = -1;

			        int.TryParse(VersionNumberString, out VersionNumber);

			        return VersionNumber;
			    }
			}

			return -1;
		}

		public static bool SelfUpdate(out bool bMajorUpgrade)
		{
			bool bThrewException = false;
			bMajorUpgrade = false;

			string InstalledFilePath = Path.Combine(BaseIgorDirectory, IgorUpdaterFilename);

			try
			{
				string LocalUpdater = IgorUtils.DownloadFileForUpdate(IgorUpdaterURL);

				if(File.Exists(LocalUpdater))
				{
					int NewVersion = GetVersionFromUpdaterFile(LocalUpdater);
					int NewMajorUpgrade = GetMajorUpgradeFromUpdaterFile(LocalUpdater);

					if(NewMajorUpgrade > MajorUpgrade)
					{
						bMajorUpgrade = true;
					}

					if(NewVersion > Version || bAlwaysUpdate)
					{
						if(File.Exists(InstalledFilePath))
						{
							IgorUpdater.HelperDelegates.DeleteFile(InstalledFilePath);
						}

						if(!Directory.Exists(Path.GetDirectoryName(InstalledFilePath)))
						{
							Directory.CreateDirectory(Path.GetDirectoryName(InstalledFilePath));
						}

						File.Copy(LocalUpdater, InstalledFilePath);

						return true;
					}
				}
			}
			catch(TimeoutException to)
			{
				if(!File.Exists(InstalledFilePath))
				{
					Debug.LogError("Igor Error: Caught exception while self-updating.  Exception is " + (to == null ? "NULL exception!" : to.ToString()));

					bThrewException = true;

					CleanupTemp();
				}
			}
			catch(Exception e)
			{
				Debug.LogError("Igor Error: Caught exception while self-updating.  Exception is " + (e == null ? "NULL exception!" : e.ToString()));

				bThrewException = true;

				CleanupTemp();
			}

			if(!HelperDelegates.IgorJobConfig_GetWasMenuTriggered())
			{
				if(bThrewException)
				{
                    Debug.LogError("Igor Error: Exiting EditorApplication because an exception was thrown.");

                    if(HelperDelegates.bIsValid)
                    {
						EditorApplication.Exit(-1);
					}
				}
			}

			return false;
		}

		public static bool UpdateCore()
		{
			bool bThrewException = false;

			string LocalModulesList = IgorUtils.DownloadFileForUpdate(IgorModulesListFilename);

			try
			{
				if(File.Exists(LocalModulesList) && HelperDelegates.bIsValid)
				{
					if(File.Exists(InstalledModulesListPath))
					{
						IgorUpdater.HelperDelegates.DeleteFile(InstalledModulesListPath);
					}

					File.Copy(LocalModulesList, InstalledModulesListPath);
				}

				UpdatedModules.Clear();

				if(UpdateModule(CoreModuleName, false))
				{
					return true;
				}
			}
			catch(TimeoutException to)
			{
				if(!File.Exists(LocalModulesList))
				{
					Debug.LogError("Igor Error: Caught exception while self-updating.  Exception is " + (to == null ? "NULL exception!" : to.ToString()));

					bThrewException = true;

					CleanupTemp();
				}
			}
			catch(Exception e)
			{
				Debug.LogError("Igor Error: Caught exception while updating core.  Exception is " + (e == null ? "NULL exception!" : e.ToString()));

				bThrewException = true;
				
				CleanupTemp();
			}

			if(!HelperDelegates.IgorJobConfig_GetWasMenuTriggered())
			{
				if(bThrewException)
				{
                    Debug.LogError("Igor Error: Exiting EditorApplication because an exception was thrown.");

                    if(HelperDelegates.bIsValid)
                    {
						EditorApplication.Exit(-1);
					}
				}
			}

			return false;
		}

		public static bool UpdateModule(string ModuleName, bool ForceUpdate)
		{
			bool bUpdated = false;

			if(File.Exists(InstalledModulesListPath))
			{
				SharedModuleListInst = IgorModuleList.Load(InstalledModulesListPath);
			}

			if(SharedModuleListInst != null)
			{
				foreach(IgorModuleList.ModuleItem CurrentModule in SharedModuleListInst.Modules)
				{
					if(CurrentModule.ModuleName == ModuleName)
					{
						string ModuleDescriptor = IgorUtils.DownloadFileForUpdate(RemoteRelativeModuleRoot + CurrentModule.ModuleDescriptorRelativePath);
						string CurrentModuleDescriptor = Path.Combine(LocalModuleRoot, CurrentModule.ModuleDescriptorRelativePath);

						if(File.Exists(ModuleDescriptor))
						{
							IgorModuleDescriptor CurrentModuleDescriptorInst = null;
							IgorModuleDescriptor NewModuleDescriptorInst = IgorModuleDescriptor.Load(ModuleDescriptor);

							if(File.Exists(CurrentModuleDescriptor))
							{
								CurrentModuleDescriptorInst = IgorModuleDescriptor.Load(CurrentModuleDescriptor);
							}

							if(NewModuleDescriptorInst != null)
							{
								if(UpdatedModules.Contains(NewModuleDescriptorInst.ModuleName))
								{
									return false;
								}

								UpdatedModules.Add(NewModuleDescriptorInst.ModuleName);

								if(NewModuleDescriptorInst.ModuleDependencies.Count > 0)
								{
									foreach(string CurrentDependency in NewModuleDescriptorInst.ModuleDependencies)
									{
										bUpdated = UpdateModule(CurrentDependency, ForceUpdate) || bUpdated;
									}
								}

								int NewVersion = NewModuleDescriptorInst.ModuleVersion;

								if(CurrentModuleDescriptorInst == null || NewVersion > CurrentModuleDescriptorInst.ModuleVersion || bAlwaysUpdate || ForceUpdate)
								{
									bUpdated = true;
                                    UpdatedContent.Add(ModuleName);

                                    List<string> FilesToDelete = new List<string>();

									if(CurrentModuleDescriptorInst != null)
									{
										IgorUpdater.HelperDelegates.DeleteFile(CurrentModuleDescriptor);

										FilesToDelete.AddRange(CurrentModuleDescriptorInst.ModuleFiles);
									}

									if(!Directory.Exists(Path.GetDirectoryName(CurrentModuleDescriptor)))
									{
										Directory.CreateDirectory(Path.GetDirectoryName(CurrentModuleDescriptor));
									}

									File.Copy(ModuleDescriptor, CurrentModuleDescriptor);

									foreach(string ModuleFile in NewModuleDescriptorInst.ModuleFiles)
									{
										FilesToDelete.Remove(ModuleFile);

                                        bool bIsExternal = false;

                                        string LocalFile = IgorUtils.GetLocalFileFromModuleFilename(ModuleFile);

										string FullLocalPath = Path.Combine(LocalModuleRoot, Path.Combine(Path.GetDirectoryName(CurrentModule.ModuleDescriptorRelativePath), LocalFile));
										
                                        string RemotePath = IgorUtils.GetRemoteFileFromModuleFilename(RemoteRelativeModuleRoot + Path.GetDirectoryName(CurrentModule.ModuleDescriptorRelativePath) + "/", ModuleFile, ref bIsExternal);

										if(LocalFile.StartsWith("."))
										{
											string Base = Path.Combine(LocalModuleRoot.Replace('/', Path.DirectorySeparatorChar), Path.GetDirectoryName(CurrentModule.ModuleDescriptorRelativePath.Replace('/', Path.DirectorySeparatorChar)));
											string NewLocalFile = LocalFile.Replace('/', Path.DirectorySeparatorChar);
											int FirstIndex = NewLocalFile.IndexOf(".." + Path.DirectorySeparatorChar);

											while(FirstIndex != -1)
											{
												int LastIndex = Base.LastIndexOf(Path.DirectorySeparatorChar);

												if(LastIndex != -1)
												{
													Base = Base.Substring(0, LastIndex);
												}

												NewLocalFile = NewLocalFile.Substring(3);

												FirstIndex = NewLocalFile.IndexOf(".." + Path.DirectorySeparatorChar);
											}

											FullLocalPath = Path.Combine(Base, NewLocalFile);
											RemotePath = IgorUtils.GetRemoteFileFromModuleFilename(RemoteRelativeModuleRoot + Path.GetDirectoryName(CurrentModule.ModuleDescriptorRelativePath) + "/", ModuleFile, ref bIsExternal);
										}

                                        string TempDownloadPath = "";
                                        if(bIsExternal)
										{
											if(!bLocalDownload || bDownloadRemoteWhenLocal || !File.Exists(FullLocalPath))
											{
												if(LocalFile.Contains("../"))
												{
													TempDownloadPath = IgorUtils.DownloadFileForUpdate(FullLocalPath, RemotePath);
												}
												else
												{
													TempDownloadPath = IgorUtils.DownloadFileForUpdate(Path.Combine(Path.GetDirectoryName(CurrentModule.ModuleDescriptorRelativePath), LocalFile), RemotePath);
												}
											}
										}
										else
										{
											TempDownloadPath = IgorUtils.DownloadFileForUpdate(RemotePath);
										}

										if(TempDownloadPath != "")
										{
											if(File.Exists(FullLocalPath))
											{
												IgorUpdater.HelperDelegates.DeleteFile(FullLocalPath);
											}

											if(!Directory.Exists(Path.GetDirectoryName(FullLocalPath)))
											{
												Directory.CreateDirectory(Path.GetDirectoryName(FullLocalPath));
											}

											if(File.Exists(TempDownloadPath))
											{
												File.Copy(TempDownloadPath, FullLocalPath);
											}
										}
									}

									foreach(string FilenameToDelete in FilesToDelete)
									{
										string LocalFile = IgorUtils.GetLocalFileFromModuleFilename(FilenameToDelete);
										string FullLocalPath = Path.Combine(LocalModuleRoot, Path.Combine(Path.GetDirectoryName(CurrentModule.ModuleDescriptorRelativePath), LocalFile));

										if(File.Exists(FullLocalPath))
										{
											IgorUpdater.HelperDelegates.DeleteFile(FullLocalPath);
										}
									}
								}
							}
						}

						return bUpdated;
					}
				}
			}

			return false;
		}

		public static bool UpdateModules()
		{
			bool bThrewException = false;

			try
			{
				FindCore();

				if(Core != null)
				{
					bool bUpdated = false;

					UpdatedModules.Clear();

					foreach(string CurrentModule in Core.GetEnabledModuleNames())
					{
						bUpdated = UpdateModule(CurrentModule, false) || bUpdated;
					}

					if(bUpdated)
					{
						return true;
					}
				}
			}
			catch(TimeoutException)
			{
				// We should eventually handle this case by triggering a re-attempt
			}
			catch(Exception e)
			{
				Debug.LogError("Igor Error: Caught exception while updating modules.  Exception is " + (e == null ? "NULL exception!" : e.ToString()));

				bThrewException = true;

				CleanupTemp();
			}

			if(!HelperDelegates.IgorJobConfig_GetWasMenuTriggered())
			{
				if(bThrewException)
				{
                    Debug.LogError("Igor Error: Exiting EditorApplication because an exception was thrown.");

                    if(HelperDelegates.bIsValid)
                    {
						EditorApplication.Exit(-1);
					}
				}
			}

			return false;
		}

		public static void CheckIfResuming()
		{
		    if(!EditorApplication.isCompiling)
		    {
		        bool bThrewException = false;

		        EditorApplication.update -= CheckIfResuming;

		        try
		        {
		            FindCore();

		            if(HelperDelegates.IgorJobConfig_IsBoolParamSet("restartingfromupdate") || HelperDelegates.IgorJobConfig_IsBoolParamSet("updatebeforebuild") || Core == null)
		            {
		                HelperDelegates.IgorJobConfig_SetBoolParam("restartingfromupdate", false);

		                if(!CheckForUpdates())
		                {
		                    if(HelperDelegates.IgorJobConfig_IsBoolParamSet("updatebeforebuild"))
		                    {
		                        HelperDelegates.IgorJobConfig_SetBoolParam("updatebeforebuild", false);

		                        if(Core != null)
		                        {
		                            Core.RunJobInst();
		                        }
		                        else
		                        {
		                            Debug.LogError("Igor Error: Something went really wrong.  We don't have Igor's core, but we've already finished updating everything.  Report this with your logs please!");
		                        }
		                    }
		                }
		            }
		        }
		        catch(Exception e)
		        {
		            Debug.LogError("Igor Error: Caught exception while resuming from updates.  Exception is " + (e == null ? "NULL exception!" : e.ToString()));

		            bThrewException = true;
		        }

		        if(!HelperDelegates.IgorJobConfig_GetWasMenuTriggered())
		        {
		            if(bThrewException)
		            {
                        Debug.LogError("Igor Error: Exiting EditorApplication because an exception was thrown.");

	                    if(HelperDelegates.bIsValid)
	                    {
			                EditorApplication.Exit(-1);
			            }
		            }
		        }
		    }
		}

		public static void GenerateModuleList()
		{
			IgorModuleList NewInst = new IgorModuleList();

			IgorModuleList.ModuleItem NewItem = new IgorModuleList.ModuleItem();

			NewItem.ModuleName = "Core.Core";
			NewItem.ModuleDescriptorRelativePath = "Core/Core/Core.mod";

			NewInst.Modules.Add(NewItem);

			NewInst.Save(IgorModulesListFilename);
		}

		public static bool CheckForOneTimeForcedUpgrade()
		{
			if(File.Exists(Path.Combine(OldBaseIgorDirectory, IgorUpdaterBaseFilename)))
			{
				return true;
			}

			return false;
		}

		public static void MoveConfigsFromOldLocation()
		{
			string OriginalConfigFile = Path.Combine(OldBaseIgorDirectory, "IgorConfig.xml");

			if(File.Exists(OriginalConfigFile))
			{
				string DestinationConfigFile = Path.Combine(BaseIgorDirectory, "IgorConfig.xml");

				if(!Directory.Exists(Path.GetDirectoryName(DestinationConfigFile)))
				{
					Directory.CreateDirectory(Path.GetDirectoryName(DestinationConfigFile));
				}

				File.Copy(OriginalConfigFile, DestinationConfigFile);
			}
		}

		public static void RemoveOldDirectory()
		{
			IgorUpdater.HelperDelegates.DeleteDirectory(OldBaseIgorDirectory);
			IgorUpdater.HelperDelegates.DeleteFile(OldBaseIgorDirectory + ".meta");
		}
	}
}