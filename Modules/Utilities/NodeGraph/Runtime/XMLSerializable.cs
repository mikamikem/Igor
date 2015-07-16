#if IGOR_RUNTIME || UNITY_EDITOR
using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace Igor
{
	public class XMLSerializable : IXmlSerializable {
		
		public enum ModifiedState
		{
			Unmodified,
			Added,
			PreModify,
			Modified,
			Deleted
		}
		
		public static bool bSafeToLoad = false;
		
		public enum LinkType
		{
			LINK_NormalLink,
			LINK_StartLink,
			LINK_EndLink
		};
		
		protected bool bReading = false;
		protected bool bEmbedded = false;
		protected int SerializedFromVersion = 0;
		protected XmlReader InternalReader = null;
		protected XmlWriter InternalWriter = null;
		protected string Filename = "";
		protected bool bReadStartElement = false;

		public virtual string GetFilename()
		{
			return Filename;
		}
		
	#if UNITY_EDITOR
		public virtual void EditorSetFilename(string NewFilename)
		{
			Filename = NewFilename;
		}
		
		public virtual void EditorSetDisplayName(string NewName)
		{
		}

		public virtual string EditorGetDisplayName()
		{
			return Filename;
		}

		public virtual string EditorGetDragAndDropText()
		{
			return "";
		}

		public virtual string EditorGetCompareString()
		{
			return "";
		}
	#endif // UNITY_EDITOR
		
		public virtual void SerializeListOwnerPrefixFixup()
		{
		}
		
		public virtual void ListFixup<EntityType>(ref List<EntityLink<EntityType>> ListToFix, XMLSerializable Owner, string ListPrefix, LinkType ListType) where EntityType : LinkedEntity<EntityType>, new()
		{
			for(int CurrentEntry = 0; CurrentEntry < ListToFix.Count; ++CurrentEntry)
			{
				ListToFix[CurrentEntry].SetOwner(Owner);
				ListToFix[CurrentEntry].ListPrefix = ListPrefix;
				ListToFix[CurrentEntry].CurrentLinkType = ListType;
			}
		}

		public void InternalSerializeXML(XmlReader Reader, XmlWriter Writer)
		{
			if(Reader != null)
			{
				bReading = true;
				InternalReader = Reader;
				
				SerializeXML();

				InternalReader.ReadEndElement();
				
				InternalReader = null;
			}
			else
			{
				bReading = false;
				InternalWriter = Writer;

				SerializeXML();

				InternalWriter = null;
			}
		}
		
		public virtual void SerializeXML()
		{
		}
		
		public void SerializeBegin(string ClassName, string TemplatedClassName1 = "", string TemplatedClassName2 = "")
		{
			string CombinedStringName = ClassName;

			if(CombinedStringName.Contains("."))
			{
				CombinedStringName = CombinedStringName.Substring(CombinedStringName.IndexOf('.'));
			}
			
			if(TemplatedClassName1.Length > 0)
			{
				CombinedStringName += "Of" + (TemplatedClassName1.Contains(".") ? TemplatedClassName1.Substring(TemplatedClassName1.IndexOf('.') + 1) : TemplatedClassName1);

				if(TemplatedClassName2.Length > 0)
				{
					CombinedStringName += (TemplatedClassName2.Contains(".") ? TemplatedClassName2.Substring(TemplatedClassName2.IndexOf('.') + 1) : TemplatedClassName2);
				}
			}

			CombinedStringName = CombinedStringName.Replace('.', '_');
			
			if(bReading)
			{
				if(!bEmbedded)
				{
					InternalReader.MoveToContent();
				}

				if(!bReadStartElement)
				{
					InternalReader.ReadStartElement(CombinedStringName);
				}

				bReadStartElement = false;

				SerializedFromVersion = InternalReader.ReadElementContentAsInt("SerializeVersion", "");
			}
			else
			{
				if(bEmbedded)
				{
					InternalWriter.WriteStartElement(CombinedStringName);
				}
				
				InternalWriter.WriteStartElement("SerializeVersion");
				InternalWriter.WriteValue(TypeUtils.GetSerializeVersion());
				InternalWriter.WriteEndElement();
				
				SerializedFromVersion = TypeUtils.GetSerializeVersion();
			}
		}
		
		public void SerializeString(string Key, ref string Value)
		{
			if(bReading)
			{
				Value = InternalReader.ReadElementContentAsString(Key, "");
			}
			else
			{
				InternalWriter.WriteElementString(Key, Value);
			}
		}
		
		public void SerializeFloat(string Key, ref float Value)
		{
			if(bReading)
			{
				Value = InternalReader.ReadElementContentAsFloat(Key, "");
			}
			else
			{
				InternalWriter.WriteElementString(Key, Value.ToString());
			}
		}
		
		public void SerializeInt(string Key, ref int Value)
		{
			if(bReading)
			{
				Value = InternalReader.ReadElementContentAsInt(Key, "");
			}
			else
			{
				InternalWriter.WriteElementString(Key, Value.ToString());
			}
		}
		
		public void SerializeBool(string Key, ref bool Value)
		{
			if(bReading)
			{
				Value = InternalReader.ReadElementContentAsString(Key, "") == "True";
			}
			else
			{
				InternalWriter.WriteElementString(Key, Value.ToString());
			}
		}
		
		public void SerializeVector2(string Key, ref Vector2 Value)
		{
			if(bReading)
			{
				Value.x = InternalReader.ReadElementContentAsFloat(Key + ".x", "");
				Value.y = InternalReader.ReadElementContentAsFloat(Key + ".y", "");
			}
			else
			{
				InternalWriter.WriteElementString(Key + ".x", Value.x.ToString());
				InternalWriter.WriteElementString(Key + ".y", Value.y.ToString());
			}
		}
		
		public void SerializeVector3(string Key, ref Vector3 Value)
		{
			if(bReading)
			{
				Value.x = InternalReader.ReadElementContentAsFloat(Key + ".x", "");
				Value.y = InternalReader.ReadElementContentAsFloat(Key + ".y", "");
				Value.z = InternalReader.ReadElementContentAsFloat(Key + ".z", "");
			}
			else
			{
				InternalWriter.WriteElementString(Key + ".x", Value.x.ToString());
				InternalWriter.WriteElementString(Key + ".y", Value.y.ToString());
				InternalWriter.WriteElementString(Key + ".z", Value.z.ToString());
			}
		}

		public void SerializeVector4(string Key, ref Vector4 Value)
		{
			if(bReading)
			{
				Value.x = InternalReader.ReadElementContentAsFloat(Key + ".x", "");
				Value.y = InternalReader.ReadElementContentAsFloat(Key + ".y", "");
				Value.z = InternalReader.ReadElementContentAsFloat(Key + ".z", "");
				Value.w = InternalReader.ReadElementContentAsFloat(Key + ".w", "");
			}
			else
			{
				InternalWriter.WriteElementString(Key + ".x", Value.x.ToString());
				InternalWriter.WriteElementString(Key + ".y", Value.y.ToString());
				InternalWriter.WriteElementString(Key + ".z", Value.z.ToString());
				InternalWriter.WriteElementString(Key + ".w", Value.w.ToString());
			}
		}

		public void SerializeQuaternion(string Key, ref Quaternion Value)
		{
			if(bReading)
			{
				Value.x = InternalReader.ReadElementContentAsFloat(Key + ".x", "");
				Value.y = InternalReader.ReadElementContentAsFloat(Key + ".y", "");
				Value.z = InternalReader.ReadElementContentAsFloat(Key + ".z", "");
				Value.w = InternalReader.ReadElementContentAsFloat(Key + ".w", "");
			}
			else
			{
				InternalWriter.WriteElementString(Key + ".x", Value.x.ToString());
				InternalWriter.WriteElementString(Key + ".y", Value.y.ToString());
				InternalWriter.WriteElementString(Key + ".z", Value.z.ToString());
				InternalWriter.WriteElementString(Key + ".w", Value.w.ToString());
			}
		}

		public void SerializeDateTime(string Key, ref DateTime Value)
		{
			if(bReading)
			{
				int Day = InternalReader.ReadElementContentAsInt(Key + ".Day", "");
				int Month = InternalReader.ReadElementContentAsInt(Key + ".Month", "");
				int Year = InternalReader.ReadElementContentAsInt(Key + ".Year", "");
				int Hour = InternalReader.ReadElementContentAsInt(Key + ".Hour", "");
				int Minute = InternalReader.ReadElementContentAsInt(Key + ".Minute", "");
				int Second = InternalReader.ReadElementContentAsInt(Key + ".Second", "");

				Value = new DateTime(Year, Month, Day, Hour, Minute, Second);
			}
			else
			{
				InternalWriter.WriteElementString(Key + ".Day", Value.Day.ToString());
				InternalWriter.WriteElementString(Key + ".Month", Value.Month.ToString());
				InternalWriter.WriteElementString(Key + ".Year", Value.Year.ToString());
				InternalWriter.WriteElementString(Key + ".Hour", Value.Hour.ToString());
				InternalWriter.WriteElementString(Key + ".Minute", Value.Minute.ToString());
				InternalWriter.WriteElementString(Key + ".Second", Value.Second.ToString());
			}
		}
		
		public void SerializeEntityID(string Key, ref EntityID Value)
		{
			if(Value == null)
			{
				Value = new EntityID();
			}
			
			SerializeInt(Key + "SerializedVersion", ref Value.SerializedVersion);
			SerializeString(Key + "Chapter", ref Value.ChapterFilename);
			SerializeString(Key + "Scene", ref Value.SceneFilename);
			SerializeString(Key + "Dialogue", ref Value.DialogueFilename);
			SerializeString(Key + "Conversation", ref Value.ConversationFilename);
		}
		
		public delegate void ToFromDelegate<SourceType, SavedType>(ref SourceType SourceIn, ref SavedType SavedIn);
		
		public void SerializeStringList<SourceType>(string ListName, string ElementName, ToFromDelegate<SourceType, string> FunctionDelegate, ref List<SourceType> ListToSerialize)
		{
			if(bReading)
			{
				bool isEmptyElement = InternalReader.IsEmptyElement;

				InternalReader.ReadStartElement(ListName);
		
				if (!isEmptyElement)
				{
					int CurrentDepth = InternalReader.Depth;
					while(InternalReader.Depth >= CurrentDepth)
					{
						InternalReader.ReadStartElement(ElementName);
						SourceType Temp = default(SourceType);
						string Content = InternalReader.ReadContentAsString();
						FunctionDelegate(ref Temp, ref Content);
						if(Temp != null)
						{
							ListToSerialize.Add(Temp);
						}
						InternalReader.ReadEndElement();
					}
					InternalReader.ReadEndElement();
				}
			}
			else
			{
				InternalWriter.WriteStartElement(ListName);
				if(ListToSerialize.Count > 0)
				{
					foreach(SourceType CurrentSource in ListToSerialize)
					{
						SourceType CurrentSourceToRef = CurrentSource;
						string ContentToWrite = "";
						FunctionDelegate(ref CurrentSourceToRef, ref ContentToWrite);
						InternalWriter.WriteElementString(ElementName, ContentToWrite);
					}
				}
				InternalWriter.WriteEndElement();
			}
		}

		public void SerializeStringList(string ListName, string ElementName, ref List<string> ListToSerialize)
		{
			if(bReading)
			{
				bool isEmptyElement = InternalReader.IsEmptyElement;

				InternalReader.ReadStartElement(ListName);
		
				if (!isEmptyElement)
				{
					int CurrentDepth = InternalReader.Depth;
					while(InternalReader.Depth >= CurrentDepth)
					{
						InternalReader.ReadStartElement(ElementName);
						string Content = InternalReader.ReadContentAsString();
						ListToSerialize.Add(Content);
						InternalReader.ReadEndElement();
					}
					InternalReader.ReadEndElement();
				}
			}
			else
			{
				InternalWriter.WriteStartElement(ListName);
				if(ListToSerialize.Count > 0)
				{
					foreach(string CurrentString in ListToSerialize)
					{
						InternalWriter.WriteElementString(ElementName, CurrentString);
					}
				}
				InternalWriter.WriteEndElement();
			}
		}

		public void SerializeListEmbedded<ListType>(string ListName, string ElementName, ref List<ListType> ListToSerialize) where ListType : XMLSerializable, new()
		{
			if(bReading)
			{
				bool isEmptyElement = InternalReader.IsEmptyElement;
				InternalReader.ReadStartElement(ListName);
		
				if (!isEmptyElement)
				{
					int CurrentDepth = InternalReader.Depth;
					while(InternalReader.Depth >= CurrentDepth)
					{
	//					InternalReader.ReadStartElement(ElementName);
						ListType NewElement = new ListType();
						XMLSerializable.SerializeFromXMLEmbedded<ListType>(ref InternalReader, ref InternalWriter, ref NewElement);
						NewElement = (ListType)NewElement.ResolveIDPatching();
						ListToSerialize.Add(NewElement);
	//					InternalReader.ReadEndElement();
					}
					InternalReader.ReadEndElement();
				}
			}
			else
			{
				InternalWriter.WriteStartElement(ListName);
				if(ListToSerialize.Count > 0)
				{
					foreach(ListType CurrentElement in ListToSerialize)
					{
						ListType RefCurrentElement = CurrentElement;
						XMLSerializable.SerializeFromXMLEmbedded<ListType>(ref InternalReader, ref InternalWriter, ref RefCurrentElement);
					}
				}
				InternalWriter.WriteEndElement();
			}
		}

		public void SerializeStringStringDictionaryEmbedded(string ListName, string KeyName, string ValueName, ref Dictionary<string, string> DictionaryToSerialize) 
		{
			if(bReading)
			{
				bool isEmptyElement = InternalReader.IsEmptyElement;
				InternalReader.ReadStartElement(ListName);
		
				if (!isEmptyElement)
				{
					int CurrentDepth = InternalReader.Depth;
					while(InternalReader.Depth >= CurrentDepth)
					{
						string NewKey = "";
						string NewValue = "";
						
						SerializeString(KeyName, ref NewKey);
						SerializeString(ValueName, ref NewValue);
						
	#if UNITY_EDITOR
						if(DictionaryToSerialize.ContainsKey(NewKey))
						{
							Debug.Log("Bad data!  Key \"" + NewKey + "\" already exists!");
						}
						else
	#endif // UNITY_EDITOR
						{
							DictionaryToSerialize.Add(NewKey, NewValue);
						}
					}
					InternalReader.ReadEndElement();
				}
			}
			else
			{
				InternalWriter.WriteStartElement(ListName);
				if(DictionaryToSerialize.Count > 0)
				{
					foreach(KeyValuePair<string, string> CurrentElement in DictionaryToSerialize)
					{
						string RefCurrentKey = CurrentElement.Key;
						SerializeString(KeyName, ref RefCurrentKey);
						string RefCurrentValue = CurrentElement.Value;
						SerializeString(ValueName, ref RefCurrentValue);
					}
				}
				InternalWriter.WriteEndElement();
			}
		}
		
		public void SerializeDictionaryStringFloatEmbedded(string ListName, string KeyName, string ValueName, ref Dictionary<string, float> DictionaryToSerialize) 
		{
			if(bReading)
			{
				bool isEmptyElement = InternalReader.IsEmptyElement;
				InternalReader.ReadStartElement(ListName);
				
				if (!isEmptyElement)
				{
					int CurrentDepth = InternalReader.Depth;
					while(InternalReader.Depth >= CurrentDepth)
					{
						string NewKey = "";
						float NewValue = 0.0f;
						
						SerializeString(KeyName, ref NewKey);
						SerializeFloat(ValueName, ref NewValue);
						
						#if UNITY_EDITOR
						if(DictionaryToSerialize.ContainsKey(NewKey))
						{
							Debug.Log("Bad data!  Key \"" + NewKey + "\" already exists!");
						}
						else
							#endif // UNITY_EDITOR
						{
							DictionaryToSerialize.Add(NewKey, NewValue);
						}
					}
					InternalReader.ReadEndElement();
				}
			}
			else
			{
				InternalWriter.WriteStartElement(ListName);
				if(DictionaryToSerialize.Count > 0)
				{
					foreach(KeyValuePair<string, float> CurrentElement in DictionaryToSerialize)
					{
						string RefCurrentKey = CurrentElement.Key;
						SerializeString(KeyName, ref RefCurrentKey);
						float RefCurrentValue = CurrentElement.Value;
						SerializeFloat(ValueName, ref RefCurrentValue);
					}
				}
				InternalWriter.WriteEndElement();
			}
		}

		public void SerializeDictionaryEmbedded<KeyType, ValueType>(string ListName, string KeyName, string ValueName, ref Dictionary<KeyType, ValueType> DictionaryToSerialize) 
																								where KeyType : XMLSerializable, new()
																								where ValueType : XMLSerializable, new()
		{
			if(bReading)
			{
				bool isEmptyElement = InternalReader.IsEmptyElement;
				InternalReader.ReadStartElement(ListName);
		
				if (!isEmptyElement)
				{
					int CurrentDepth = InternalReader.Depth;
					while(InternalReader.Depth >= CurrentDepth)
					{
						KeyType NewKey = new KeyType();

						if(InternalReader.IsStartElement())
						{
							string NextElementType = InternalReader.Name;

							object Temp = TypeUtils.GetNewObjectOfTypeString(NextElementType);
							
							if(typeof(KeyType).IsAssignableFrom(Temp.GetType()))
							{
								NewKey = (KeyType)Temp;
							}
							
							bReadStartElement = true;
						}

						XMLSerializable.SerializeFromXMLEmbedded<KeyType>(ref InternalReader, ref InternalWriter, ref NewKey);
						ValueType NewValue = new ValueType();
						
						if(InternalReader.IsStartElement())
						{
							string NextElementType = InternalReader.Name;
							
							object Temp = TypeUtils.GetNewObjectOfTypeString(NextElementType);

							if(typeof(ValueType).IsAssignableFrom(Temp.GetType()))
							{
								NewValue = (ValueType)Temp;
							}
							
							bReadStartElement = true;
						}

						XMLSerializable.SerializeFromXMLEmbedded<ValueType>(ref InternalReader, ref InternalWriter, ref NewValue);
						NewKey = (KeyType)NewKey.ResolveIDPatching();
						NewValue = (ValueType)NewValue.ResolveIDPatching();
						
						if(NewKey == null)
						{
							NewKey = new KeyType();
						}
						
						if(NewValue == null)
						{
							NewValue = new ValueType();
						}
						
						DictionaryToSerialize.Add(NewKey, NewValue);
					}
					InternalReader.ReadEndElement();
				}
			}
			else
			{
				InternalWriter.WriteStartElement(ListName);
				if(DictionaryToSerialize.Count > 0)
				{
					foreach(KeyValuePair<KeyType, ValueType> CurrentElement in DictionaryToSerialize)
					{
						KeyType RefCurrentKey = CurrentElement.Key;
						XMLSerializable.SerializeFromXMLEmbedded<KeyType>(ref InternalReader, ref InternalWriter, ref RefCurrentKey);
						ValueType RefCurrentValue = CurrentElement.Value;
						XMLSerializable.SerializeFromXMLEmbedded<ValueType>(ref InternalReader, ref InternalWriter, ref RefCurrentValue);
					}
				}
				InternalWriter.WriteEndElement();
			}
		}

		public void SerializeDictionaryStringEmbedded<ValueType>(string ListName, string KeyName, string ValueName, ref Dictionary<string, ValueType> DictionaryToSerialize) 
																							where ValueType : XMLSerializable, new()
		{
			if(bReading)
			{
				bool isEmptyElement = InternalReader.IsEmptyElement;
				InternalReader.ReadStartElement(ListName);
				
				if (!isEmptyElement)
				{
					int CurrentDepth = InternalReader.Depth;
					while(InternalReader.Depth >= CurrentDepth)
					{
						string NewKey = "";

						SerializeString(KeyName, ref NewKey);

						ValueType NewValue = new ValueType();
						
						if(InternalReader.IsStartElement())
						{
							string NextElementType = InternalReader.Name;
							
							object Temp = TypeUtils.GetNewObjectOfTypeString(NextElementType);
							
							if(typeof(ValueType).IsAssignableFrom(Temp.GetType()))
							{
								NewValue = (ValueType)Temp;
							}
							
							bReadStartElement = true;
						}
						
						XMLSerializable.SerializeFromXMLEmbedded<ValueType>(ref InternalReader, ref InternalWriter, ref NewValue);
						NewValue = (ValueType)NewValue.ResolveIDPatching();
						
						if(NewKey == null)
						{
							NewKey = "";
						}
						
						if(NewValue == null)
						{
							NewValue = new ValueType();
						}
						
						DictionaryToSerialize.Add(NewKey, NewValue);
					}
					InternalReader.ReadEndElement();
				}
			}
			else
			{
				InternalWriter.WriteStartElement(ListName);
				if(DictionaryToSerialize.Count > 0)
				{
					foreach(KeyValuePair<string, ValueType> CurrentElement in DictionaryToSerialize)
					{
						string RefCurrentKey = CurrentElement.Key;
						SerializeString(KeyName, ref RefCurrentKey);
						ValueType RefCurrentValue = CurrentElement.Value;
						XMLSerializable.SerializeFromXMLEmbedded<ValueType>(ref InternalReader, ref InternalWriter, ref RefCurrentValue);
					}
				}
				InternalWriter.WriteEndElement();
			}
		}

		public void SerializeDictionaryObjectFloatEmbedded<KeyType>(string ListName, string KeyName, string ValueName, ref Dictionary<KeyType, float> DictionaryToSerialize) 
																								where KeyType : XMLSerializable, new()
		{
			if(bReading)
			{
				bool isEmptyElement = InternalReader.IsEmptyElement;
				InternalReader.ReadStartElement(ListName);
		
				if (!isEmptyElement)
				{
					int CurrentDepth = InternalReader.Depth;
					while(InternalReader.Depth >= CurrentDepth)
					{
						KeyType NewKey = new KeyType();
						float NewValue = 0.0f;
						XMLSerializable.SerializeFromXMLEmbedded<KeyType>(ref InternalReader, ref InternalWriter, ref NewKey);
						NewKey = (KeyType)NewKey.ResolveIDPatching();
						SerializeFloat(ValueName, ref NewValue);
						if(NewKey == null)
						{
							NewKey = new KeyType();
						}
						DictionaryToSerialize.Add(NewKey, NewValue);
					}
					InternalReader.ReadEndElement();
				}
			}
			else
			{
				InternalWriter.WriteStartElement(ListName);
				if(DictionaryToSerialize.Count > 0)
				{
					foreach(KeyValuePair<KeyType, float> CurrentElement in DictionaryToSerialize)
					{
						KeyType RefCurrentKey = CurrentElement.Key;
						XMLSerializable.SerializeFromXMLEmbedded<KeyType>(ref InternalReader, ref InternalWriter, ref RefCurrentKey);
						float RefCurrentValue = CurrentElement.Value;
						SerializeFloat(ValueName, ref RefCurrentValue);
					}
				}
				InternalWriter.WriteEndElement();
			}
		}
		
		public virtual XMLSerializable ResolveIDPatching()
		{
			return this;
		}

		public XmlSchema GetSchema() { return null; }

		public void ReadXml(XmlReader reader)
		{
			InternalSerializeXML(reader, null);
		}

		public void WriteXml(XmlWriter writer)
		{
			InternalSerializeXML(null, writer);
		}
		
		public static void GuaranteePathExists(string Filename)
		{
			if(Filename != "")
			{
				FileInfo FileNameInfo = new FileInfo(Filename);
				string FilePath = FileNameInfo.DirectoryName;
				
				Directory.CreateDirectory(FilePath);
				
				if(File.Exists(Filename))
				{
					File.SetAttributes(Filename, FileAttributes.Normal);
				}
			}
		}
		
		public static void SerializeFromXML<SerializeType>(string Filename, ref SerializeType ToSerialize, bool bSaving, XmlSerializer Serializer = null) where SerializeType : XMLSerializable
		{
			if(bSaving)
			{
				XmlSerializer SpecificSerializer = Serializer;
				
				if(SpecificSerializer == null)
				{
					try
					{
						SpecificSerializer = new XmlSerializer(typeof(SerializeType));
					}
					catch(Exception Ex)
					{
						if(Ex != null)
						{
							Debug.LogError("Error creating an XML serializer for type " + typeof(SerializeType).ToString() + "!!  Exception message is\n" + Ex.Message + "\nAnd inner exception is\n" + Ex.InnerException.Message);
						}
					}
				}
			
				GuaranteePathExists(Filename);
				TextWriter SpecificWriter = new StreamWriter(Filename);
				try
				{
					SpecificSerializer.Serialize(SpecificWriter, ToSerialize);
				}
				catch(Exception Ex)
				{
					if(Ex != null)
					{
						Debug.LogError("Error serializing an XML file!!  Exception message is\n" + Ex.Message + "\nAnd inner exception is\n" + Ex.InnerException.Message);
					}
				}
				ToSerialize.PostSerialize();
				SpecificWriter.Close();
			}
			else
			{
				if(!bSafeToLoad)
				{
					TypeUtils.RegisterAllTypes();
	//				ToSerialize = null;
					
	//				return;
				}
	#if !UNITY_EDITOR
				if(Filename.StartsWith("Assets/Resources/"))
				{
					XmlSerializer SpecificSerializer = Serializer;
					
					if(SpecificSerializer == null)
					{
						SpecificSerializer = new XmlSerializer(typeof(SerializeType));
					}
					
					string ResourcePath = Filename.Substring("Assets/Resources/".Length);
					ResourcePath = ResourcePath.Substring(0, ResourcePath.Length - 4);
					TextAsset XMLAsset = (TextAsset)Resources.Load(ResourcePath, typeof(TextAsset));
					
					if(XMLAsset != null)
					{
						string RawXML = XMLAsset.text;
						
						TextReader SpecificReader = new StringReader(RawXML);
						ToSerialize = (SerializeType)SpecificSerializer.Deserialize(SpecificReader);
						ToSerialize.PostSerialize();
						SpecificReader.Close();
					}
					else
					{
						Debug.LogError("Failed to load XML from resource file with name " + ResourcePath);
					}
				}
				else
				{
					if(!Filename.Contains("GameSaves"))
					{
						Debug.LogError("Trying to serialize in an XML file that isn't in the resources folder.  Filename is " + Filename);
					}
	#else
				{
	#endif
					if(File.Exists(Filename))
					{
						XmlSerializer SpecificSerializer = Serializer;
						
						if(SpecificSerializer == null)
						{
							try
							{
								SpecificSerializer = new XmlSerializer(typeof(SerializeType));
							}
							catch(Exception Ex)
							{
								if(Ex != null)
								{
									Debug.LogError("Error creating an XML deserializer for type " + typeof(SerializeType).ToString() + "!!  Exception message is\n" + Ex.Message + "\nAnd inner exception is\n" + Ex.InnerException.Message);
								}
							}
						}
						
						TextReader SpecificReader = new StreamReader(Filename);
						try
						{
							ToSerialize = (SerializeType)SpecificSerializer.Deserialize(SpecificReader);
						}
						catch(Exception Ex)
						{
							if(Ex != null)
							{
								Debug.LogError("Error deserializing an XML file!!  Exception message is\n" + Ex.Message + "\nAnd inner exception is\n" + Ex.InnerException != null ? Ex.InnerException.Message : "");
							}
						}
						ToSerialize.PostSerialize();
						SpecificReader.Close();
					}
				}
			}
		}

		public static void SerializeFromXMLEmbedded<SerializeType>(ref XmlReader Reader, ref XmlWriter Writer, ref SerializeType ToSerialize) where SerializeType : XMLSerializable
		{
			ToSerialize.bEmbedded = true;
			ToSerialize.InternalSerializeXML(Reader, Writer);
			if(Writer != null)
			{
				Writer.WriteEndElement();
			}
			ToSerialize.bEmbedded = false;
		}
		
		public virtual void PostSerialize()
		{
			CreateStaticNodesIfNotPresent();
		}
		
		public virtual void CreateStaticNodesIfNotPresent()
		{
		}
		
		public virtual void PopulateToFromValue<GenericType>(ref GenericType LocalValue, ref GenericType ToFromInstanceValue, bool bFromInstance)
		{
			if(bFromInstance)
			{
				LocalValue = ToFromInstanceValue;
			}
			else
			{
				ToFromInstanceValue = LocalValue;
			}
		}
	}
}

#endif // IGOR_RUNTIME || UNITY_EDITOR
