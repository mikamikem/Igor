using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.IO;
using System;
using System.Reflection;
using System.Xml.Serialization;
using UnityEditor.XCodeEditor;

namespace Igor
{
	public class IgorXCodeProjUtils
	{
		public static void SetDevTeamID(IIgorModule ModuleInst, string ProjectPath, string DevTeamID)
		{
			if(IgorAssert.EnsureTrue(ModuleInst, Directory.Exists(ProjectPath), "XCodeProj doesn't exist at path " + ProjectPath))
			{
				XCProject CurrentProject = new XCProject(ProjectPath);

				CurrentProject.Backup();

				string ProjectGUID = CurrentProject.project.guid;

				object ProjectSectionObj = CurrentProject.GetObject(ProjectGUID);

				if(IgorAssert.EnsureTrue(ModuleInst, ProjectSectionObj != null, "Can't find Project Section in XCodeProj."))
				{
					PBXDictionary ProjectSection = (PBXDictionary)ProjectSectionObj;

					object AttributesSectionObj = ProjectSection["attributes"];

					if(IgorAssert.EnsureTrue(ModuleInst, AttributesSectionObj != null, "Can't find Attributes Section in Project Section."))
					{
						object TargetAttributesObj = ((PBXDictionary)AttributesSectionObj)["TargetAttributes"];

						if(IgorAssert.EnsureTrue(ModuleInst, TargetAttributesObj != null, "Can't find TargetAttributes Section in Attributes Section."))
						{
							PBXDictionary TargetAttributes = (PBXDictionary)TargetAttributesObj;

							object TargetsObj = ProjectSection["targets"];

							if(IgorAssert.EnsureTrue(ModuleInst, TargetsObj != null, "Can't find Targets Section in Project Section."))
							{
								PBXList TargetsList = ((PBXList)TargetsObj);

								if(IgorAssert.EnsureTrue(ModuleInst, TargetsList.Count > 0, "No build targets defined in XCodeProj."))
								{
									string PrimaryBuildTargetGUID = (string)(TargetsList[0]);

									PBXDictionary PrimaryBuildTargetToDevTeam = new PBXDictionary();
									PBXDictionary DevTeamIDDictionary = new PBXDictionary();

									DevTeamIDDictionary.Add("DevelopmentTeam", DevTeamID);

									PrimaryBuildTargetToDevTeam.Add(PrimaryBuildTargetGUID, DevTeamIDDictionary);

									if(TargetAttributes.ContainsKey(PrimaryBuildTargetGUID))
									{
										object ExistingPrimaryBuildTargetObj = TargetAttributes[PrimaryBuildTargetGUID];

										if(ExistingPrimaryBuildTargetObj != null)
										{
											PBXDictionary ExistingPrimaryBuildTarget = (PBXDictionary)ExistingPrimaryBuildTargetObj;

											if(!ExistingPrimaryBuildTarget.ContainsKey("DevelopmentTeam"))
											{
												ExistingPrimaryBuildTarget.Append(DevTeamIDDictionary);

												IgorCore.Log(ModuleInst, "Added Development Team to XCodeProj.");
											}
											else
											{
												IgorCore.Log(ModuleInst, "Development Team already set up in XCodeProj.");
											}
										}
										else
										{
											IgorCore.LogError(ModuleInst, "Primary build target already has a key in TargetAttributes, but the value stored is invalid.");
										}
									}
									else
									{
										TargetAttributes.Append(PrimaryBuildTargetToDevTeam);

										IgorCore.Log(ModuleInst, "Added Development Team to XCodeProj.");
									}

									CurrentProject.Save();
								}
							}
						}
					}
				}
			}
		}

