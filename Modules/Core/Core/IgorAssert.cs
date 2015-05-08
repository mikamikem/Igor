using System;
using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;

namespace Igor
{
	public class IgorAssert
	{
		public static bool EnsureTrue(IIgorModule Module, bool bTrue, string FailMessage)
		{
			if(!bTrue)
			{
				IgorCore.LogWarning(Module, FailMessage);

				Debug.Break();
			}

			return bTrue;
		}

		public static bool AssertTrue(IIgorModule Module, bool bTrue, string FailMessage)
		{
#if DEBUG
			if(!bTrue)
			{
				IgorCore.LogError(Module, FailMessage);

				Debug.Break();
			}
#endif

			return bTrue;
		}

		public static bool VerifyTrue(IIgorModule Module, bool bTrue, string FailMessage)
		{
			if(!bTrue)
			{
				IgorCore.LogWarning(Module, FailMessage);
			}

			return bTrue;
		}
	}
}