#if MONSTER_TEST_RUNTIME || UNITY_EDITOR
using UnityEngine;

namespace Igor
{
	public class MonsterTestRunner
	{
		public string RunningTestName = "";
		public MonsterTest CurrentTest = null;
		public IGraphEvent CurrentTestEvent = null;

		public virtual void PreloadTest(string TestName)
		{
			RunningTestName = TestName;

			CurrentTest = MonsterTest.LoadMonsterTest(TestName);
		}

		public virtual void StartTest(string TestName)
		{
			if(RunningTestName != TestName)
			{
				RunningTestName = TestName;

				CurrentTest = MonsterTest.LoadMonsterTest(TestName);
			}

			CurrentTestEvent = CurrentTest.GetStartingEvent();

			if(CurrentTestEvent == null)
			{
				CurrentTest.TestSucceeded();
			}
		}

		public virtual bool RunTests()
		{
			bool bHasEventToExecute = CurrentTestEvent != null;

			while(bHasEventToExecute && CurrentTestEvent != null)
			{
				bool bStillRunning = CurrentTestEvent.ExecuteEvent();
				
				if(bStillRunning)
				{
					bHasEventToExecute = false;
				}
				else
				{
					IGraphEvent PreviousEvent = CurrentTestEvent;

					bHasEventToExecute = CurrentTestEvent.IsNextEventImmediate();
					CurrentTestEvent = CurrentTestEvent.GetNextEvent();
				}
			}

			return CurrentTestEvent == null;
		}
	}
}

#endif // MONSTER_TEST_RUNTIME || UNITY_EDITOR
