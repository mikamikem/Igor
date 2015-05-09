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
	[XmlRoot("IgorConfig")]
	public class IgorConfig
	{
		public static string IgorConfigFilename = "IgorConfig.xml";
		public static string DefaultConfigPath = Path.Combine(IgorUpdater.BaseIgorDirectory, IgorConfigFilename);
		private static IgorConfig Instance = null;

		public static IgorConfig GetInstance()
		{
			if(!File.Exists(DefaultConfigPath))
			{
				InitializeStartingConfig();
			}

			if(Instance == null)
			{
				Instance = Load(DefaultConfigPath);
			}

			return Instance;
		}

		public static IgorConfig ReGetInstance()
		{
			Instance = null;

			return GetInstance();
		}

		public static void InitializeStartingConfig()
		{
			IgorConfig NewInst = new IgorConfig();

			NewInst.Save();
		}

		public void Save()
		{
			Save(DefaultConfigPath);
		}

	 	public void Save(string path)
	 	{
	 		XmlSerializer serializer = new XmlSerializer(typeof(IgorConfig));

	 		IgorUtils.DeleteFile(path);

	 		using(FileStream stream = new FileStream(path, FileMode.Create))
	 		{
	 			serializer.Serialize(stream, this);
	 		}
	 	}
	 
	 	public static IgorConfig Load(string path)
	 	{
	 		XmlSerializer serializer = new XmlSerializer(typeof(IgorConfig));

	 		if(File.Exists(path))
			{
				File.SetAttributes(path, System.IO.FileAttributes.Normal);
			}

	 		using(var stream = new FileStream(path, FileMode.Open))
	 		{
	 			return serializer.Deserialize(stream) as IgorConfig;
	 		}
	 	}

	 	public class IgorConfigKeyValuePair<KeyType, ValueType>
	 	{
	 		public KeyType Key;
	 		public ValueType Value;

	 		public IgorConfigKeyValuePair()
	 		{}

	 		public IgorConfigKeyValuePair(KeyType NewKey, ValueType NewValue)
	 		{
	 			Key = NewKey;
	 			Value = NewValue;
	 		}
	 	}

		[XmlArray("EnabledModules")]
		[XmlArrayItem("Module")]
		public List<string> EnabledModules = new List<string>();

		[XmlArray("ModuleBoolValues")]
		[XmlArrayItem("ConfigValue")]
		public List<IgorConfigKeyValuePair<string, bool>> ModuleBools = new List<IgorConfigKeyValuePair<string, bool>>();

		[XmlArray("ModuleStringValues")]
		[XmlArrayItem("ConfigValue")]
		public List<IgorConfigKeyValuePair<string, string>> ModuleStrings = new List<IgorConfigKeyValuePair<string, string>>();

		[XmlArray("JobConfigs")]
		[XmlArrayItem("Job")]
		public List<IgorPersistentJobConfig> JobConfigs = new List<IgorPersistentJobConfig>();

		public virtual List<string> GetEnabledModuleNames()
		{
			return EnabledModules;
		}

		public static bool GetModuleBool(IIgorModule Module, string BoolKey, bool bDefaultValue = false)
		{
			IgorConfig Inst = GetInstance();

			if(Inst != null)
			{
				string FullKey = BoolKey;

				if(Module != null)
				{
					FullKey = Module.GetModuleName() + "." + FullKey;
				}

				foreach(IgorConfigKeyValuePair<string, bool> CurrentValue in Inst.ModuleBools)
				{
					if(CurrentValue.Key == FullKey)
					{
						return CurrentValue.Value;
					}
				}

				return bDefaultValue;
			}

			return bDefaultValue;
		}

		public static void SetModuleBool(IIgorModule Module, string BoolKey, bool bValue)
		{
			IgorConfig Inst = GetInstance();

			if(Inst != null)
			{
				string FullKey = BoolKey;

				if(Module != null)
				{
					FullKey = Module.GetModuleName() + "." + FullKey;
				}

				int CurrentIndex = 0;

				foreach(IgorConfigKeyValuePair<string, bool> CurrentValue in Inst.ModuleBools)
				{
					if(CurrentValue.Key == FullKey)
					{
						Inst.ModuleBools[CurrentIndex].Value = bValue;

						return;
					}

					++CurrentIndex;
				}

				IgorConfigKeyValuePair<string, bool> NewInst = new IgorConfigKeyValuePair<string, bool>(FullKey, bValue);

				Inst.ModuleBools.Add(NewInst);
			}
		}

		public static string GetModuleString(IIgorModule Module, string StringKey, string DefaultValue = "")
		{
			IgorConfig Inst = GetInstance();

			if(Inst != null)
			{
				string FullKey = StringKey;

				if(Module != null)
				{
					FullKey = Module.GetModuleName() + "." + FullKey;
				}

				foreach(IgorConfigKeyValuePair<string, string> CurrentValue in Inst.ModuleStrings)
				{
					if(CurrentValue.Key == FullKey)
					{
						return CurrentValue.Value;
					}
				}

				return DefaultValue;
			}

			return DefaultValue;
		}

		public static void SetModuleString(IIgorModule Module, string StringKey, string Value)
		{
			IgorConfig Inst = GetInstance();

			if(Inst != null)
			{
				string FullKey = StringKey;

				if(Module != null)
				{
					FullKey = Module.GetModuleName() + "." + FullKey;
				}

				int CurrentIndex = 0;

				foreach(IgorConfigKeyValuePair<string, string> CurrentValue in Inst.ModuleStrings)
				{
					if(CurrentValue.Key == FullKey)
					{
						Inst.ModuleStrings[CurrentIndex].Value = Value;

						return;
					}

					++CurrentIndex;
				}

				IgorConfigKeyValuePair<string, string> NewInst = new IgorConfigKeyValuePair<string, string>(FullKey, Value);

				Inst.ModuleStrings.Add(NewInst);
			}
		}

		public static IgorPersistentJobConfig GetJobByName(string JobName)
		{
			IgorConfig Inst = GetInstance();

			if(Inst != null)
			{
				foreach(IgorPersistentJobConfig CurrentJob in Inst.JobConfigs)
				{
					if(CurrentJob.JobName == JobName)
					{
						return CurrentJob;
					}
				}
			}

			return null;
		}

		public static void CreateOrUpdateSavedJob(string JobName, IgorPersistentJobConfig JobConfig)
		{
			IgorConfig Inst = GetInstance();

			if(Inst != null)
			{
				int CurrentJobIndex = 0;

				foreach(IgorPersistentJobConfig CurrentJob in Inst.JobConfigs)
				{
					if(CurrentJob.JobName == JobName)
					{
						Inst.JobConfigs[CurrentJobIndex] = JobConfig;

						Inst.Save();

						return;
					}

					++CurrentJobIndex;
				}

				Inst.JobConfigs.Add(JobConfig);

				Inst.Save();
			}
		}

		public static List<IgorPersistentJobConfig> GetAllJobs()
		{
			IgorConfig Inst = GetInstance();

			if(Inst != null)
			{
				return Inst.JobConfigs;
			}

			return null;
		}

		public static void TriggerJobByName(string JobName, bool bFromMenu, bool bAttemptUpdate)
		{
			IgorConfig Inst = GetInstance();

			if(Inst != null)
			{
				foreach(IgorPersistentJobConfig CurrentJob in Inst.JobConfigs)
				{
					if(CurrentJob.JobName == JobName)
					{
						IgorJobConfig NewConfig = new IgorJobConfig();

						NewConfig.Persistent = CurrentJob;

						NewConfig.Save(IgorJobConfig.IgorJobConfigPath);

					    if(!bAttemptUpdate)
					    {
					        IgorCore.RunJob(bFromMenu);
					    }
					    else
					    {
					        IgorCore.UpdateAndRunJob();
					    }

					    return;
					}
				}
			}
		}
	}
}