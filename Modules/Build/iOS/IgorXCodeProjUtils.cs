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
			if(Directory.Exists(ProjectPath))
			{
				XCProject CurrentProject = new XCProject(ProjectPath);

				CurrentProject.Backup();

				string ProjectGUID = CurrentProject.project.guid;

				object ProjectSectionObj = CurrentProject.GetObject(ProjectGUID);

				if(ProjectSectionObj != null)
				{
					PBXDictionary ProjectSection = (PBXDictionary)ProjectSectionObj;

					object AttributesSectionObj = ProjectSection["attributes"];

					if(AttributesSectionObj != null)
					{
						object TargetAttributesObj = ((PBXDictionary)AttributesSectionObj)["TargetAttributes"];

						if(TargetAttributesObj != null)
						{
							PBXDictionary TargetAttributes = (PBXDictionary)TargetAttributesObj;

							object TargetsObj = ProjectSection["targets"];

							if(TargetsObj != null)
							{
								PBXList TargetsList = ((PBXList)TargetsObj);

								if(TargetsList.Count > 0)
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
								else
								{
									IgorCore.LogError(ModuleInst, "No build targets defined in XCodeProj.");
								}
							}
							else
							{
								IgorCore.LogError(ModuleInst, "Can't find Targets Section in Project Section.");
							}
						}
						else
						{
							IgorCore.LogError(ModuleInst, "Can't find TargetAttributes Section in Attributes Section.");
						}
					}
					else
					{
						IgorCore.LogError(ModuleInst, "Can't find Attributes Section in Project Section.");
					}
				}
				else
				{
					IgorCore.LogError(ModuleInst, "Can't find Project Section in XCodeProj.");
				}
			}
			else
			{
				IgorCore.LogError(ModuleInst, "XCodeProj doesn't exist at path " + ProjectPath);
			}
		}
	}
}