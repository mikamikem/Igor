using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.IO;
using System;
using System.Reflection;
using System.Xml.Serialization;

namespace System.Numerics
{
	public class BigInteger
	{
		public int Value;

		public BigInteger(byte[] ByteArray)
		{
			Value = BitConverter.ToInt32(ByteArray, 0);
		}

		public static implicit operator int(BigInteger Val)
		{
			return Val.Value;
		}
	}
}