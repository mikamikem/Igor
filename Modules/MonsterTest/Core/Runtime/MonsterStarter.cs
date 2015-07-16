#if MONSTER_TEST_RUNTIME || UNITY_EDITOR
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif // UNITY_EDITOR
using System;

namespace Igor
{
	public class MonsterStarter : MonoBehaviour
	{
		private static MonsterStarter Instance = null;

		public static MonsterStarter GetInstance(bool bAllowSearchInEditor = true)
		{
#if UNITY_EDITOR
			if(!EditorApplication.isPlaying && bAllowSearchInEditor)
			{
				MonsterStarter[] Starters = GameObject.FindObjectsOfType<MonsterStarter>();

				if(Starters.Length > 0)
				{
					return Starters[0];
				}
			}
#endif // UNITY_EDITOR

			return Instance;
		}

		public static MonsterTestRunner TestRunnerInst = null;
		public static string CurrentTestName = "";

		public static void CheckForAndRunTest()
		{
			string TestName = Environment.GetEnvironmentVariable(MonsterTestCore.MonsterStarterTestNameEnvVariable);

//			MonsterDebug.Log("Checking for test " + TestName);

			if(TestName != null && TestName != "" && (TestRunnerInst == null || TestName != CurrentTestName))
			{
				if(TestRunnerInst == null)
				{
					TestRunnerInst = new MonsterTestRunner();
				}

				TestRunnerInst.PreloadTest(TestName);
				TestRunnerInst.StartTest(TestName);

				MonsterDebug.Log("Starting test " + TestName);

				CurrentTestName = TestName;
			}

			if(TestRunnerInst != null && TestRunnerInst.RunTests())
			{
				MonsterDebug.Log("Finished running test " + TestName);

				Application.Quit();
			}
		}

		public virtual void Awake()
		{
			if(Instance == null)
			{
				DontDestroyOnLoad(gameObject);
				Instance = this;

				CheckForAndRunTest();
			}
			else
			{
				GameObject.DestroyImmediate(gameObject);
			}
		}

		public virtual void Update()
		{
			CheckForAndRunTest();
		}
	}
}

#endif // MONSTER_TEST_RUNTIME || UNITY_EDITOR