		public static void AddOrUpdateForAllBuildProducts(IIgorModule ModuleInst, string ProjectPath, string Key, string Value)
		{
			if(IgorAssert.EnsureTrue(ModuleInst, Directory.Exists(ProjectPath), "XCodeProj doesn't exist at path " + ProjectPath))
			{
				XCProject CurrentProject = new XCProject(ProjectPath);

				CurrentProject.Backup();

				foreach(KeyValuePair<string, XCBuildConfiguration> CurrentConfig in CurrentProject.buildConfigurations)
				{
					object BuildSettingsObj = CurrentConfig.Value.data["buildSettings"];
					PBXDictionary BuildSettingsDict = (PBXDictionary)BuildSettingsObj;

					if(BuildSettingsDict.ContainsKey(Key))
					{
						BuildSettingsDict[Key] = Value;

						IgorCore.Log(ModuleInst, "Updated KeyValuePair (Key: " + Key + " Value: " + Value + ") to build target GUID " + CurrentConfig.Key);
					}
					else
					{
						BuildSettingsDict.Add(Key, Value);

						IgorCore.Log(ModuleInst, "Added new KeyValuePair (Key: " + Key + " Value: " + Value + ") to build target GUID " + CurrentConfig.Key);
					}
				}

				CurrentProject.Save();
			}
		}

		public static void AddFrameworkSearchPath(IIgorModule ModuleInst, string ProjectPath, string NewPath)
		{
			if(IgorAssert.EnsureTrue(ModuleInst, Directory.Exists(ProjectPath), "XCodeProj doesn't exist at path " + ProjectPath))
			{
				XCProject CurrentProject = new XCProject(ProjectPath);

				CurrentProject.Backup();

				CurrentProject.AddFrameworkSearchPaths(NewPath);

				IgorCore.Log(ModuleInst, "Added framework search path " + NewPath);

				CurrentProject.Save();
			}
		}

		public static string AddNewFileReference(IIgorModule ModuleInst, string ProjectPath, string Filename, TreeEnum TreeBase, string Path = "", int FileEncoding = -1, string LastKnownFileType = "", string Name = "")
		{
			if(IgorAssert.EnsureTrue(ModuleInst, Directory.Exists(ProjectPath), "XCodeProj doesn't exist at path " + ProjectPath))
			{
				XCProject CurrentProject = new XCProject(ProjectPath);

				CurrentProject.Backup();

				foreach(KeyValuePair<string, PBXFileReference> CurrentFileRef in CurrentProject.fileReferences)
				{
					if(CurrentFileRef.Value.name == Filename)
					{
						IgorCore.Log(ModuleInst, "The file " + Filename + " is already referenced in the XCodeProj.");

						return CurrentFileRef.Value.guid;
					}
				}

				PBXFileReference NewFile = new PBXFileReference(Filename, TreeBase);

				if(Path != "")
				{
					if(NewFile.ContainsKey("path"))
					{
						NewFile.Remove("path");
					}
					
					NewFile.Add("path", Path);
				}
				
				if(FileEncoding != -1)
				{
					if(NewFile.ContainsKey("fileEncoding"))
					{
						NewFile.Remove("fileEncoding");
					}

					NewFile.Add("fileEncoding", FileEncoding);
				}

				if(LastKnownFileType != "")
				{
					if(NewFile.ContainsKey("lastKnownFileType"))
					{
						NewFile.Remove("lastKnownFileType");
					}
					
					NewFile.Add("lastKnownFileType", LastKnownFileType);
				}

				if(Name != "")
				{
					if(NewFile.ContainsKey("name"))
					{
						NewFile.Remove("name");
					}
					
					NewFile.Add("name", Name);
				}

				CurrentProject.fileReferences.Add(NewFile);

				IgorCore.Log(ModuleInst, "File " + Filename + " has been added to the XCodeProj.");

				CurrentProject.Save();

				return NewFile.guid;
			}

			return "";
		}

		public static string AddNewBuildFile(IIgorModule ModuleInst, string ProjectPath, string FileRefGUID)
		{
			if(IgorAssert.EnsureTrue(ModuleInst, Directory.Exists(ProjectPath), "XCodeProj doesn't exist at path " + ProjectPath))
			{
				XCProject CurrentProject = new XCProject(ProjectPath);

				CurrentProject.Backup();

				foreach(KeyValuePair<string, PBXBuildFile> CurrentBuildFile in CurrentProject.buildFiles)
				{
					if(IgorAssert.EnsureTrue(ModuleInst, CurrentBuildFile.Value.ContainsKey("fileRef"), "PBXBuildFile doesn't contain a key for fileRef."))
					{
						if(CurrentBuildFile.Value.data["fileRef"] == FileRefGUID)
						{
							IgorCore.Log(ModuleInst, "The file GUID " + FileRefGUID + " already has an associated BuildFile in the XCodeProj.");

							return CurrentBuildFile.Value.guid;
						}
					}
				}

				foreach(KeyValuePair<string, PBXFileReference> CurrentFileRef in CurrentProject.fileReferences)
				{
					if(CurrentFileRef.Value.guid == FileRefGUID)
					{
						PBXBuildFile NewBuildFile = new PBXBuildFile(CurrentFileRef.Value);

						CurrentProject.buildFiles.Add(NewBuildFile);

						IgorCore.Log(ModuleInst, "BuildFile for FileRefGUID " + FileRefGUID + " has been added to the XCodeProj.");

						CurrentProject.Save();

						return NewBuildFile.guid;
					}
				}
			}

			return "";
		}

