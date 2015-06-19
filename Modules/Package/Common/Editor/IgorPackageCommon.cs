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
	public class IgorPackageCommon : IgorModuleBase
	{
		public static StepID PackageStep = new StepID("Package", 750);

		public override string GetModuleName()
		{
			return "Package.Common";
		}

		public override void RegisterModule()
		{
			IgorCore.RegisterNewModule(this);
		}

		public override void ProcessArgs(IIgorStepHandler StepHandler)
		{
		}
	}
}