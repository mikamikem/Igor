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
	public class IgoriOSSourceUtils
	{
		public static void AddHeaderToAppControllerSource(IIgorModule ModuleInst, string PathToProject, string HeaderFile)
		{
			IgorUtils.ReplaceStringsInFile(ModuleInst, Path.Combine(PathToProject, Path.Combine("Classes", "UnityAppController.mm")),
				"@implementation UnityAppController", "#import \"" + HeaderFile + "\"\n@implementation UnityAppController");
		}

		public static void AddFunctionToAppControllerSource(IIgorModule ModuleInst, string PathToProject, string FunctionSource)
		{
			IgorUtils.ReplaceStringsInFile(ModuleInst, Path.Combine(PathToProject, Path.Combine("Classes", "UnityAppController.mm")),
				"@implementation UnityAppController", "@implementation UnityAppController\n" + FunctionSource);
		}

		public static void AddSourceToApplicationDidBecomeActive(IIgorModule ModuleInst, string PathToProject, string AdditionalSource)
		{
			IgorUtils.ReplaceStringsInFile(ModuleInst, Path.Combine(PathToProject, Path.Combine("Classes", "UnityAppController.mm")),
				"printf_console(\"-> applicationDidBecomeActive()\\n\");", AdditionalSource + "\nprintf_console(\"-> applicationDidBecomeActive()\\n\");");
		}
	}
}