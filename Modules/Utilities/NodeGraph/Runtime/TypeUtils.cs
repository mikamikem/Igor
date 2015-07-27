#if IGOR_RUNTIME || UNITY_EDITOR
using UnityEngine;
using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Xml.Serialization;
using System.Reflection;

namespace Igor
{
	public class TypeUtils
	{
		public static void RegisterAllTypes()
		{
			List<Type> XMLTypes = IgorRuntimeUtils.GetTypesInheritFrom<XMLSerializable>();

			foreach(Type CurrentXMLType in XMLTypes)
			{
				MethodInfo RegisterFunction = CurrentXMLType.GetMethod("RegisterType", BindingFlags.Static | BindingFlags.Public | BindingFlags.DeclaredOnly);

				if(RegisterFunction != null)
				{
					RegisterFunction.Invoke(null, new object[]{});
				}
			}

			XMLSerializable.bSafeToLoad = true;
		}
		
		public enum GlobalSerializationVersion
		{
			Version_Max
		}
		
		public static int GetSerializeVersion()
		{
			return ((int)GlobalSerializationVersion.Version_Max)-1;
		}
		
		public delegate object CreateNewEntityOfType();
		public delegate XmlSerializer CreateNewXMLSerializerOfType();
		public delegate object CreateEditorBoxForType(object Owner, object EntityToWatch);
		
		private struct FunctionTable
		{
			public FunctionTable(CreateNewEntityOfType InEntity, CreateNewXMLSerializerOfType InXML, CreateEditorBoxForType InEditorBox)
			{
				NewEntity = InEntity;
				NewXMLSerializer = InXML;
				NewEditorBox = InEditorBox;
			}
			
			public CreateNewEntityOfType NewEntity;
			public CreateNewXMLSerializerOfType NewXMLSerializer;
			public CreateEditorBoxForType NewEditorBox;
		}
		
		static private Dictionary<string, FunctionTable> TypeNameToFunctionTable;
		
		static public void RegisterType(string TypeName, CreateNewEntityOfType TypeGenerator, CreateNewXMLSerializerOfType XMLGenerator)
		{
			if(TypeNameToFunctionTable == null)
			{
				TypeNameToFunctionTable = new Dictionary<string, FunctionTable>();
			}
			
			if(TypeNameToFunctionTable.ContainsKey(TypeName))
			{
				TypeNameToFunctionTable[TypeName] = new FunctionTable(TypeGenerator, XMLGenerator, TypeNameToFunctionTable[TypeName].NewEditorBox);
			}
			else
			{
				TypeNameToFunctionTable.Add(TypeName, new FunctionTable(TypeGenerator, XMLGenerator, null));
			}
		}
		
		static public void RegisterEditorType(string TypeName, CreateEditorBoxForType InNewEditorBox)
		{
			if(TypeNameToFunctionTable == null)
			{
				TypeNameToFunctionTable = new Dictionary<string, FunctionTable>();
			}
			
			if(TypeNameToFunctionTable.ContainsKey(TypeName))
			{
				TypeNameToFunctionTable[TypeName] = new FunctionTable(TypeNameToFunctionTable[TypeName].NewEntity, TypeNameToFunctionTable[TypeName].NewXMLSerializer, InNewEditorBox);
			}
			else
			{
				TypeNameToFunctionTable.Add(TypeName, new FunctionTable(null, null, InNewEditorBox));
			}
		}
		
		static public object GetNewObjectOfTypeString(string TypeName)
		{
			if(TypeNameToFunctionTable != null && TypeNameToFunctionTable.ContainsKey(TypeName))
			{
				return TypeNameToFunctionTable[TypeName].NewEntity();
			}

			UnityEngine.Debug.LogError("Could not get new object of type " + TypeName);

			return null;
		}

		static public XmlSerializer GetXMLSerializerForTypeString(string TypeName)
		{
			if(TypeNameToFunctionTable != null && TypeNameToFunctionTable.ContainsKey(TypeName))
			{
				try
				{
					return TypeNameToFunctionTable[TypeName].NewXMLSerializer();
				}
				catch(Exception Ex)
				{
					UnityEngine.Debug.LogError("Error creating a TypeUtils XML serializer for type " + TypeName + "!!  Exception message is\n" + Ex.Message + "\nAnd inner exception is\n" + Ex.InnerException.Message);
				}
			}
			
			UnityEngine.Debug.LogError("Could not get XMLSerializer for type " + TypeName);
			
			return null;
		}
		
		static public object GetEditorBoxForTypeString(string TypeName, object Owner, object EntityToWatch)
		{
			if(TypeNameToFunctionTable != null && TypeNameToFunctionTable.ContainsKey(TypeName) &&
			   TypeNameToFunctionTable[TypeName].NewEditorBox != null)
			{
				return TypeNameToFunctionTable[TypeName].NewEditorBox(Owner, EntityToWatch);
			}
			
			UnityEngine.Debug.LogError("Could not get editor box for type " + TypeName);
			
			return null;
		}
	}
}

#endif // IGOR_RUNTIME || UNITY_EDITOR
