using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.IO;
using System;
using System.Reflection;
using System.Xml.Serialization;
using Claunia.PropertyList;

namespace Igor
{
	public class IgorPlistUtils
	{
		public static void SetBoolValue(IIgorModule ModuleInst, string PlistPath, string BoolKey, bool bValue)
		{
			if(IgorAssert.EnsureTrue(ModuleInst, File.Exists(PlistPath), "Plist " + PlistPath + " doesn't exist!"))
			{
				FileInfo PlistFileInfo = new FileInfo(PlistPath);

				NSObject PlistRoot = PropertyListParser.Parse(PlistFileInfo);

				if(IgorAssert.EnsureTrue(ModuleInst, PlistRoot != null, "Plist " + PlistPath + " could not be parsed!"))
				{
					if(IgorAssert.EnsureTrue(ModuleInst, typeof(NSDictionary).IsAssignableFrom(PlistRoot.GetType()), "Plist " + PlistPath + " root object is not a dictionary."))
					{
						NSDictionary RootDictionary = (NSDictionary)PlistRoot;

						if(IgorAssert.EnsureTrue(ModuleInst, RootDictionary != null, "Plist root is not a dictionary."))
						{
							if(RootDictionary.ContainsKey(BoolKey))
							{
								RootDictionary[BoolKey] = new NSNumber(bValue);

								IgorRuntimeUtils.DeleteFile(PlistPath);

								PropertyListParser.SaveAsXml(RootDictionary, PlistFileInfo);

								IgorDebug.Log(ModuleInst, "Plist key " + BoolKey + " updated to " + bValue);
							}
							else
							{
								RootDictionary.Add(BoolKey, new NSNumber(bValue));

								IgorRuntimeUtils.DeleteFile(PlistPath);

								PropertyListParser.SaveAsXml(RootDictionary, PlistFileInfo);

								IgorDebug.Log(ModuleInst, "Plist key " + BoolKey + " added with value of " + bValue);
							}
						}
					}
				}
			}
		}

		public static string GetStringValue(IIgorModule ModuleInst, string PlistPath, string StringKey)
		{
			if(IgorAssert.EnsureTrue(ModuleInst, File.Exists(PlistPath), "Plist " + PlistPath + " doesn't exist!"))
			{
				FileInfo PlistFileInfo = new FileInfo(PlistPath);

				NSObject PlistRoot = PropertyListParser.Parse(PlistFileInfo);

				if(IgorAssert.EnsureTrue(ModuleInst, PlistRoot != null, "Plist " + PlistPath + " could not be parsed!"))
				{
					if(IgorAssert.EnsureTrue(ModuleInst, typeof(NSDictionary).IsAssignableFrom(PlistRoot.GetType()), "Plist " + PlistPath + " root object is not a dictionary."))
					{
						NSDictionary RootDictionary = (NSDictionary)PlistRoot;

						if(IgorAssert.EnsureTrue(ModuleInst, RootDictionary != null, "Plist root is not a dictionary."))
						{
							if(IgorAssert.EnsureTrue(ModuleInst, RootDictionary.ContainsKey(StringKey), "Plist does not contain " + StringKey + " in root dictionary."))
							{
								NSObject StringObj = RootDictionary.ObjectForKey(StringKey);

								if(IgorAssert.EnsureTrue(ModuleInst, typeof(NSString).IsAssignableFrom(StringObj.GetType()), "Plist key " + StringKey + " is not a string type."))
								{
									NSString StringValue = (NSString)StringObj;

									if(IgorAssert.EnsureTrue(ModuleInst, StringValue != null, "Plist key " + StringKey + " could not be cast to an NSString."))
									{
										return StringValue.GetContent();
									}
								}
							}
						}
					}
				}
			}

			return "";
		}

		public static void SetStringValue(IIgorModule ModuleInst, string PlistPath, string StringKey, string Value)
		{
			if(IgorAssert.EnsureTrue(ModuleInst, File.Exists(PlistPath), "Plist " + PlistPath + " doesn't exist!"))
			{
				FileInfo PlistFileInfo = new FileInfo(PlistPath);

				NSObject PlistRoot = PropertyListParser.Parse(PlistFileInfo);

				if(IgorAssert.EnsureTrue(ModuleInst, PlistRoot != null, "Plist " + PlistPath + " could not be parsed!"))
				{
					if(IgorAssert.EnsureTrue(ModuleInst, typeof(NSDictionary).IsAssignableFrom(PlistRoot.GetType()), "Plist " + PlistPath + " root object is not a dictionary."))
					{
						NSDictionary RootDictionary = (NSDictionary)PlistRoot;

						if(IgorAssert.EnsureTrue(ModuleInst, RootDictionary != null, "Plist root is not a dictionary."))
						{
							if(RootDictionary.ContainsKey(StringKey))
							{
								RootDictionary[StringKey] = new NSString(Value);

								IgorRuntimeUtils.DeleteFile(PlistPath);

								PropertyListParser.SaveAsXml(RootDictionary, PlistFileInfo);

								IgorDebug.Log(ModuleInst, "Plist key " + StringKey + " updated to " + Value);
							}
							else
							{
								RootDictionary.Add(StringKey, new NSString(Value));

								IgorRuntimeUtils.DeleteFile(PlistPath);

								PropertyListParser.SaveAsXml(RootDictionary, PlistFileInfo);

								IgorDebug.Log(ModuleInst, "Plist key " + StringKey + " added with value of " + Value);
							}
						}
					}
				}
			}
		}

