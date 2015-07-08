using System;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif // UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;

namespace Igor
{
	public class IgorAssert
	{
		protected static bool bJobFailed = false;

		public static void StartJob()
		{
			bJobFailed = false;
		}

		public static void JobFailed()
		{
			bJobFailed = true;
		}

		public static bool HasJobFailed()
		{
			return bJobFailed;
		}

		public static bool EnsureTrue(IIgorModule Module, bool bTrue, string FailMessage)
		{
			if(!bTrue)
			{
				IgorDebug.LogWarning(Module, FailMessage);

				JobFailed();

				Debug.Break();
			}

			return bTrue;
		}

		public static bool AssertTrue(IIgorModule Module, bool bTrue, string FailMessage)
		{
#if DEBUG
			if(!bTrue)
			{
				IgorDebug.LogError(Module, FailMessage);

				JobFailed();

				Debug.Break();
			}
#endif

			return bTrue;
		}

		public static bool VerifyTrue(IIgorModule Module, bool bTrue, string FailMessage)
		{
			if(!bTrue)
			{
				IgorDebug.LogWarning(Module, FailMessage);
			}

			return bTrue;
		}
	}
}