		public static void AddFramework(IIgorModule ModuleInst, string ProjectPath, string Filename, TreeEnum TreeBase, string Path = "", int FileEncoding = -1, string LastKnownFileType = "", string Name = "")
		{
			string FrameworkFileRefGUID = AddNewFileReference(ModuleInst, ProjectPath, Filename, TreeBase, Path, FileEncoding, LastKnownFileType, Name);

			if(IgorAssert.EnsureTrue(ModuleInst, Directory.Exists(ProjectPath), "XCodeProj doesn't exist at path " + ProjectPath))
			{
				XCProject CurrentProject = new XCProject(ProjectPath);

				CurrentProject.Backup();

				if(IgorAssert.EnsureTrue(ModuleInst, CurrentProject != null, "XCodeProj couldn't be loaded."))
				{
					bool bFoundFrameworksGroup = false;

					foreach(KeyValuePair<string, PBXGroup> CurrentGroup in CurrentProject.groups)
					{
						if(CurrentGroup.Value.name == "Frameworks")
						{
							if(IgorAssert.EnsureTrue(ModuleInst, CurrentGroup.Value.ContainsKey("children"), "XCodeProj Frameworks PBXGroup doesn't have a children array."))
							{
								object FrameworkChildrenObj = CurrentGroup.Value.data["children"];

								if(IgorAssert.EnsureTrue(ModuleInst, FrameworkChildrenObj != null, "XCodeProj Frameworks PBXGroup has a children key, but it can't be retrieved."))
								{
									if(IgorAssert.EnsureTrue(ModuleInst, typeof(PBXList).IsAssignableFrom(FrameworkChildrenObj.GetType()), "XCodeProj Frameworks PBXGroup has a children key, but it can't be cast to PBXList."))
									{
										PBXList FrameworkChildrenList = (PBXList)FrameworkChildrenObj;

										if(IgorAssert.EnsureTrue(ModuleInst, FrameworkChildrenList != null, "XCodeProj casted Framework Children List is null."))
										{
											if(FrameworkChildrenList.Contains(FrameworkFileRefGUID))
											{
												IgorCore.Log(ModuleInst, "Framework " + Filename + " has already been added to the Framework Group " + CurrentGroup.Key + ".");
											}
											else
											{
												FrameworkChildrenList.Add(FrameworkFileRefGUID);

												CurrentGroup.Value.data["children"] = FrameworkChildrenList;

												IgorCore.Log(ModuleInst, "Added the " + Filename + " framework to the Framework Group " + CurrentGroup.Key + ".");
											}
										}
									}
								}

								bFoundFrameworksGroup = true;

								break;
							}
						}
					}

					IgorAssert.EnsureTrue(ModuleInst, bFoundFrameworksGroup, "Couldn't find a Frameworks PBXGroup in the XCodeProj.");

					CurrentProject.Save();
				}
			}

			string FrameworkBuildFileGUID = AddNewBuildFile(ModuleInst, ProjectPath, FrameworkFileRefGUID);

			if(IgorAssert.EnsureTrue(ModuleInst, Directory.Exists(ProjectPath), "XCodeProj doesn't exist at path " + ProjectPath))
			{
				XCProject CurrentProject = new XCProject(ProjectPath);

				CurrentProject.Backup();

				if(IgorAssert.EnsureTrue(ModuleInst, CurrentProject != null, "XCodeProj couldn't be loaded."))
				{
					foreach(KeyValuePair<string, PBXFrameworksBuildPhase> CurrentTarget in CurrentProject.frameworkBuildPhases)
					{
						if(IgorAssert.EnsureTrue(ModuleInst, CurrentTarget.Value.ContainsKey("files"), "XCodeProj Framework Build Phase doesn't have a files array."))
						{
							object FrameworkFilesObj = CurrentTarget.Value.data["files"];

							if(IgorAssert.EnsureTrue(ModuleInst, FrameworkFilesObj != null, "XCodeProj Framework Build Phase has a files key, but it can't be retrieved."))
							{
								if(IgorAssert.EnsureTrue(ModuleInst, typeof(PBXList).IsAssignableFrom(FrameworkFilesObj.GetType()), "XCodeProj Framework Build Phase has a files key, but it can't be cast to PBXList."))
								{
									PBXList FrameworkFilesList = (PBXList)FrameworkFilesObj;

									if(IgorAssert.EnsureTrue(ModuleInst, FrameworkFilesList != null, "XCodeProj casted Framework File List is null."))
									{
										if(FrameworkFilesList.Contains(FrameworkBuildFileGUID))
										{
											IgorCore.Log(ModuleInst, "Framework " + Filename + " has already been added to the Framework Build Phase " + CurrentTarget.Key + ".");
										}
										else
										{
											FrameworkFilesList.Add(FrameworkBuildFileGUID);

											CurrentTarget.Value.data["files"] = FrameworkFilesList;

											IgorCore.Log(ModuleInst, "Added the " + Filename + " framework to the Framework Build Phase " + CurrentTarget.Key + ".");
										}
									}
								}
							}
						}
					}

					CurrentProject.Save();
				}
			}
		}