		public static void AddRequiredDeviceCapability(IIgorModule ModuleInst, string PlistPath, string NewRequiredDeviceCapability)
		{
			if(IgorAssert.EnsureTrue(ModuleInst, File.Exists(PlistPath), "Plist " + PlistPath + " doesn't exist!"))
			{
				FileInfo PlistFileInfo = new FileInfo(PlistPath);

				NSObject PlistRoot = PropertyListParser.Parse(PlistFileInfo);

				if(IgorAssert.EnsureTrue(ModuleInst, PlistRoot != null, "Plist " + PlistPath + " could not be parsed!"))
				{
					if(IgorAssert.EnsureTrue(ModuleInst, typeof(NSDictionary).IsAssignableFrom(PlistRoot.GetType()), "Plist " + PlistPath + " root object is not a dictionary."))
					{
						NSDictionary RootDictionary = (NSDictionary)PlistRoot;

						if(IgorAssert.EnsureTrue(ModuleInst, RootDictionary != null, "Plist root is not a dictionary."))
						{
							if(IgorAssert.EnsureTrue(ModuleInst, RootDictionary.ContainsKey("UIRequiredDeviceCapabilities"), "Can't find UIRequiredDeviceCapabilities in plist."))
							{
								NSObject DeviceCapabilities = RootDictionary.Get("UIRequiredDeviceCapabilities");

								if(IgorAssert.EnsureTrue(ModuleInst, DeviceCapabilities != null, "Plist does not contain UIRequiredDeviceCapabilities."))
								{
									if(IgorAssert.EnsureTrue(ModuleInst, typeof(NSArray).IsAssignableFrom(DeviceCapabilities.GetType()), "Plist UIRequiredDeviceCapabilities is not an array."))
									{
										NSArray CapabilitiesArray = (NSArray)DeviceCapabilities;

										if(IgorAssert.EnsureTrue(ModuleInst, CapabilitiesArray != null, "UIRequiredDeviceCapabilities is not an array."))
										{
											if(CapabilitiesArray.ContainsObject(new NSString(NewRequiredDeviceCapability)))
											{
												IgorDebug.Log(ModuleInst, "UIRequiredDeviceCapabilities already contains " + NewRequiredDeviceCapability);
											}
											else
											{
												NSSet NewCapabilitiesSet = new NSSet(CapabilitiesArray.GetArray());

												NewCapabilitiesSet.AddObject(new NSString(NewRequiredDeviceCapability));

												NSArray NewCapabilitiesArray = new NSArray(NewCapabilitiesSet);

												RootDictionary["UIRequiredDeviceCapabilities"] = NewCapabilitiesArray;

												IgorRuntimeUtils.DeleteFile(PlistPath);

												PropertyListParser.SaveAsXml(RootDictionary, PlistFileInfo);

												IgorDebug.Log(ModuleInst, NewRequiredDeviceCapability + " added to UIRequiredDeviceCapabilities.");
											}
										}
									}
								}
							}
						}
					}
				}
			}
		}

