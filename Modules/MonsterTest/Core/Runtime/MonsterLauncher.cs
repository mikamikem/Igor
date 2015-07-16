#if MONSTER_TEST_RUNTIME || UNITY_EDITOR
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif // UNITY_EDITOR
using System;

namespace Igor
{
	public class MonsterLauncher : MonoBehaviour
	{
		private static MonsterLauncher Instance = null;

		public static MonsterLauncher GetInstance(bool bAllowSearchInEditor = true)
		{
#if UNITY_EDITOR
			if(!EditorApplication.isPlaying && bAllowSearchInEditor)
			{
				MonsterLauncher[] Starters = GameObject.FindObjectsOfType<MonsterLauncher>();

				if(Starters.Length > 0)
				{
					return Starters[0];
				}
			}
#endif // UNITY_EDITOR

			return Instance;
		}

		public static string CurrentJobName = "";

		public static void CheckForAndRunJobs()
		{
			string JobName = Environment.GetEnvironmentVariable(MonsterTestCore.MonsterLauncherJobNameEnvVariable);

			MonsterDebug.Log("Checking for job " + JobName);

			if(JobName != null && JobName != "" && JobName != CurrentJobName)
			{
				IgorConfig.SetJobToRunByName(JobName);

				MonsterDebug.Log("Starting job " + JobName);

				CurrentJobName = JobName;
			}

	        IgorCore.HandleJobStatus(IgorCore.RunJob(false));
		}

		public virtual void Awake()
		{
			if(Instance == null)
			{
				DontDestroyOnLoad(gameObject);
				Instance = this;

				CheckForAndRunJobs();
			}
			else
			{
				GameObject.DestroyImmediate(gameObject);
			}
		}

		public virtual void Update()
		{
			CheckForAndRunJobs();
		}
	}
}

#endif // MONSTER_TEST_RUNTIME || UNITY_EDITOR