		public static void SortGUIDIntoGroup(IIgorModule ModuleInst, string ProjectPath, string FileGUID, string GroupPath)
		{
			if(IgorAssert.EnsureTrue(ModuleInst, Directory.Exists(ProjectPath), "XCodeProj doesn't exist at path " + ProjectPath))
			{
				XCProject CurrentProject = new XCProject(ProjectPath);

				CurrentProject.Backup();

				if(IgorAssert.EnsureTrue(ModuleInst, CurrentProject != null, "XCodeProj couldn't be loaded."))
				{
					bool bFoundGroup = false;

					foreach(KeyValuePair<string, PBXGroup> CurrentGroup in CurrentProject.groups)
					{
						if(CurrentGroup.Value.path == GroupPath)
						{
							if(IgorAssert.EnsureTrue(ModuleInst, CurrentGroup.Value.ContainsKey("children"), "XCodeProj PBXGroup " + GroupPath + " doesn't have a children array."))
							{
								object GroupChildrenObj = CurrentGroup.Value.data["children"];

								if(IgorAssert.EnsureTrue(ModuleInst, GroupChildrenObj != null, "XCodeProj PBXGroup " + GroupPath + " has a children key, but it can't be retrieved."))
								{
									if(IgorAssert.EnsureTrue(ModuleInst, typeof(PBXList).IsAssignableFrom(GroupChildrenObj.GetType()), "XCodeProj PBXGroup " + GroupPath + " has a children key, but it can't be cast to PBXList."))
									{
										PBXList GroupChildrenList = (PBXList)GroupChildrenObj;

										if(IgorAssert.EnsureTrue(ModuleInst, GroupChildrenList != null, "XCodeProj casted Children List is null."))
										{
											if(GroupChildrenList.Contains(FileGUID))
											{
												IgorCore.Log(ModuleInst, "FileGUID " + FileGUID + " has already been added to the Group " + CurrentGroup.Key + ".");
											}
											else
											{
												GroupChildrenList.Add(FileGUID);

												CurrentGroup.Value.data["children"] = GroupChildrenList;

												IgorCore.Log(ModuleInst, "Added the " + FileGUID + " file to the Group " + CurrentGroup.Key + ".");
											}
										}
									}
								}

								bFoundGroup = true;

								break;
							}
						}
					}

					IgorAssert.EnsureTrue(ModuleInst, bFoundGroup, "Couldn't find a PBXGroup with path " + GroupPath + " in the XCodeProj.");

					CurrentProject.Save();
				}
			}
		}
	}
}