		public static void AddBundleURLType(IIgorModule ModuleInst, string PlistPath, string NewURLScheme)
		{
			if(IgorAssert.EnsureTrue(ModuleInst, File.Exists(PlistPath), "Plist " + PlistPath + " doesn't exist!"))
			{
				FileInfo PlistFileInfo = new FileInfo(PlistPath);

				NSObject PlistRoot = PropertyListParser.Parse(PlistFileInfo);

				if(IgorAssert.EnsureTrue(ModuleInst, PlistRoot != null, "Plist " + PlistPath + " could not be parsed!"))
				{
					if(IgorAssert.EnsureTrue(ModuleInst, typeof(NSDictionary).IsAssignableFrom(PlistRoot.GetType()), "Plist " + PlistPath + " root object is not a dictionary."))
					{
						NSDictionary RootDictionary = (NSDictionary)PlistRoot;

						if(IgorAssert.EnsureTrue(ModuleInst, RootDictionary != null, "Plist root is not a dictionary."))
						{
							NSSet BundleURLTypes = null;

							if(RootDictionary.ContainsKey("CFBundleURLTypes"))
							{
								NSObject BundleURLTypesObj = RootDictionary.Get("CFBundleURLTypes");

								if(IgorAssert.EnsureTrue(ModuleInst, BundleURLTypesObj != null, "CFBundleURLTypes wasn't found in the root dictionary even though the key exists."))
								{
									if(IgorAssert.EnsureTrue(ModuleInst, typeof(NSArray).IsAssignableFrom(BundleURLTypesObj.GetType()), "CFBundleURLTypes isn't an NSArray."))
									{
										BundleURLTypes = new NSSet(((NSArray)BundleURLTypesObj).GetArray());
									}
								}
							}

							if(BundleURLTypes == null)
							{
								BundleURLTypes = new NSSet();
							}

							bool bAlreadyExists = false;

							foreach(NSObject CurrentURLType in BundleURLTypes)
							{
								if(bAlreadyExists)
								{
									break;
								}

								if(IgorAssert.EnsureTrue(ModuleInst, typeof(NSDictionary).IsAssignableFrom(CurrentURLType.GetType()), "One of the CFBundleURLTypes isn't an NSDictionary."))
								{
									NSDictionary CurrentURLTypeDict = (NSDictionary)CurrentURLType;

									if(IgorAssert.EnsureTrue(ModuleInst, CurrentURLTypeDict != null, "One of the CFBundleURLTypes didn't cast to NSDictionary correctly."))
									{
										if(CurrentURLTypeDict.ContainsKey("CFBundleURLSchemes"))
										{
											NSObject CurrentURLSchemesArrayObj = CurrentURLTypeDict.Get("CFBundleURLSchemes");

											if(IgorAssert.EnsureTrue(ModuleInst, typeof(NSArray).IsAssignableFrom(CurrentURLSchemesArrayObj.GetType()), "A CFBundleURLSchemes key exists for a given CFBundleURLType, but it's not an NSArray type."))
											{
												NSArray CurrentURLSchemesArray = (NSArray)CurrentURLSchemesArrayObj;

												if(IgorAssert.EnsureTrue(ModuleInst, CurrentURLSchemesArray != null, "The CFBundleURLSchemes object didn't cast to NSDictionary correctly."))
												{
													NSSet CurrentURLSchemesSet = new NSSet(CurrentURLSchemesArray.GetArray());

													foreach(NSObject CurrentURLSchemeObj in CurrentURLSchemesSet)
													{
														if(IgorAssert.EnsureTrue(ModuleInst, typeof(NSString).IsAssignableFrom(CurrentURLSchemeObj.GetType()), "One of the CFBundleURLSchemes is not an NSString."))
														{
															NSString CurrentURLScheme = (NSString)CurrentURLSchemeObj;

															if(IgorAssert.EnsureTrue(ModuleInst, CurrentURLScheme != null, "A CFBundleURLScheme entry didn't cast to NSString correctly."))
															{
																if(CurrentURLScheme.GetContent() == NewURLScheme)
																{
																	bAlreadyExists = true;

																	IgorDebug.Log(ModuleInst, "URL scheme " + NewURLScheme + " is already in " + PlistPath);

																	break;
																}
															}
														}
													}
												}
											}
										}
									}
								}
							}

							if(!bAlreadyExists)
							{
								NSString NewSchemeString = new NSString(NewURLScheme);

								NSArray NewSchemeArray = new NSArray(1);

								NewSchemeArray.SetValue(0, NewSchemeString);

								NSDictionary NewTypeDictionary = new NSDictionary();

								NewTypeDictionary.Add("CFBundleURLSchemes", NewSchemeArray);

								BundleURLTypes.AddObject(NewTypeDictionary);

								NSArray BundleURLTypesArray = new NSArray(BundleURLTypes.AllObjects());

								if(RootDictionary.ContainsKey("CFBundleURLTypes"))
								{
									RootDictionary["CFBundleURLTypes"] = BundleURLTypesArray;

									IgorDebug.Log(ModuleInst, "Updated CFBundleURLTypes to add " + NewURLScheme + ".");
								}
								else
								{
									RootDictionary.Add("CFBundleURLTypes", BundleURLTypesArray);

									IgorDebug.Log(ModuleInst, "Added CFBundleURLTypes to add " + NewURLScheme + ".");
								}

								IgorRuntimeUtils.DeleteFile(PlistPath);

								PropertyListParser.SaveAsXml(RootDictionary, PlistFileInfo);
							}
						}
					}
				}
			}
		}
	}
}